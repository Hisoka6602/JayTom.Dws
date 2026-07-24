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
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;
using TriggerPositionEvent = JayTom.Dws.Client.EventMediators.TriggerPositionEvent;

namespace JayTom.Dws.Client.Service.ResultOutput {

    public class DefaultResultOutputService : IResultOutputService {
        private readonly IConfigRepository _configRepository;
        private readonly ISpeech _speech;
        private readonly ITcpContentOutput _tcpContentOutput;

        private readonly ISoundRepository _soundRepository;
        private readonly SemaphoreSlim _settingsSemaphore = new(1, 1);
        private readonly SemaphoreSlim _outputSemaphore = new(1, 1);
        private readonly SemaphoreSlim _soundSemaphore = new(1, 1);
        private ResultOutputSettingsDto? _outputSettingsDto;
        private ConcurrentDictionary<string, byte[]> _sounds = new();
        private System.IO.Ports.SerialPort? _serialPort { get; set; }

        public DefaultResultOutputService(IConfigRepository configRepository,
            ISpeech speech, ITcpContentOutput tcpContentOutput,
            ISoundRepository soundRepository) {
            _configRepository = configRepository;
            _speech = speech;
            _tcpContentOutput = tcpContentOutput;
            //tcp事件
            _tcpContentOutput.Exception += delegate (object? sender, Exception exception) {
                OnOutputFailed(exception);
            };
            _tcpContentOutput.ConnectionException += delegate (object? sender, string s) {
                OnOutputFailed(new Exception(s));
            };
            _soundRepository = soundRepository;

            //扫到包裹
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(position => {
                //播放声音事件
                if (position is TriggerPositionEvent trigger) {
                    var settings = Volatile.Read(ref _outputSettingsDto);
                    if (settings?.IsUseAudioOutput == true) {
                        if (settings.AudioOutputSettingsInfo?.TriggerPosition == trigger.TriggerPosition) {
                            SoundOutput(trigger.IsSuccess);
                        }
                    }
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(settings => {
                if (settings is SettingsChangedEvent { SettingsName: "ResultOutputSettings" }) {
                    // 配置和声音来自数据库，必须脱离事件发布线程执行。
                    _ = Task.Run(ReloadSettingsAsync);
                }
            });
            //默认加载
            EventAggregator.Instance.Publish(new SettingsChangedEvent() {
                SettingsName = "ResultOutputSettings",
            });
        }

        private async Task ReloadSettingsAsync() {
            await _settingsSemaphore.WaitAsync();
            try {
                var configInfoModel = await _configRepository.FirstOrDefault(
                    settings => settings.ConfigName.Equals("ResultOutputSettings"));
                var nextSettings = configInfoModel is null
                    ? new ResultOutputSettingsDto()
                    : JsonConvert.DeserializeObject<ResultOutputSettingsDto>(configInfoModel.Value)
                      ?? new ResultOutputSettingsDto();

                var nextSounds = new ConcurrentDictionary<string, byte[]>();
                if (nextSettings.IsUseAudioOutput) {
                    var soundInfoModels = await _soundRepository.Select(s => s.Id > 0, o => o.Id);
                    foreach (var soundInfoModel in soundInfoModels?
                                 .Where(soundInfoModel => soundInfoModel.SoundFile is not null)
                             ?? new List<SoundInfoModel>()) {
                        nextSounds.TryAdd(
                            soundInfoModel.SoundName,
                            soundInfoModel.SoundFile ?? Array.Empty<byte>());
                    }
                }

                // 输出资源的替换与发送使用同一把锁，避免发送时关闭串口/TCP。
                await _outputSemaphore.WaitAsync();
                try {
                    if (_tcpContentOutput.ConnectionStatus == ConnectionStatus.Connected) {
                        _tcpContentOutput.Close();
                    }

                    if (nextSettings.IsUseTcpOutput) {
                        if (nextSettings.TcpSettingsInfo.ConnectionMode == TcpConnectionMode.Server) {
                            await _tcpContentOutput.Connect(
                                nextSettings.TcpSettingsInfo.ServerConfig.IpAddress,
                                nextSettings.TcpSettingsInfo.ServerConfig.Port,
                                ConnectionType.Server);
                        }
                        else {
                            await _tcpContentOutput.Connect(
                                nextSettings.TcpSettingsInfo.ClientConfig.IpAddress,
                                nextSettings.TcpSettingsInfo.ClientConfig.Port,
                                ConnectionType.Client);
                        }
                    }

                    _serialPort?.Close();
                    _serialPort?.Dispose();
                    _serialPort = null;
                    if (nextSettings.IsUseSerialOutput) {
                        _serialPort = new System.IO.Ports.SerialPort {
                            BaudRate = nextSettings.SerialPortSettingsInfo.BaudRate,
                            DataBits = nextSettings.SerialPortSettingsInfo.DataBits,
                            Parity = nextSettings.SerialPortSettingsInfo.Parity,
                            StopBits = nextSettings.SerialPortSettingsInfo.StopBits,
                            PortName = nextSettings.SerialPortSettingsInfo.PortName,
                        };
                        _serialPort.Open();
                        if (!_serialPort.IsOpen) {
                            OnOutputFailed(new Exception("输出串口连接失败"));
                        }
                    }

                    Volatile.Write(ref _sounds, nextSounds);
                    Volatile.Write(ref _outputSettingsDto, nextSettings);
                }
                finally {
                    _outputSemaphore.Release();
                }
            }
            catch (Exception e) {
                OnOutputFailed(e);
            }
            finally {
                _settingsSemaphore.Release();
            }
        }

        public event EventHandler<Exception>? OutputFailed;

        public void ExecuteOutput(string barCode, float weight, DateTime scanTime, float length, float width, float height,
            float volume, string cameraSerialNumber, CancellationToken cancellationToken = default) {
            var currentSettings = Volatile.Read(ref _outputSettingsDto);
            if (currentSettings is not null &&
                (currentSettings.IsUseLocationOutput || currentSettings.IsUseSerialOutput
                 || currentSettings.IsUseTcpOutput)) {
                _ = Task.Run(async () => {
                    var lockTaken = false;
                    try {
                        await _outputSemaphore.WaitAsync(cancellationToken);
                        lockTaken = true;
                        var settings = Volatile.Read(ref _outputSettingsDto);
                        if (settings is null) {
                            return;
                        }

                    //获取数据格式
                    var list = settings.DataTemplate
                        ?.Where(w => w.ApplicationType == ItemApplicationType.ResultData)?
                        .Select(s => ParseTemplate(s.Content, barCode, weight, scanTime, length, width, height,
                            volume, cameraSerialNumber, true))
                        ?.ToList();
                    if (list?.Any() != true) {
                        OnOutputFailed(new Exception($"{Languages.Language.ResourceManager.GetString("输出数据格式错误,未找到模板内容") ?? string.Empty}"));
                        return;
                    }
                    var message = string.Join(",", list);
                    //使用polly
                    var retryPolicy = Policy.HandleResult<bool>(result => !result)
                        .Or<TimeoutException>().RetryAsync(settings.UploadSettingsInfo.RetryCount, (a, b) => {
                        });

                    await retryPolicy.ExecuteAsync(async () => {
                        await Task.Delay(settings.UploadSettingsInfo.SendDelay, cancellationToken);
                        //Tcp输出
                        if (settings.IsUseTcpOutput) {
                            return await TcpOutput(settings, message, cancellationToken);
                        }
                        //串口输出
                        if (settings.IsUseSerialOutput) {
                            if (settings.SerialPortResultOutputInfo.IsUseCustomContentOutput) {
                                return await SerialPortOutput(settings, settings.SerialPortResultOutputInfo.CustomOutputContent);
                            }
                            else {
                                return await SerialPortOutput(settings, message);
                            }
                        }
                        //Http输出
                        //位置输出
                        return true;
                    });
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                        // 调用方取消属于正常结束。
                    }
                    catch (Exception e) {
                        OnOutputFailed(e);
                    }
                    finally {
                        if (lockTaken) {
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
            CancellationToken cancellationToken = default) {
            var isSend = false;
            if (settings.IsUseTcpOutput) {
                    isSend = await _tcpContentOutput.SendMessage(message, cancellationToken);
                    if (isSend) {
                        EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                            IsSuccess = isSend,
                            TriggerPosition = TriggerPositionEnum.TcpOutput
                        });
                        EventAggregator.Instance.Publish(new OutputLogInfoModel() {
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
        private Task<bool> SerialPortOutput(ResultOutputSettingsDto settings, string message) {
            if (settings.IsUseSerialOutput) {
                    try {
                        switch (settings.SerialPortSettingsInfo.DataFormat) {
                            case DataFormatType.Ascii:
                                _serialPort?.WriteLine(message);
                                EventAggregator.Instance.Publish(new OutputLogInfoModel() {
                                    Type = LogType.Information,
                                    CreateTime = DateTime.Now,
                                    OutputContent = message,
                                    OutputType = OutputType.SerialPortOutput,
                                    Message = $"串口输出:{message}"
                                });
                                return Task.FromResult(true);

                            case DataFormatType.Hex: {
                                    var toByteArray = HexStringToByteArray(message);
                                    _serialPort?.Write(toByteArray, 0, toByteArray.Length);
                                    EventAggregator.Instance.Publish(new OutputLogInfoModel() {
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
                    catch (Exception e) {
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
        private void SoundOutput(bool isSuccess, CancellationToken cancellationToken = default) {
            _ = Task.Run(() => SoundOutputAsync(isSuccess, cancellationToken));
        }

        private async Task SoundOutputAsync(bool isSuccess, CancellationToken cancellationToken) {
            var lockTaken = false;
            try {
                await _soundSemaphore.WaitAsync(cancellationToken);
                lockTaken = true;
                var settings = Volatile.Read(ref _outputSettingsDto);
                var sounds = Volatile.Read(ref _sounds);
                if (settings is not null) {
                    if (settings.IsUseAudioOutput) {
                        if (isSuccess) {
                            var soundName = settings.AudioOutputSettingsInfo.SuccessAudio ?? string.Empty;
                            var tryGetValue = sounds.TryGetValue(soundName, out var file);
                            if (tryGetValue && file is not null) {
                                await _speech.PlayCacheByteFile(soundName, file);
                                EventAggregator.Instance.Publish(new OutputLogInfoModel() {
                                    Type = LogType.Information,
                                    CreateTime = DateTime.Now,
                                    OutputContent = soundName,
                                    OutputType = OutputType.AudioOutput,
                                    Message = $"声音输出:{soundName}"
                                });
                            }
                            else {
                                NLog.LogManager.GetCurrentClassLogger().Error("找不到声音信息对象");
                            }
                        }
                        else {
                            var soundName = settings.AudioOutputSettingsInfo.FailureAudio ?? string.Empty;
                            var tryGetValue = sounds.TryGetValue(soundName, out var file);
                            if (tryGetValue && file is not null) {
                                await _speech.PlayCacheByteFile(soundName, file);
                                EventAggregator.Instance.Publish(new OutputLogInfoModel() {
                                    Type = LogType.Information,
                                    CreateTime = DateTime.Now,
                                    OutputContent = soundName,
                                    OutputType = OutputType.AudioOutput,
                                    Message = $"声音输出:{soundName}"
                                });
                            }
                            else {
                                NLog.LogManager.GetCurrentClassLogger().Error("找不到声音信息对象");
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                // 调用方取消属于正常结束。
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                if (lockTaken) {
                    _soundSemaphore.Release();
                }
            }
        }

        //Tcp输出
        //串口输出(暂缓)
        //位置输出(暂缓)
        //Http输出(暂缓)
        //声音输出
        public string ParseTemplate(string source, string barCode, float weight, DateTime scanTime, float length,
            float width, float height, float volume, string cameraSerialNumber, bool isWatermark = false) {
            return source switch {
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

        protected virtual void OnOutputFailed(Exception e) {
            OutputFailed?.Invoke(this, e);
        }

        private static byte[] HexStringToByteArray(string hexString) {
            hexString = hexString.Replace(" ", ""); // 移除空格

            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < hexString.Length; i += 2) {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return bytes;
        }
    }
}
