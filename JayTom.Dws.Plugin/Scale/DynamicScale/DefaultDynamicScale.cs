using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using TouchSocket.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Plugin.Scale.ScaleValueParameters;

namespace JayTom.Dws.Plugin.Scale.DynamicScale {
    public class DefaultDynamicScale : IDynamicScale {
        private System.IO.Ports.SerialPort? _serialPort { get; set; }
        private DefaultDynamicScaleValueParameters _defaultDynamicScaleValueParameters = new();
        private BaseScaleConnectParam _baseScaleConnectParam = new();
        private SemaphoreSlim _semaphore = new(1);

        public void Dispose() {
            if (_serialPort?.IsOpen == true) {
                _serialPort?.Close();
            }
            _serialPort?.Dispose();
            _serialPort = null;
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

        public bool Connect(BaseScaleConnectParam connectParam) {
            _baseScaleConnectParam = connectParam;
            if (_serialPort?.IsOpen == true) {
                return true;
            }

            try {
                if (_serialPort is null) {
                    _serialPort = new System.IO.Ports.SerialPort() {
                        BaudRate = _baseScaleConnectParam.BaudRate,
                        DataBits = _baseScaleConnectParam.DataBits,
                        Parity = _baseScaleConnectParam.Parity,
                        StopBits = _baseScaleConnectParam.StopBits,
                        PortName = _baseScaleConnectParam.PortName,
                    };
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

                                    NLog.LogManager.GetCurrentClassLogger().Error($"接收到的重量内容:{receivedData}");

                                    // 定义匹配重量的正则表达式模式(不考虑负数)
                                    const string pattern = @"\b\d+\.\d+\b";
                                    var regex = new Regex(pattern);
                                    // 在输入字符串中查找匹配项
                                    var match = regex.Match(receivedData);
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
                    _serialPort.BaudRate = _baseScaleConnectParam.BaudRate;
                    _serialPort.DataBits = _baseScaleConnectParam.DataBits;
                    _serialPort.Parity = _baseScaleConnectParam.Parity;
                    _serialPort.StopBits = _baseScaleConnectParam.StopBits;
                    _serialPort.PortName = _baseScaleConnectParam.PortName;
                }

                _serialPort.Open();
                if (_serialPort.IsOpen) {
                    OnConnected(this);
                    return true;
                }
            }
            catch (Exception e) {
                Dispose();
                OnExcepted(e);
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
                    return weightInt / 1000f;
                }
            }
            catch {
                // ignored
            }

            return 0;
        }

        protected virtual async void OnStabledWeight(float e) {
            await Task.Yield();

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

        protected virtual async void OnReceived(string e) {
            await Task.Yield();
            Received?.Invoke(this, e);
        }

        protected virtual async void OnConnected(IScale e) {
            await Task.Yield();
            Status = ScaleStatus.Running;
            Connected?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(IScale e) {
            await Task.Yield();
            Status = ScaleStatus.Disconnected;
            Disconnected?.Invoke(this, e);
        }

        protected virtual async void OnExcepted(Exception e) {
            await Task.Yield();
            Excepted?.Invoke(this, e);
        }

        protected virtual async void OnWeightStabilized(WeightChangedEventArgs e) {
            await Task.Yield();
            WeightStabilized?.Invoke(this, e);
        }
    }
}