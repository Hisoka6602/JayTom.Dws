using JayTom.Dws.Application.Configuration;
using System;
using ImTools;
using System.Linq;
using System.Globalization;
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

namespace JayTom.Dws.Client.Service.ExternalDataService
{
    public class ExternalDataService : IExternalDataService
    {
        private readonly ISettingsStore _settingsStore;
        private readonly ITcpVolumeInput _tcpVolumeInput;
        private readonly ITcpContentInput _tcpContentInput;
        private VolumeSettingsDto _volumeSettingsDto = new();
        private ContentInputSettingsDto _contentInputSettingsDto = new();
        private readonly ConcurrentQueue<PendingVolumeRequest> _volumeBarCodeItems = new();
        /// <summary>
        /// 清除外部输入控制字符的复用正则。
        /// </summary>
        private static readonly Regex ControlCharacterRegex = new(
            @"[\u0000-\u001f\b]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        public ExternalDataService(ISettingsStore settingsStore,
            ITcpVolumeInput tcpVolumeInput,
            ITcpContentInput tcpContentInput)
        {
            _settingsStore = settingsStore;
            _tcpVolumeInput = tcpVolumeInput;
            _tcpContentInput = tcpContentInput;
            _tcpVolumeInput.Exception += delegate (object? sender, Exception exception)
            {
                OnExternalDataException(exception);
            };
            _tcpVolumeInput.ConnectionException += delegate (object? sender, string s)
            {
                OnExternalDataException(new Exception(s));
            };
        }

        public void Dispose()
        {
            _tcpVolumeInput.Communication -= TcpCommunicationOnCommunication;
            _tcpContentInput.Communication -= TcpContentInputOnCommunication;
            _tcpVolumeInput.Close();
            _tcpContentInput.Close();
            _volumeBarCodeItems.Clear();
        }

        public event EventHandler<Exception>? ExternalDataException;

        public event EventHandler<ExternalDataSourceEventArgs>? DataSourceEnabled;

        public event EventHandler<ExternalVolumeInputEventArgs>? VolumeReceived;

        public event EventHandler<ExternalContentInputEventArgs>? ContentInputReceived;

        public event EventHandler<KeyValuePair<bool, string>>? WeightReceived;

        public event EventHandler<KeyValuePair<bool, string>>? ImagePathReceived;

        public event EventHandler<KeyValuePair<bool, string>>? ResponseContentReceived;

        public async Task<KeyValuePair<bool, string>> GetVolume(string barcode, CancellationToken token = default)
        {
            await Task.Delay(_volumeSettingsDto.VolumeInformationRequesterInfo.SendDelay, token);
            if (_volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType == VolumeRequesterType.Tcp)
            {
                var pendingRequest = new PendingVolumeRequest(barcode);
                _volumeBarCodeItems.Enqueue(pendingRequest);
                var sendCount = _volumeSettingsDto.VolumeInformationRequesterInfo.SendCount < 0
                    ? 0
                    : _volumeSettingsDto.VolumeInformationRequesterInfo.SendCount;

                for (int i = 0; i < sendCount; i++)
                {
                    var sendMessage = await _tcpVolumeInput.SendMessage(_volumeSettingsDto.VolumeInformationRequesterInfo.SendContent, token);
                    if (!sendMessage)
                    {
                        Interlocked.Exchange(ref pendingRequest.Cancelled, 1);
                        OnExternalDataException(new Exception($"{Languages.Language.ResourceManager.GetString("发送失败") ?? string.Empty}"));
                        return new KeyValuePair<bool, string>(false, $"{Languages.Language.ResourceManager.GetString("发送失败") ?? string.Empty}");
                    }
                    await Task.Delay(_volumeSettingsDto.VolumeInformationRequesterInfo.SendInterval, token);
                }

                return new KeyValuePair<bool, string>(true, $"{Languages.Language.ResourceManager.GetString("发送成功") ?? string.Empty}");
            }

            return new KeyValuePair<bool, string>(false, "当前外部体积请求方式尚未实现");
        }

