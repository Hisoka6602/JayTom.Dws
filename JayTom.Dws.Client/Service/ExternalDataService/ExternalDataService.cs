using System;
using ImTools;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm;

namespace JayTom.Dws.Client.Service.ExternalDataService {

    public class ExternalDataService : IExternalDataService {
        private readonly IConfigRepository _configRepository;
        private readonly ITcpVolumeInput _tcpVolumeInput;
        private readonly ITcpContentInput _tcpContentInput;
        private VolumeSettingsDto _volumeSettingsDto = new();
        private ContentInputSettingsDto _contentInputSettingsDto = new();
        private ConcurrentQueue<string> _volumeBarCodeItems = new();

        public ExternalDataService(IConfigRepository configRepository,
            ITcpVolumeInput tcpVolumeInput,
            ITcpContentInput tcpContentInput) {
            _configRepository = configRepository;
            _tcpVolumeInput = tcpVolumeInput;
            _tcpContentInput = tcpContentInput;
            _tcpVolumeInput.Exception += delegate (object? sender, Exception exception) {
                OnExternalDataException(exception);
            };
            _tcpVolumeInput.ConnectionException += delegate (object? sender, string s) {
                OnExternalDataException(new Exception(s));
            };
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public event EventHandler<Exception>? ExternalDataException;

        public event EventHandler<ExternalDataSourceEventArgs>? DataSourceEnabled;

        public event EventHandler<ExternalVolumeInputEventArgs>? VolumeReceived;

        public event EventHandler<ExternalContentInputEventArgs>? ContentInputReceived;

        public event EventHandler<KeyValuePair<bool, string>>? WeightReceived;

        public event EventHandler<KeyValuePair<bool, string>>? ImagePathReceived;

        public event EventHandler<KeyValuePair<bool, string>>? ResponseContentReceived;

        public async Task<KeyValuePair<bool, string>> GetVolume(string barcode, CancellationToken token = default) {
            await Task.Delay(_volumeSettingsDto.VolumeInformationRequesterInfo.SendDelay, token);
            if (_volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType == VolumeRequesterType.Tcp) {
                var sendCount = _volumeSettingsDto.VolumeInformationRequesterInfo.SendCount < 0
                    ? 0
                    : _volumeSettingsDto.VolumeInformationRequesterInfo.SendCount;

                for (int i = 0; i < sendCount; i++) {
                    var sendMessage = await _tcpVolumeInput.SendMessage(_volumeSettingsDto.VolumeInformationRequesterInfo.SendContent, token);
                    if (!sendMessage) {
                        OnExternalDataException(new Exception($"{Languages.Language.ResourceManager.GetString("发送失败") ?? string.Empty}"));
                        return new KeyValuePair<bool, string>(false, $"{Languages.Language.ResourceManager.GetString("发送失败") ?? string.Empty}");
                    }
                    await Task.Delay(_volumeSettingsDto.VolumeInformationRequesterInfo.SendInterval, token);
                }

                _volumeBarCodeItems.Enqueue(barcode);
            }
            else {
                //先不要管串口
            }

            //判断等待时间
            //发送消息
            //条码加入队列
            return new KeyValuePair<bool, string>(false, $"{Languages.Language.ResourceManager.GetString("发送成功") ?? string.Empty}");
        }

        public Task<KeyValuePair<bool, string>> GetWeight(string barcode, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> GetImagePath(string barcode, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> GetResponseContent(string barcode, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default) {
            //读取体积配置
            try {
                var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("VolumeSettings"), token);
                if (configInfoModel is not null) {
                    _volumeSettingsDto = JsonConvert.DeserializeObject<VolumeSettingsDto>(configInfoModel.Value) ?? new VolumeSettingsDto();

                    if (_volumeSettingsDto.IsUseExternalVolumeInput) {
                        if (_volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType == VolumeRequesterType.Tcp) {
                            if (_volumeSettingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo.ConnectionMode == TcpConnectionMode.Server) {
                                //创建服务端

                                if (_tcpVolumeInput.ConnectionStatus == ConnectionStatus.Connected) {
                                    _tcpVolumeInput.Close();
                                }
                                _tcpVolumeInput.Communication += TcpCommunicationOnCommunication;
                                var connect = await _tcpVolumeInput.Connect(_volumeSettingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo.ServerConfig.IpAddress,
                                    _volumeSettingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo.ServerConfig.Port, ConnectionType.Server, token: token);
                                if (!connect) {
                                    OnExternalDataException(new Exception("TCP server creation failed"));
                                }
                            }
                            else {
                                if (_tcpVolumeInput.ConnectionStatus == ConnectionStatus.Connected) {
                                    _tcpVolumeInput.Close();
                                }
                                _tcpVolumeInput.Communication += TcpCommunicationOnCommunication;
                                var connect = await _tcpVolumeInput.Connect(_volumeSettingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo.ClientConfig.IpAddress,
                                    _volumeSettingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo.ClientConfig.Port, ConnectionType.Client, token: token);
                                if (!connect) {
                                    OnExternalDataException(new Exception("TCP client creation failed"));
                                }
                                //创建客户端
                            }
                        }
                        else if (_volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType ==
                                 VolumeRequesterType.SerialPort) {
                            //先不管串口
                        }
                    }

                    OnDataSourceEnabled(new ExternalDataSourceEventArgs() {
                        IsVolumeInput = _volumeSettingsDto.IsUseExternalVolumeInput
                    });
                }

                var infoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("ContentInputSettings"), token);
                if (infoModel is not null) {
                    _contentInputSettingsDto = JsonConvert.DeserializeObject<ContentInputSettingsDto>(infoModel.Value) ?? new ContentInputSettingsDto();
                }
            }
            catch (Exception e) {
                OnExternalDataException(e);
                return new KeyValuePair<bool, string>(false, $"{e.Message}");
            }

            return new KeyValuePair<bool, string>(false, string.Empty);
        }

        //Tcp内容输入
        private void TcpContentInputOnCommunication(object? sender, CommunicationInfo e) {
            if (!string.IsNullOrEmpty(e.Content) && e.Type == CommunicationType.Receive) {
            }
        }

        /// <summary>
        /// 体积输入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TcpCommunicationOnCommunication(object? sender, CommunicationInfo e) {
            if (!string.IsNullOrEmpty(e.Content) && e.Type == CommunicationType.Receive) {
                float length = 0, width = 0, height = 0, volume = 0;
                var split = Regex.Escape(e.Content).Split(_volumeSettingsDto.Separator);
                if (split.Length == _volumeSettingsDto.DataTemplate.Count(c => c.Type != 2)) {
                    var templateInfos = _volumeSettingsDto.DataTemplate.Where(w => w.Type != 2).ToList();
                    for (int i = 0; i < split.Length; i++) {
                        if (templateInfos[i].Content.ToLower().Contains("length")) {
                            float.TryParse(split[i], out length);
                        }
                        else if (templateInfos[i].Content.ToLower().Contains("width")) {
                            float.TryParse(split[i], out width);
                        }
                        else if (templateInfos[i].Content.ToLower().Contains("height")) {
                            float.TryParse(split[i], out height);
                        }
                        else if (templateInfos[i].Content.ToLower().Contains("volume")) {
                            float.TryParse(split[i], out volume);
                        }
                    }
                }
                else {
                    OnExternalDataException(new Exception($"split.Length:{split.Length} DataTemplate:{_volumeSettingsDto.DataTemplate.Count(c => c.Type != 2)},判断不相等"));
                }

                _volumeBarCodeItems.TryDequeue(out var barcode);
                if (barcode is not null) {
                    OnVolumeReceived(new ExternalVolumeInputEventArgs() {
                        BarCode = barcode,
                        Length = length,
                        Width = width,
                        Height = height,
                        Volume = volume,
                        ReceiveSource = string.Empty,
                        ReceiveTime = DateTime.Now
                    });
                }
                //取出模板组合

                //判断消息是否符合模板
                //从队列取出条码
                //取出长宽高，组合成消息模板
                //事件通知
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default) {
            await Task.Yield();
            _volumeBarCodeItems.Clear();
            try {
                if (_volumeSettingsDto.IsUseExternalVolumeInput) {
                    if (_volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType == VolumeRequesterType.Tcp) {
                        _tcpVolumeInput.Communication -= TcpCommunicationOnCommunication;
                        if (_tcpVolumeInput.ConnectionStatus == ConnectionStatus.Connected) {
                            _tcpVolumeInput.Close();
                        }
                    }
                    else if (_volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType ==
                             VolumeRequesterType.SerialPort) {
                    }
                }

                if (_contentInputSettingsDto.IsUseTcpInput) {
                    _tcpContentInput.Communication -= TcpContentInputOnCommunication;
                    if (_tcpContentInput.ConnectionStatus == ConnectionStatus.Connected) {
                        _tcpContentInput.Close();
                    }
                }
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            catch (Exception e) {
                OnExternalDataException(e);
                return new KeyValuePair<bool, string>(false, $"{e.Message}");
            }
        }

        protected virtual async void OnExternalDataException(Exception e) {
            await Task.Yield();
            ExternalDataException?.Invoke(this, e);
        }

        protected virtual async void OnDataSourceEnabled(ExternalDataSourceEventArgs e) {
            await Task.Yield();
            DataSourceEnabled?.Invoke(this, e);
        }

        protected virtual async void OnVolumeReceived(ExternalVolumeInputEventArgs e) {
            await Task.Yield();
            VolumeReceived?.Invoke(this, e);
        }

        protected virtual async void OnContentInputReceived(ExternalContentInputEventArgs e) {
            await Task.Yield();
            ContentInputReceived?.Invoke(this, e);
        }
    }
}