using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using TouchSocket.Core;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using JayTom.Dws.Plugin.Scale.ScaleValueParameters;

namespace JayTom.Dws.Plugin.Scale.DynamicScale {

    public class DefaultDynamicScale : IDynamicScale {
        private static readonly Regex WeightPattern = new(
            @"-?\b\d+(?:\.\d+)?\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private System.IO.Ports.SerialPort? _serialPort { get; set; }
        private BaseTcpOperations? TcpOperations { get; set; }
        private DefaultDynamicScaleValueParameters _defaultDynamicScaleValueParameters = new();
        private BaseScaleConnectParam _baseScaleConnectParam = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public void Dispose() {
            if (_serialPort?.IsOpen == true) {
                _serialPort?.Close();
            }
            // _serialPort?.Dispose();
            _serialPort = null;
            TcpOperations?.Close();
            TcpOperations = null;
        }

        public WeightAdditionalProperties WeightAdditionalProperties { get; set; } = new();
        public ScaleWeightFormat WeightFormat { get; set; }

        public event EventHandler<float>? StabledWeight;

        public event EventHandler<WeightChangedEventArgs>? WeightStabilized;

        public event EventHandler<string>? Received;

        public event EventHandler<IScale>? Connected;

        public event EventHandler<IScale>? Disconnected;

        public event EventHandler<Exception>? Excepted;

        public bool SetWeightCalculationParameters(BaseScaleValueParameters param) {
            if (param is DefaultDynamicScaleValueParameters parameters) {
                _defaultDynamicScaleValueParameters = parameters;
                return true;
            }
            return false;
        }

        public ScaleStatus Status { get; private set; } = ScaleStatus.NotConnected;

        public async Task<bool> Connect(BaseScaleConnectParam connectParam) {
            _baseScaleConnectParam = connectParam;
            try {
                if (_baseScaleConnectParam.Mode == ScaleCommunicationMode.SerialPort) {
                    if (_serialPort?.IsOpen == true) {
                        return true;
                    }
                    if (_baseScaleConnectParam.SerialPortInfo is not null) {
                        if (_serialPort is null) {
                            _serialPort = new System.IO.Ports.SerialPort() {
                                BaudRate = _baseScaleConnectParam.SerialPortInfo.BaudRate,
                                DataBits = _baseScaleConnectParam.SerialPortInfo.DataBits,
                                Parity = _baseScaleConnectParam.SerialPortInfo.Parity,
                                StopBits = _baseScaleConnectParam.SerialPortInfo.StopBits,
                                PortName = _baseScaleConnectParam.SerialPortInfo.PortName,
                            };
                            NLog.LogManager.GetCurrentClassLogger().Error("新的连接");
                            //注册事件
                            _serialPort.DataReceived += async delegate (object sender, SerialDataReceivedEventArgs args) {
                                //读数据

                                try {
                                    await Task.Delay(150);
                                    await _semaphore.WaitAsync();
                                    if (sender is System.IO.Ports.SerialPort { IsOpen: true, BytesToRead: > 0 } port && _serialPort.IsOpen) {
                                        string receivedData;
                                        if (WeightFormat == ScaleWeightFormat.Ascii) {
                                            // 读取接收到的数据
                                            receivedData = port.ReadExisting() /*.Trim().Replace(" ", string.Empty)*/;

                                            var match = WeightPattern.Match(receivedData);
                                            // 提取重量值
                                            if (match.Success) {
                                                var weight = match.Value;
                                                var tryParse = float.TryParse(weight, out var result);
                                                if (tryParse) {
                                                    //输出重量
                                                    OnStabledWeight(result);
                                                    //输出重量原文
                                                    OnWeightStabilized(new WeightChangedEventArgs() {
                                                        Format = WeightFormat,
                                                        FormattedWeight = result,
                                                        OriginalContent = receivedData,
                                                        Type = WeightType.Static
                                                    });
                                                }
                                            }
                                        }
                                        else {
                                            //接收十六进制内容
                                            // 接收数据存储的字节数组
                                            var buffer = new byte[port.BytesToRead];
                                            // 读取数据到字节数组
                                            port.Read(buffer, 0, buffer.Length);
                                            // 将字节数组转换为十六进制表示
                                            receivedData = BitConverter.ToString(buffer).Replace("-", " ");
                                            if (!string.IsNullOrEmpty(receivedData)) {
                                                var weightFromHex = ExtractWeightFromHex(receivedData);
                                                //输出重量
                                                OnStabledWeight(weightFromHex);
                                                //输出重量原文
                                                OnWeightStabilized(new WeightChangedEventArgs() {
                                                    Format = WeightFormat,
                                                    FormattedWeight = weightFromHex,
                                                    OriginalContent = receivedData,
                                                    Type = WeightType.Static
                                                });
                                            }
                                        }

                                        OnReceived(receivedData);
                                    }
                                }
                                catch (Exception e) {
                                    OnExcepted(e);
                                }
                                finally {
                                    _semaphore.Release();
                                }
                            };
                            _serialPort.Disposed += delegate {
                                OnDisconnected(this);
                            };
                            _serialPort.ErrorReceived += delegate (object sender, SerialErrorReceivedEventArgs args) {
                                OnExcepted(new Exception(args.ToString()));
                            };
                        }
                        else {
                            _serialPort.BaudRate = _baseScaleConnectParam.SerialPortInfo.BaudRate;
                            _serialPort.DataBits = _baseScaleConnectParam.SerialPortInfo.DataBits;
                            _serialPort.Parity = _baseScaleConnectParam.SerialPortInfo.Parity;
                            _serialPort.StopBits = _baseScaleConnectParam.SerialPortInfo.StopBits;
                            _serialPort.PortName = _baseScaleConnectParam.SerialPortInfo.PortName;
                            NLog.LogManager.GetCurrentClassLogger().Error("其他连接");
                        }

                        _serialPort.Open();
                        if (_serialPort.IsOpen) {
                            OnConnected(this);
                            NLog.LogManager.GetCurrentClassLogger().Error("连接成功");
                            return true;
                        }
                    }
                }
                else if (_baseScaleConnectParam is { Mode: ScaleCommunicationMode.Tcp, TcpConnectInfo: not null }) {
                    if (TcpOperations?.ConnectionStatus == ConnectionStatus.Connected) {
                        return true;
                    }

                    if (TcpOperations is null) {
                        TcpOperations = new BaseTcpOperations(new TouchSocketTcpClient(), new TouchSocketTcpServer());
                        TcpOperations.Communication += (sender, info) => {
                            if (info.Type == CommunicationType.Receive) {
                                string receivedData;
                                if (WeightFormat == ScaleWeightFormat.Ascii) {
                                    // 读取接收到的数据
                                    receivedData = info.Content /*.Trim().Replace(" ", string.Empty)*/;

                                    var match = WeightPattern.Match(receivedData);
                                    // 提取重量值
                                    if (match.Success) {
                                        var weight = match.Value;
                                        var tryParse = float.TryParse(weight, out var result);
                                        if (!weight.Contains(".")) {
                                            result = result / 1000;
                                        }
                                        if (tryParse) {
                                            //输出重量
                                            OnStabledWeight(result);
                                            //输出重量原文
                                            OnWeightStabilized(new WeightChangedEventArgs() {
                                                Format = WeightFormat,
                                                FormattedWeight = result,
                                                OriginalContent = receivedData,
                                                Type = WeightType.Static
                                            });
                                        }
                                    }
                                }
                                else {
                                    receivedData = info.Content;
                                    if (!string.IsNullOrEmpty(receivedData)) {
                                        var weightFromHex = ExtractWeightFromHex(receivedData);
                                        //输出重量
                                        OnStabledWeight(weightFromHex);
                                        //输出重量原文
                                        OnWeightStabilized(new WeightChangedEventArgs() {
                                            Format = WeightFormat,
                                            FormattedWeight = weightFromHex,
                                            OriginalContent = receivedData,
                                            Type = WeightType.Static
                                        });
                                    }
                                }

                                OnReceived(receivedData);
                            }
                        };
                        TcpOperations.Connected += (sender, s) => {
                            OnConnected(this);
                            NLog.LogManager.GetCurrentClassLogger().Error("连接成功");
                        };
                        TcpOperations.Disconnected += (sender, s) => {
                            OnDisconnected(this);
                        };
                        TcpOperations.ConnectionException += (sender, s) => {
                            OnExcepted(new Exception(s));
                        };
                        TcpOperations.Exception += (sender, exception) => {
                            OnExcepted(exception);
                        };
                    }

                    //定义事件
                    var isConnect = _baseScaleConnectParam.TcpConnectInfo.ConnectionMode switch {
                        TcpConnectionMode.Client => await TcpOperations.Connect(
                            _baseScaleConnectParam.TcpConnectInfo.ClientConfig.IpAddress,
                            _baseScaleConnectParam.TcpConnectInfo.ClientConfig.Port, ConnectionType.Client, 1000,
                            _baseScaleConnectParam.TcpConnectInfo.DataFormat),
                        TcpConnectionMode.Server => await TcpOperations.Connect(
                            _baseScaleConnectParam.TcpConnectInfo.ServerConfig.IpAddress,
                            _baseScaleConnectParam.TcpConnectInfo.ServerConfig.Port, ConnectionType.Server, 1000,
                            _baseScaleConnectParam.TcpConnectInfo.DataFormat),
                        _ => false
                    };
                    return isConnect;
                }
            }
            catch (Exception e) {
                Dispose();
                OnExcepted(e);
                OnDisconnected(this);
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return false;
            }
            return false;
        }

        public static float ExtractWeightFromHex(string input) {
            // 移除所有空格
            try {
                var hexString = input.Replace(" ", "");
                if (hexString.Length == 16) {
                    var weightSubstring = hexString.Substring(4, 10);
                    var processedWeight = string.Concat(weightSubstring.Where((ch, index) => index % 2 == 1));
                    int.TryParse(processedWeight, out var weightInt);
                    return weightInt / 100f;
                }
            }
            catch {
                // ignored
            }

            return 0;
        }

        protected virtual void OnStabledWeight(float e) {
            //使用附加属性
            if (WeightAdditionalProperties.IsUseActualWeightConversionRate) {
                //使用重量转换率
                e = (float)(e * (WeightAdditionalProperties.WeightConversionRate / 100));
            }
            if (WeightAdditionalProperties.IsUseAppendedWeight) {
                //追加重量
                e = (float)(e + WeightAdditionalProperties.AppendedWeightValue);
            }
            e = (float)Math.Round(e, _defaultDynamicScaleValueParameters.DecimalPlaces);
            if (WeightAdditionalProperties.IsUseFixedWeight) {
                //固定重量输出
                e = (float)WeightAdditionalProperties.FixedWeightValue;
            }

            StabledWeight?.Invoke(this, e);
        }

        protected virtual void OnReceived(string e) {
            Received?.Invoke(this, e);
        }

        protected virtual void OnConnected(IScale e) {
            Status = ScaleStatus.Running;
            Connected?.Invoke(this, e);
        }

        protected virtual void OnDisconnected(IScale e) {
            Status = ScaleStatus.Disconnected;
            Disconnected?.Invoke(this, e);
        }

        protected virtual void OnExcepted(Exception e) {
            Excepted?.Invoke(this, e);
        }

        protected virtual void OnWeightStabilized(WeightChangedEventArgs e) {
            WeightStabilized?.Invoke(this, e);
        }
    }
}