        public Task<KeyValuePair<bool, string>> GetWeight(string barcode, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> GetImagePath(string barcode, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> GetResponseContent(string barcode, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default)
        {
            var startupErrors = new List<string>();
            //读取体积配置
            try
            {
                var volumeSettings = await _settingsStore
                    .GetAsync<VolumeSettingsDto>("VolumeSettings", token);
                if (volumeSettings is not null)
                {
                    _volumeSettingsDto = volumeSettings;

                    if (_volumeSettingsDto.IsUseExternalVolumeInput)
                    {
                        if (_volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType == VolumeRequesterType.Tcp)
                        {
                            if (_volumeSettingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo.ConnectionMode == TcpConnectionMode.Server)
                            {
                                //创建服务端

                                if (_tcpVolumeInput.ConnectionStatus == ConnectionStatus.Connected)
                                {
                                    _tcpVolumeInput.Close();
                                }
                                _tcpVolumeInput.Communication -= TcpCommunicationOnCommunication;
                                _tcpVolumeInput.Communication += TcpCommunicationOnCommunication;
                                var connect = await _tcpVolumeInput.Connect(_volumeSettingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo.ServerConfig.IpAddress,
                                    _volumeSettingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo.ServerConfig.Port, ConnectionType.Server, token: token);
                                if (!connect)
                                {
                                    startupErrors.Add("TCP体积输入服务端创建失败");
                                    OnExternalDataException(new Exception("TCP server creation failed"));
                                }
                            }
                            else
                            {
                                if (_tcpVolumeInput.ConnectionStatus == ConnectionStatus.Connected)
                                {
                                    _tcpVolumeInput.Close();
                                }
                                _tcpVolumeInput.Communication -= TcpCommunicationOnCommunication;
                                _tcpVolumeInput.Communication += TcpCommunicationOnCommunication;
                                var connect = await _tcpVolumeInput.Connect(_volumeSettingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo.ClientConfig.IpAddress,
                                    _volumeSettingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo.ClientConfig.Port, ConnectionType.Client, token: token);
                                if (!connect)
                                {
                                    startupErrors.Add("TCP体积输入客户端连接失败");
                                    OnExternalDataException(new Exception("TCP client creation failed"));
                                }
                                //创建客户端
                            }
                        }
                        else if (_volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType ==
                                 VolumeRequesterType.SerialPort)
                        {
                            //先不管串口
                        }
                    }

                    OnDataSourceEnabled(new ExternalDataSourceEventArgs()
                    {
                        IsVolumeInput = _volumeSettingsDto.IsUseExternalVolumeInput
                    });
                }

                var contentInputSettings = await _settingsStore
                    .GetAsync<ContentInputSettingsDto>("ContentInputSettings", token);
                if (contentInputSettings is not null)
                {
                    _contentInputSettingsDto = contentInputSettings;

                    if (_contentInputSettingsDto.IsUseTcpInput)
                    {
                        if (_contentInputSettingsDto.TcpSettingsInfo.ConnectionMode == TcpConnectionMode.Server)
                        {
                            //创建服务端

                            if (_tcpContentInput.ConnectionStatus == ConnectionStatus.Connected)
                                {
                                    _tcpContentInput.Close();
                                }
                                _tcpContentInput.Communication -= TcpContentInputOnCommunication;
                                _tcpContentInput.Communication += TcpContentInputOnCommunication;
                                var connect = await _tcpContentInput.Connect(_contentInputSettingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                                    _contentInputSettingsDto.TcpSettingsInfo.ServerConfig.Port, ConnectionType.Server, token: token);
                                if (!connect)
                                {
                                    startupErrors.Add("TCP内容输入服务端创建失败");
                                    OnExternalDataException(new Exception("TCP server creation failed"));
                                }
                        }
                        else
                        {
                            //创建客户端
                            if (_tcpContentInput.ConnectionStatus == ConnectionStatus.Connected)
                                {
                                    _tcpContentInput.Close();
                                }
                                _tcpContentInput.Communication -= TcpContentInputOnCommunication;
                                _tcpContentInput.Communication += TcpContentInputOnCommunication;
                                var connect = await _tcpContentInput.Connect(_contentInputSettingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                                    _contentInputSettingsDto.TcpSettingsInfo.ClientConfig.Port, ConnectionType.Client, token: token);
                                if (!connect)
                                {
                                    startupErrors.Add("TCP内容输入客户端连接失败");
                                    OnExternalDataException(new Exception("TCP client creation failed"));
                                }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OnExternalDataException(e);
                return new KeyValuePair<bool, string>(false, $"{e.Message}");
            }

            return startupErrors.Count == 0
                ? new KeyValuePair<bool, string>(true, "外部数据服务启动成功")
                : new KeyValuePair<bool, string>(false, string.Join(Environment.NewLine, startupErrors));
        }

        //Tcp内容输入
        private void TcpContentInputOnCommunication(object? sender, CommunicationInfo e)
        {
            if (!string.IsNullOrEmpty(e.Content) && e.Type == CommunicationType.Receive)
            {
                //暂时先不管Json格式
                //默认分隔符= '|'
                var inputEventArgs = new ExternalContentInputEventArgs
                {
                    SourceContent = e.Content
                };
                try
                {
                    float length = 0, width = 0, height = 0, volume = 0, weight = 0;
                    var split = e.Content.Split(_contentInputSettingsDto.Separator);
                    if (split.Length == _contentInputSettingsDto.DataTemplate.Count(c => c.Type != 2))
                    {
                        var templateInfos = _contentInputSettingsDto.DataTemplate.Where(w => w.Type != 2).ToList();
                        for (int i = 0; i < split.Length; i++)
                        {
                            if (templateInfos[i].Content.Contains("length", StringComparison.OrdinalIgnoreCase))
                            {
                                float.TryParse(split[i], NumberStyles.Float, CultureInfo.InvariantCulture, out length);
                                inputEventArgs.Length = length;
                            }
                            else if (templateInfos[i].Content.Contains("width", StringComparison.OrdinalIgnoreCase))
                            {
                                float.TryParse(split[i], NumberStyles.Float, CultureInfo.InvariantCulture, out width);
                                inputEventArgs.Width = width;
                            }
                            else if (templateInfos[i].Content.Contains("height", StringComparison.OrdinalIgnoreCase))
                            {
                                float.TryParse(split[i], NumberStyles.Float, CultureInfo.InvariantCulture, out height);
                                inputEventArgs.Height = height;
                            }
                            else if (templateInfos[i].Content.Contains("volume", StringComparison.OrdinalIgnoreCase))
                            {
                                float.TryParse(split[i], NumberStyles.Float, CultureInfo.InvariantCulture, out volume);
                                inputEventArgs.Volume = volume;
                            }
                            else if (templateInfos[i].Content.Contains("weight", StringComparison.OrdinalIgnoreCase))
                            {
                                float.TryParse(split[i], NumberStyles.Float, CultureInfo.InvariantCulture, out weight);
                                inputEventArgs.Weight = weight;
                            }
                            else if (templateInfos[i].Content.Contains("barcode", StringComparison.OrdinalIgnoreCase))
                            {
                                inputEventArgs.Barcode = ControlCharacterRegex.Replace(split[i], "");
                            }
                        }
                    }
                    else
                    {
                        OnExternalDataException(new Exception($"split.Length:{split.Length} DataTemplate:{_contentInputSettingsDto.DataTemplate.Count(c => c.Type != 2)},判断不相等"));
                    }
                    OnContentInputReceived(inputEventArgs);
                }
                catch (Exception exception)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{exception}");
                }
            }
        }

        /// <summary>
        /// 体积输入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TcpCommunicationOnCommunication(object? sender, CommunicationInfo e)
        {
            if (!string.IsNullOrEmpty(e.Content) && e.Type == CommunicationType.Receive)
            {
                float length = 0, width = 0, height = 0, volume = 0;
                var split = e.Content.Split(_volumeSettingsDto.Separator);
                if (split.Length == _volumeSettingsDto.DataTemplate.Count(c => c.Type != 2))
                {
                    var templateInfos = _volumeSettingsDto.DataTemplate.Where(w => w.Type != 2).ToList();
                    for (var i = 0; i < split.Length; i++)
                    {
                        if (templateInfos[i].Content.Contains("length", StringComparison.OrdinalIgnoreCase))
                        {
                            float.TryParse(split[i], NumberStyles.Float, CultureInfo.InvariantCulture, out length);
                        }
                        else if (templateInfos[i].Content.Contains("width", StringComparison.OrdinalIgnoreCase))
                        {
                            float.TryParse(split[i], NumberStyles.Float, CultureInfo.InvariantCulture, out width);
                        }
                        else if (templateInfos[i].Content.Contains("height", StringComparison.OrdinalIgnoreCase))
                        {
                            float.TryParse(split[i], NumberStyles.Float, CultureInfo.InvariantCulture, out height);
                        }
                        else if (templateInfos[i].Content.Contains("volume", StringComparison.OrdinalIgnoreCase))
                        {
                            float.TryParse(split[i], NumberStyles.Float, CultureInfo.InvariantCulture, out volume);
                        }
                    }
                }
                else
                {
                    OnExternalDataException(new Exception($"split.Length:{split.Length} DataTemplate:{_volumeSettingsDto.DataTemplate.Count(c => c.Type != 2)},判断不相等"));
                }

                PendingVolumeRequest? pendingRequest;
                do
                {
                    _volumeBarCodeItems.TryDequeue(out pendingRequest);
                } while (pendingRequest is not null &&
                         Volatile.Read(ref pendingRequest.Cancelled) != 0);
                OnVolumeReceived(new ExternalVolumeInputEventArgs()
                {
                    BarCode = pendingRequest?.Barcode ?? string.Empty,
                    Length = length,
                    Width = width,
                    Height = height,
                    Volume = volume,
                    ReceiveSource = string.Empty,
                    ReceiveTime = DateTime.Now
                });
                //取出模板组合

                //判断消息是否符合模板
                //从队列取出条码
                //取出长宽高，组合成消息模板
                //事件通知
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default)
        {
            await Task.Yield();
            _volumeBarCodeItems.Clear();
            try
            {
                if (_volumeSettingsDto.IsUseExternalVolumeInput)
                {
                    if (_volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType == VolumeRequesterType.Tcp)
                    {
                        _tcpVolumeInput.Communication -= TcpCommunicationOnCommunication;
                        if (_tcpVolumeInput.ConnectionStatus == ConnectionStatus.Connected)
                        {
                            _tcpVolumeInput.Close();
                        }
                    }
                    else if (_volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType ==
                             VolumeRequesterType.SerialPort)
                    {
                    }
                }

                if (_contentInputSettingsDto.IsUseTcpInput)
                {
                    _tcpContentInput.Communication -= TcpContentInputOnCommunication;
                    if (_tcpContentInput.ConnectionStatus == ConnectionStatus.Connected)
                    {
                        _tcpContentInput.Close();
                    }
                }
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            catch (Exception e)
            {
                OnExternalDataException(e);
                return new KeyValuePair<bool, string>(false, $"{e.Message}");
            }
        }

        protected virtual void OnExternalDataException(Exception e)
        {
            ExternalDataException?.Invoke(this, e);
        }

        private sealed class PendingVolumeRequest(string barcode)
        {
            /// <summary>
            /// 与本次体积请求关联的条码。
            /// </summary>
            public string Barcode { get; } = barcode;
            /// <summary>
            /// 标记发送失败后不应再用于匹配回包的请求。
            /// </summary>
            public int Cancelled;
        }

        protected virtual void OnDataSourceEnabled(ExternalDataSourceEventArgs e)
        {
            DataSourceEnabled?.Invoke(this, e);
        }

        protected virtual void OnVolumeReceived(ExternalVolumeInputEventArgs e)
        {
            VolumeReceived?.Invoke(this, e);
        }

        protected virtual void OnContentInputReceived(ExternalContentInputEventArgs e)
        {
            ContentInputReceived?.Invoke(this, e);
        }
    }
}
