using JayTom.Dws.Application.Configuration;
using Polly;
using System;
using DryIoc;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Globalization;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Plugin.Speech;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Channels;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm;
using SettingsChangedEvent = JayTom.Dws.Domain.EventMediators.SettingsChangedEvent;
using TriggerPositionEvent = JayTom.Dws.Domain.EventMediators.TriggerPositionEvent;

namespace JayTom.Dws.Client.Service.ResultOutput
{

    public class DefaultResultOutputService : IResultOutputService, IAsyncDisposable
    {
        private readonly ISettingsStore _settingsStore;
        private readonly ISpeech _speech;
        private readonly ITcpContentOutput _tcpContentOutput;

        private readonly ISoundRepository _soundRepository;
        private readonly SemaphoreSlim _settingsSemaphore = new(1, 1);
        private readonly SemaphoreSlim _outputSemaphore = new(1, 1);
        private readonly SemaphoreSlim _soundSemaphore = new(1, 1);
        /// <summary>
        /// 串行处理数据输出的工作通道。
        /// </summary>
        private readonly Channel<Func<Task>> _outputWorkChannel =
            Channel.CreateBounded<Func<Task>>(new BoundedChannelOptions(1024)
            {
                SingleReader = true,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        /// <summary>
        /// 串行处理声音输出的工作通道。
        /// </summary>
        private readonly Channel<Func<Task>> _soundWorkChannel =
            Channel.CreateBounded<Func<Task>>(new BoundedChannelOptions(256)
            {
                SingleReader = true,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        /// <summary>
        /// 串行处理结果输出配置重载的工作通道。
        /// </summary>
        private readonly Channel<Func<Task>> _settingsWorkChannel =
            Channel.CreateBounded<Func<Task>>(new BoundedChannelOptions(32)
            {
                SingleReader = true,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        /// <summary>
        /// 统一控制三个结果输出消费者的生命周期。
        /// </summary>
        private readonly CancellationTokenSource _workerCancellation = new();
        /// <summary>
        /// 数据输出通道消费者。
        /// </summary>
        private readonly Task _outputWorker;
        /// <summary>
        /// 声音输出通道消费者。
        /// </summary>
        private readonly Task _soundWorker;
        /// <summary>
        /// 结果输出配置重载通道消费者。
        /// </summary>
        private readonly Task _settingsWorker;
        private ResultOutputSettingsDto? _outputSettingsDto;
        private ConcurrentDictionary<string, byte[]> _sounds = new();
        private System.IO.Ports.SerialPort? _serialPort { get; set; }

        public DefaultResultOutputService(ISettingsStore settingsStore,
            ISpeech speech, ITcpContentOutput tcpContentOutput,
            ISoundRepository soundRepository)
        {
            _settingsStore = settingsStore;
            _speech = speech;
            _tcpContentOutput = tcpContentOutput;
            _soundRepository = soundRepository;
            // 三个消费者彼此独立；每个消费者内部保持原有的单实例串行约束。
            _outputWorker = Task.Run(() => ProcessWorkAsync(_outputWorkChannel, _workerCancellation.Token));
            _soundWorker = Task.Run(() => ProcessWorkAsync(_soundWorkChannel, _workerCancellation.Token));
            _settingsWorker = Task.Run(() => ProcessWorkAsync(_settingsWorkChannel, _workerCancellation.Token));
            //tcp事件
            _tcpContentOutput.Exception += delegate (object? sender, Exception exception)
            {
                OnOutputFailed(exception);
            };
            _tcpContentOutput.ConnectionException += delegate (object? sender, string s)
            {
                OnOutputFailed(new Exception(s));
            };

            //扫到包裹
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(position =>
            {
                //播放声音事件
                if (position is TriggerPositionEvent trigger)
                {
                    var settings = Volatile.Read(ref _outputSettingsDto);
                    if (settings?.IsUseAudioOutput == true)
                    {
                        if (settings.AudioOutputSettingsInfo?.TriggerPosition == trigger.TriggerPosition)
                        {
                            SoundOutput(trigger.IsSuccess);
                        }
                    }
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(settings =>
            {
                if (settings is SettingsChangedEvent { SettingsName: "ResultOutputSettings" })
                {
                    // 配置和声音来自数据库，必须脱离事件发布线程执行。
                    QueueWork(_settingsWorkChannel, ReloadSettingsAsync);
                }
            });
            //默认加载
            EventAggregator.Instance.Publish(new SettingsChangedEvent()
            {
                SettingsName = "ResultOutputSettings",
            });
        }

        private async Task ReloadSettingsAsync()
        {
            await _settingsSemaphore.WaitAsync();
            try
            {
                var nextSettings = await _settingsStore
                    .GetAsync<ResultOutputSettingsDto>("ResultOutputSettings") ??
                    new ResultOutputSettingsDto();

                var nextSounds = new ConcurrentDictionary<string, byte[]>();
                if (nextSettings.IsUseAudioOutput)
                {
                    var soundInfoModels = await _soundRepository.Select(s => s.Id > 0, o => o.Id);
                    foreach (var soundInfoModel in soundInfoModels?
                                 .Where(soundInfoModel => soundInfoModel.SoundFile is not null)
                             ?? new List<SoundInfoModel>())
                    {
                        nextSounds.TryAdd(
                            soundInfoModel.SoundName,
                            soundInfoModel.SoundFile ?? []);
                    }
                }

                // 输出资源的替换与发送使用同一把锁，避免发送时关闭串口/TCP。
                await _outputSemaphore.WaitAsync();
                try
                {
                    if (_tcpContentOutput.ConnectionStatus == ConnectionStatus.Connected)
                    {
                        _tcpContentOutput.Close();
                    }

                    if (nextSettings.IsUseTcpOutput)
                    {
                        if (nextSettings.TcpSettingsInfo.ConnectionMode == TcpConnectionMode.Server)
                        {
                            await _tcpContentOutput.Connect(
                                nextSettings.TcpSettingsInfo.ServerConfig.IpAddress,
                                nextSettings.TcpSettingsInfo.ServerConfig.Port,
                                ConnectionType.Server);
                        }
                        else
                        {
                            await _tcpContentOutput.Connect(
                                nextSettings.TcpSettingsInfo.ClientConfig.IpAddress,
                                nextSettings.TcpSettingsInfo.ClientConfig.Port,
                                ConnectionType.Client);
                        }
                    }

                    _serialPort?.Close();
                    _serialPort?.Dispose();
                    _serialPort = null;
                    if (nextSettings.IsUseSerialOutput)
                    {
                        _serialPort = new System.IO.Ports.SerialPort
                        {
                            BaudRate = nextSettings.SerialPortSettingsInfo.BaudRate,
                            DataBits = nextSettings.SerialPortSettingsInfo.DataBits,
                            Parity = (System.IO.Ports.Parity)nextSettings.SerialPortSettingsInfo.Parity,
                            StopBits = (System.IO.Ports.StopBits)nextSettings.SerialPortSettingsInfo.StopBits,
                            PortName = nextSettings.SerialPortSettingsInfo.PortName,
                        };
                        _serialPort.Open();
                        if (!_serialPort.IsOpen)
                        {
                            OnOutputFailed(new Exception("输出串口连接失败"));
                        }
                    }

                    Volatile.Write(ref _sounds, nextSounds);
                    Volatile.Write(ref _outputSettingsDto, nextSettings);
                }
                finally
                {
                    _outputSemaphore.Release();
                }
            }
            catch (Exception e)
            {
                OnOutputFailed(e);
            }
            finally
            {
                _settingsSemaphore.Release();
            }
        }

        public event EventHandler<Exception>? OutputFailed;

        public void ExecuteOutput(string barCode, decimal weight, DateTime scanTime, decimal length, decimal width, decimal height,
            decimal volume, string cameraSerialNumber, CancellationToken cancellationToken = default)
        {
            var currentSettings = Volatile.Read(ref _outputSettingsDto);
            if (currentSettings is not null &&
                (currentSettings.IsUseLocationOutput || currentSettings.IsUseSerialOutput
                 || currentSettings.IsUseTcpOutput))
            {
                QueueWork(_outputWorkChannel, async () =>
                {
                    var lockTaken = false;
                    try
                    {
                        await _outputSemaphore.WaitAsync(cancellationToken);
                        lockTaken = true;
                        var settings = Volatile.Read(ref _outputSettingsDto);
                        if (settings is null)
                        {
                            return;
                        }

                        //获取数据格式
                        var list = settings.DataTemplate
                            ?.Where(w => w.ApplicationType == ItemApplicationType.ResultData)?
                            .Select(s => ParseTemplate(s.Content, barCode, weight, scanTime, length, width, height,
                                volume, cameraSerialNumber, true))
                            ?.ToList();
                        if (list?.Any() != true)
                        {
                            OnOutputFailed(new Exception($"{Languages.Language.ResourceManager.GetString("输出数据格式错误,未找到模板内容") ?? string.Empty}"));
                            return;
                        }
                        var message = string.Join(",", list);
                        //使用polly
                        var retryPolicy = Policy.HandleResult<bool>(result => !result)
                            .Or<TimeoutException>().RetryAsync(settings.UploadSettingsInfo.RetryCount, (a, b) =>
                            {
                            });

                        await retryPolicy.ExecuteAsync(async () =>
                        {
                            await Task.Delay(settings.UploadSettingsInfo.SendDelay, cancellationToken);
                            //Tcp输出
                            if (settings.IsUseTcpOutput)
                            {
                                return await TcpOutput(settings, message, cancellationToken);
                            }
                            //串口输出
                            if (settings.IsUseSerialOutput)
                            {
                                if (settings.SerialPortResultOutputInfo.IsUseCustomContentOutput)
                                {
                                    return await SerialPortOutput(settings, settings.SerialPortResultOutputInfo.CustomOutputContent);
                                }
                                else
                                {
                                    return await SerialPortOutput(settings, message);
                                }
                            }
                            //Http输出
                            //位置输出
                            return true;
                        });
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        // 调用方取消属于正常结束。
                    }
                    catch (Exception e)
                    {
                        OnOutputFailed(e);
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            _outputSemaphore.Release();
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Tcp输出
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> TcpOutput(
            ResultOutputSettingsDto settings,
            string message,
            CancellationToken cancellationToken = default)
        {
            var isSend = false;
            if (settings.IsUseTcpOutput)
            {
                isSend = await _tcpContentOutput.SendMessage(message, cancellationToken);
                if (isSend)
                {
                    EventAggregator.Instance.Publish(new TriggerPositionEvent()
                    {
                        IsSuccess = isSend,
                        TriggerPosition = TriggerPositionEnum.TcpOutput
                    });
                    EventAggregator.Instance.Publish(new OutputLogInfoModel()
                    {
                        Type = LogType.Information,
                        CreateTime = DateTime.Now,
                        OutputContent = message,
                        OutputType = OutputType.TcpOutput,
                        Message = $"Tcp输出:{message}"
                    });
                }
            }

            return isSend;
        }

        /// <summary>
        /// 串口输出
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Task<bool> SerialPortOutput(ResultOutputSettingsDto settings, string message)
        {
            if (settings.IsUseSerialOutput)
            {
                try
                {
                    switch (settings.SerialPortSettingsInfo.DataFormat)
                    {
                        case DataFormatType.Ascii:
                            _serialPort?.WriteLine(message);
                            EventAggregator.Instance.Publish(new OutputLogInfoModel()
                            {
                                Type = LogType.Information,
                                CreateTime = DateTime.Now,
                                OutputContent = message,
                                OutputType = OutputType.SerialPortOutput,
                                Message = $"串口输出:{message}"
                            });
                            return Task.FromResult(true);

                        case DataFormatType.Hex:
                            {
                                var toByteArray = HexStringToByteArray(message);
                                _serialPort?.Write(toByteArray, 0, toByteArray.Length);
                                EventAggregator.Instance.Publish(new OutputLogInfoModel()
                                {
                                    Type = LogType.Information,
                                    CreateTime = DateTime.Now,
                                    OutputContent = message,
                                    OutputType = OutputType.SerialPortOutput,
                                    Message = $"串口输出:{message}"
                                });
                                return Task.FromResult(true);
                            }
                    }
                }
                catch (Exception e)
                {
                    OnOutputFailed(e);
                    return Task.FromResult(false);
                }
            }
            return Task.FromResult(false);
        }

        /// <summary>
        /// 声音输出
        /// </summary>
        /// <param name="isSuccess"></param>
        /// <param name="cancellationToken"></param>
        private void SoundOutput(bool isSuccess, CancellationToken cancellationToken = default)
        {
            QueueWork(_soundWorkChannel,
                () => SoundOutputAsync(isSuccess, cancellationToken));
        }

        /// <summary>
        /// 将工作加入有界通道，并在通道繁忙或关闭时记录拒绝原因。
        /// </summary>
        private static void QueueWork(Channel<Func<Task>> channel, Func<Task> work)
        {
            if (!channel.Writer.TryWrite(work))
            {
                NLog.LogManager.GetCurrentClassLogger().Warn("结果输出队列已满或已停止，本次工作未入队");
            }
        }

        /// <summary>
        /// 持续执行通道中的输出工作，并隔离单项工作异常。
        /// </summary>
        private static async Task ProcessWorkAsync(Channel<Func<Task>> channel, CancellationToken token)
        {
            try
            {
                await foreach (var work in channel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    try
                    {
                        await work().ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        NLog.LogManager.GetCurrentClassLogger()
                            .Error(exception, "执行结果输出工作失败");
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 服务释放时终止消费者。
            }
        }

        /// <summary>
        /// 完成所有输出通道并等待后台消费者退出。
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            _outputWorkChannel.Writer.TryComplete();
            _soundWorkChannel.Writer.TryComplete();
            _settingsWorkChannel.Writer.TryComplete();
            _workerCancellation.Cancel();
            await Task.WhenAll(_outputWorker, _soundWorker, _settingsWorker).ConfigureAwait(false);
            _workerCancellation.Dispose();
            _settingsSemaphore.Dispose();
            _outputSemaphore.Dispose();
            _soundSemaphore.Dispose();
            _serialPort?.Dispose();
        }

        private async Task SoundOutputAsync(bool isSuccess, CancellationToken cancellationToken)
        {
            var lockTaken = false;
            try
            {
                await _soundSemaphore.WaitAsync(cancellationToken);
                lockTaken = true;
                var settings = Volatile.Read(ref _outputSettingsDto);
                var sounds = Volatile.Read(ref _sounds);
                if (settings is not null)
                {
                    if (settings.IsUseAudioOutput)
                    {
                        if (isSuccess)
                        {
                            var soundName = settings.AudioOutputSettingsInfo.SuccessAudio ?? string.Empty;
                            var tryGetValue = sounds.TryGetValue(soundName, out var file);
                            if (tryGetValue && file is not null)
                            {
                                await _speech.PlayCacheByteFile(soundName, file);
                                EventAggregator.Instance.Publish(new OutputLogInfoModel()
                                {
                                    Type = LogType.Information,
                                    CreateTime = DateTime.Now,
                                    OutputContent = soundName,
                                    OutputType = OutputType.AudioOutput,
                                    Message = $"声音输出:{soundName}"
                                });
                            }
                            else
                            {
                                NLog.LogManager.GetCurrentClassLogger().Error("找不到声音信息对象");
                            }
                        }
                        else
                        {
                            var soundName = settings.AudioOutputSettingsInfo.FailureAudio ?? string.Empty;
                            var tryGetValue = sounds.TryGetValue(soundName, out var file);
                            if (tryGetValue && file is not null)
                            {
                                await _speech.PlayCacheByteFile(soundName, file);
                                EventAggregator.Instance.Publish(new OutputLogInfoModel()
                                {
                                    Type = LogType.Information,
                                    CreateTime = DateTime.Now,
                                    OutputContent = soundName,
                                    OutputType = OutputType.AudioOutput,
                                    Message = $"声音输出:{soundName}"
                                });
                            }
                            else
                            {
                                NLog.LogManager.GetCurrentClassLogger().Error("找不到声音信息对象");
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 调用方取消属于正常结束。
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally
            {
                if (lockTaken)
                {
                    _soundSemaphore.Release();
                }
            }
        }

        //Tcp输出
        //串口输出(暂缓)
        //位置输出(暂缓)
        //Http输出(暂缓)
        //声音输出
        public string ParseTemplate(string source, string barCode, decimal weight, DateTime scanTime, decimal length,
            decimal width, decimal height, decimal volume, string cameraSerialNumber, bool isWatermark = false)
        {
            return source switch
            {
                "{BarCode}" => barCode,
                "{Weight}" => weight.ToString(CultureInfo.InvariantCulture),
                "{Volume}" => volume.ToString(CultureInfo.InvariantCulture),
                "{Length}" => length.ToString(CultureInfo.InvariantCulture),
                "{Width}" => width.ToString(CultureInfo.InvariantCulture),
                "{Height}" => height.ToString(CultureInfo.InvariantCulture),
                "{ScanTime}" => isWatermark ? $"{scanTime:yyyy-MM-dd HH:mm:ss.fff}" : $"{scanTime:yyyyMMddHHmmssfff}",
                "{TimestampedGuid}" => new DateTimeOffset(scanTime).ToUnixTimeMilliseconds().ToString(),
                "{CameraSerialNumber}" => cameraSerialNumber,
                "{Year}" => $"{scanTime:yyyy}",
                "{Month}" => $"{scanTime:MM}",
                "{Day}" => $"{scanTime:dd}",
                "{Hour}" => $"{scanTime:hh}",
                _ => "null"
            };
        }

        protected virtual void OnOutputFailed(Exception e)
        {
            OutputFailed?.Invoke(this, e);
        }

        private static byte[] HexStringToByteArray(string hexString)
        {
            hexString = hexString.Replace(" ", ""); // 移除空格

            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < hexString.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return bytes;
        }
    }
}
