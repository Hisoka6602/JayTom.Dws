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
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm;

namespace JayTom.Dws.Client.Service.ResultOutput {

    public class DefaultResultOutputService : IResultOutputService {
        private readonly IConfigRepository _configRepository;
        private readonly ISpeech _speech;
        private readonly ITcpContentOutput _tcpContentOutput;

        private readonly ISoundRepository _soundRepository;
        private SemaphoreSlim _semaphore = new(1);
        private ResultOutputSettingsDto? _outputSettingsDto;
        private ConcurrentDictionary<string, byte[]>? _sounds = new();
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
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(async position => {
                //播放声音事件
                if (position is TriggerPositionEvent trigger) {
                    if (_outputSettingsDto?.IsUseAudioOutput == true) {
                        if (_outputSettingsDto?.AudioOutputSettingsInfo?.TriggerPosition == trigger.TriggerPosition) {
                            SoundOutput(trigger.IsSuccess);
                        }
                    }
                }
            });
            //包裹信息组合完成
            EventAggregator.Instance.Subscribe<PackageInfo>(async position => {
                //播放声音事件
                if (position is PackageInfo packageInfo) {
                    if (_outputSettingsDto is { IsUseAudioOutput: true, AudioOutputSettingsInfo.TriggerPosition: TriggerPositionEnum.PackageInfoAssigned }) {
                        SoundOutput(true);
                    }
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async settings => {
                if (settings is SettingsChangedEvent { SettingsName: "ResultOutputSettings" }) {
                    await _semaphore.WaitAsync();
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("ResultOutputSettings"));
                    if (configInfoModel is not null) {
                        try {
                            _outputSettingsDto = JsonConvert.DeserializeObject<ResultOutputSettingsDto>(configInfoModel.Value);
                        }
                        catch (Exception e) {
                            OnOutputFailed(e);
                        }
                    }
                    _outputSettingsDto ??= new ResultOutputSettingsDto();
                    if (_outputSettingsDto.IsUseTcpOutput) {
                        //连接Tcp
                        //判断使用客户端还是服务端
                        //如果使用服务端，则一开始就有开启
                        if (_outputSettingsDto.TcpSettingsInfo.ConnectionMode == TcpConnectionMode.Server) {
                            if (_tcpContentOutput.ConnectionStatus == ConnectionStatus.Connected) {
                                //创建连接
                                _tcpContentOutput.Close();
                            }
                            await _tcpContentOutput.Connect(_outputSettingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                                _outputSettingsDto.TcpSettingsInfo.ServerConfig.Port, ConnectionType.Server);
                        }
                        else {
                            if (_tcpContentOutput.ConnectionStatus == ConnectionStatus.Connected) {
                                //创建连接
                                _tcpContentOutput.Close();
                            }
                            await _tcpContentOutput.Connect(_outputSettingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                                _outputSettingsDto.TcpSettingsInfo.ClientConfig.Port, ConnectionType.Client);
                        }
                    }

                    if (_outputSettingsDto.IsUseAudioOutput) {
                        var soundInfoModels = await _soundRepository.
                            Select(s => s.Id > 0, o => o.Id);
                        foreach (var soundInfoModel in soundInfoModels?.Where(soundInfoModel => soundInfoModel.SoundFile is not null) ?? new List<SoundInfoModel>()) {
                            _sounds?.AddOrUpdate(soundInfoModel.SoundName,
                                soundInfoModel?.SoundFile ?? Array.Empty<byte>(), (a, b) => b);
                        }
                    }

                    if (_outputSettingsDto.IsUseSerialOutput) {
                        //连接串口
                        try {
                            _serialPort?.Close();
                            _serialPort = new System.IO.Ports.SerialPort() {
                                BaudRate = _outputSettingsDto.SerialPortSettingsInfo.BaudRate,
                                DataBits = _outputSettingsDto.SerialPortSettingsInfo.DataBits,
                                Parity = _outputSettingsDto.SerialPortSettingsInfo.Parity,
                                StopBits = _outputSettingsDto.SerialPortSettingsInfo.StopBits,
                                PortName = _outputSettingsDto.SerialPortSettingsInfo.PortName,
                            };
                            _serialPort.Open();
                            if (!_serialPort.IsOpen) {
                                //语言设置
                                OnOutputFailed(new Exception("输出串口连接失败"));
                            }
                        }
                        catch (Exception e) {
                            OnOutputFailed(e);
                        }
                    }
                    _semaphore.Release();
                }
            });
            //默认加载
            EventAggregator.Instance.Publish(new SettingsChangedEvent() {
                SettingsName = "ResultOutputSettings",
            });
        }

        public event EventHandler<Exception>? OutputFailed;

