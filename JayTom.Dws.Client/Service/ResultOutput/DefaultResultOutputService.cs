using Polly;
using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using TouchSocket.Sockets;
using System.Globalization;
using JayTom.Dws.Plugin.Tcp;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Plugin.Speech;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Client.EventMediators;
using NetTopologySuite.GeometriesGraph;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Client.Service.ResultOutput {

    public class DefaultResultOutputService : IResultOutputService {
        private readonly IConfigRepository _configRepository;
        private readonly ISpeech _speech;
        private readonly ITcpCommunicationClient _tcpCommunicationClient;
        private readonly ITcpCommunication _tcpCommunication;
        private readonly ISoundRepository _soundRepository;
        private SemaphoreSlim _semaphore = new(1);
        private ResultOutputSettingsDto? _outputSettingsDto;
        private List<SoundInfoModel>? _soundInfoModels = new();

        public DefaultResultOutputService(IConfigRepository configRepository,
            ISpeech speech, ITcpCommunicationClient tcpCommunicationClient,
            ITcpCommunication tcpCommunication, ISoundRepository soundRepository) {
            _configRepository = configRepository;
            _speech = speech;
            _tcpCommunicationClient = tcpCommunicationClient;
            _tcpCommunication = tcpCommunication;
            _soundRepository = soundRepository;
            //定义事件
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(position => {
                //播放声音事件
                if (position is TriggerPositionEvent trigger) {
                    if (_outputSettingsDto?.IsUseAudioOutput == true) {
                        if (_outputSettingsDto?.AudioOutputSettingsInfo?.TriggerPosition == trigger.TriggerPosition) {
                            SoundOutput(trigger.IsSuccess);
                        }
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
                            if (_tcpCommunication.Status != ServerState.Running) {
                                //创建连接
                                _tcpCommunication.SetParameter(new TcpConnectParam {
                                    Address = _outputSettingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                                    Port = _outputSettingsDto.TcpSettingsInfo.ServerConfig.Port,
                                });
                                _tcpCommunication.Connect();
                            }
                        }
                        else {
                            if (_tcpCommunicationClient.IsConnected) {
                                _tcpCommunicationClient.Close();
                            }
                            _tcpCommunicationClient.SetParameter(new TcpConnectParam {
                                Address = _outputSettingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                                Port = _outputSettingsDto.TcpSettingsInfo.ClientConfig.Port,
                            });
                            _tcpCommunicationClient?.Connect();
                        }
                    }

                    if (_outputSettingsDto.IsUseAudioOutput) {
                        _soundInfoModels = await _soundRepository.
                            Select(s => s.Id > 0, o => o.Id);
                    }
                    _semaphore.Release();
                }
            });
        }

        public event EventHandler<Exception>? OutputFailed;

        public async void ExecuteOutput(string barCode, float weight, DateTime scanTime, float length, float width, float height,
            float volume, string cameraSerialNumber, CancellationToken cancellationToken = default) {
            if (_outputSettingsDto is null) {
                await _semaphore.WaitAsync(cancellationToken);
                var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("ResultOutputSettings"), cancellationToken);
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
                        if (_tcpCommunication.Status != ServerState.Running) {
                            //创建连接
                            _tcpCommunication.SetParameter(new TcpConnectParam {
                                Address = _outputSettingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                                Port = _outputSettingsDto.TcpSettingsInfo.ServerConfig.Port,
                            });
                            _tcpCommunication.Connect();
                        }
                    }
                    else {
                        if (_tcpCommunicationClient.IsConnected) {
                            _tcpCommunicationClient.Close();
                        }
                        _tcpCommunicationClient.SetParameter(new TcpConnectParam {
                            Address = _outputSettingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                            Port = _outputSettingsDto.TcpSettingsInfo.ClientConfig.Port,
                        });
                        _tcpCommunicationClient?.Connect();
                    }
                }
                _semaphore.Release();
            }

            Task.Run(async () => {
                //获取数据格式
                var list = _outputSettingsDto.DataTemplate
                    ?.Where(w => w.ApplicationType == ItemApplicationType.ResultData)?
                    .Select(s => ParseTemplate(s.Content, barCode, weight, scanTime, length, width, height,
                        volume, cameraSerialNumber, true))
                    ?.ToList();
                if (list?.Any() != true) {
                    OnOutputFailed(new Exception("输出数据格式错误,未找到模板内容!"));
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
                    //Http输出
                    //位置输出
                    return true;
                });
            }, cancellationToken);
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
                    if (_outputSettingsDto.TcpSettingsInfo.ConnectionMode == TcpConnectionMode.Server) {
                        isSend = await _tcpCommunication.SendMessage(message);
                    }
                    if (_outputSettingsDto.TcpSettingsInfo.ConnectionMode == TcpConnectionMode.Client) {
                        isSend = await _tcpCommunicationClient.SendMessage(message);
                    }
                    if (isSend) {
                        EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                            IsSuccess = isSend,
                            TriggerPosition = TriggerPositionEnum.TcpOutput
                        });
                    }
                }
            }

            return isSend;
        }

        /// <summary>
        /// 声音输出
        /// </summary>
        /// <param name="isSuccess"></param>
        /// <param name="cancellationToken"></param>
        private void SoundOutput(bool isSuccess, CancellationToken cancellationToken = default) {
            if (_outputSettingsDto is not null) {
                if (_outputSettingsDto.IsUseAudioOutput) {
                    if (isSuccess) {
                        var soundInfoModel = _soundInfoModels?.FirstOrDefault(f =>
                            f.SoundName.Equals(_outputSettingsDto.AudioOutputSettingsInfo.SuccessAudio));
                        if (soundInfoModel is not null) {
                            _speech.PlayCacheByteFile(soundInfoModel.SoundName, soundInfoModel.SoundFile ?? Array.Empty<byte>());
                        }
                    }
                    else {
                        var soundInfoModel = _soundInfoModels?.FirstOrDefault(f =>
                            f.SoundName.Equals(_outputSettingsDto.AudioOutputSettingsInfo.FailureAudio));
                        if (soundInfoModel is not null) {
                            _speech.PlayCacheByteFile(soundInfoModel.SoundName, soundInfoModel.SoundFile ?? Array.Empty<byte>());
                        }
                    }
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

        protected virtual async void OnOutputFailed(Exception e) {
            await Task.Yield();
            OutputFailed?.Invoke(this, e);
        }
    }
}