        public void ExecuteOutput(string barCode, float weight, DateTime scanTime, float length, float width, float height,
            float volume, string cameraSerialNumber, CancellationToken cancellationToken = default) {
            if (_outputSettingsDto is not null &&
                (_outputSettingsDto.IsUseLocationOutput || _outputSettingsDto.IsUseSerialOutput
                || _outputSettingsDto.IsUseTcpOutput)) {
                Task.Run(async () => {
                    //获取数据格式
                    var list = _outputSettingsDto.DataTemplate
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
                        .Or<TimeoutException>().RetryAsync(_outputSettingsDto.UploadSettingsInfo.RetryCount, (a, b) => {
                        });

                    await retryPolicy.ExecuteAsync(async () => {
                        await Task.Delay(_outputSettingsDto.UploadSettingsInfo.SendDelay, cancellationToken);
                        //Tcp输出
                        if (_outputSettingsDto.IsUseTcpOutput) {
                            return await TcpOutput(message, cancellationToken);
                        }
                        //串口输出
                        if (_outputSettingsDto.IsUseSerialOutput) {
                            if (_outputSettingsDto.SerialPortResultOutputInfo.IsUseCustomContentOutput) {
                                return await SerialPortOutput(_outputSettingsDto.SerialPortResultOutputInfo.CustomOutputContent, cancellationToken);
                            }
                            else {
                                return await SerialPortOutput(message, cancellationToken);
                            }
                        }
                        //Http输出
                        //位置输出
                        return true;
                    });
                }, cancellationToken);
            }
        }

        /// <summary>
        /// Tcp输出
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> TcpOutput(string message, CancellationToken cancellationToken = default) {
            var isSend = false;
            if (_outputSettingsDto is not null) {
                if (_outputSettingsDto.IsUseTcpOutput) {
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
            }

            return isSend;
        }

        /// <summary>
        /// 串口输出
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> SerialPortOutput(string message, CancellationToken cancellationToken = default) {
            await Task.Yield();
            if (_outputSettingsDto is not null) {
                if (_outputSettingsDto.IsUseSerialOutput) {
                    try {
                        switch (_outputSettingsDto.SerialPortSettingsInfo.DataFormat) {
                            case DataFormatType.Ascii:
                                _serialPort?.WriteLine(message);
                                EventAggregator.Instance.Publish(new OutputLogInfoModel() {
                                    Type = LogType.Information,
                                    CreateTime = DateTime.Now,
                                    OutputContent = message,
                                    OutputType = OutputType.SerialPortOutput,
                                    Message = $"串口输出:{message}"
                                });
                                return true;

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
                                    return true;
                                }
                        }
                    }
                    catch (Exception e) {
                        OnOutputFailed(e);
                        return false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 声音输出
        /// </summary>
        /// <param name="isSuccess"></param>
        /// <param name="cancellationToken"></param>
        private async void SoundOutput(bool isSuccess, CancellationToken cancellationToken = default) {
            try {
                if (_outputSettingsDto is not null && _sounds is not null) {
                    if (_outputSettingsDto.IsUseAudioOutput) {
                        if (isSuccess) {
                            var tryGetValue = _sounds.TryGetValue(
                                _outputSettingsDto.AudioOutputSettingsInfo.SuccessAudio ?? string.Empty, out var file);
                            if (tryGetValue && file is not null) {
                                await _speech.PlayCacheByteFile(
                                    _outputSettingsDto.AudioOutputSettingsInfo.SuccessAudio ?? string.Empty, file);
                                EventAggregator.Instance.Publish(new OutputLogInfoModel() {
                                    Type = LogType.Information,
                                    CreateTime = DateTime.Now,
                                    OutputContent = _outputSettingsDto?.AudioOutputSettingsInfo?.SuccessAudio ?? string.Empty,
                                    OutputType = OutputType.AudioOutput,
                                    Message = $"声音输出:{_outputSettingsDto?.AudioOutputSettingsInfo?.SuccessAudio ?? string.Empty}"
                                });
                            }
                            else {
                                NLog.LogManager.GetCurrentClassLogger().Error("找不到声音信息对象");
                            }
                        }
                        else {
                            var tryGetValue = _sounds.TryGetValue(
                                _outputSettingsDto.AudioOutputSettingsInfo.FailureAudio ?? string.Empty, out var file);
                            if (tryGetValue && file is not null) {
                                await _speech.PlayCacheByteFile(
                                    _outputSettingsDto.AudioOutputSettingsInfo.FailureAudio ?? string.Empty, file);
                                EventAggregator.Instance.Publish(new OutputLogInfoModel() {
                                    Type = LogType.Information,
                                    CreateTime = DateTime.Now,
                                    OutputContent = _outputSettingsDto?.AudioOutputSettingsInfo?.SuccessAudio ?? string.Empty,
                                    OutputType = OutputType.AudioOutput,
                                    Message = $"声音输出:{_outputSettingsDto?.AudioOutputSettingsInfo?.SuccessAudio ?? string.Empty}"
                                });
                            }
                            else {
                                NLog.LogManager.GetCurrentClassLogger().Error("找不到声音信息对象");
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
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

        protected virtual async void OnOutputFailed(Exception e) {
            await Task.Yield();
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