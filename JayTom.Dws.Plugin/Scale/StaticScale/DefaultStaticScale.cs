using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.SerialPort;
using System.Runtime.Serialization;
using System.Collections.Concurrent;
using JayTom.Dws.Plugin.WeighingScale;
using JayTom.Dws.Plugin.Scale.ScaleValueParameters;
using static JayTom.Dws.Plugin.WeighingScale.WeighingScale;

namespace JayTom.Dws.Plugin.Scale.StaticScale {

    public class DefaultStaticScale : IStaticScale {
        private System.IO.Ports.SerialPort? _serialPort { get; set; }
        private readonly ConcurrentQueue<float> _weightQueue = new();
        private readonly ConcurrentQueue<string> _character = new();
        private DateTime StabledTime { get; set; } = DateTime.Now;
        private CancellationTokenSource _tokenSource = new();
        private DefaultStaticScaleValueParameters _defaultStaticScaleValueParameters = new();
        private BaseScaleConnectParam _baseScaleConnectParam = new();
        private SemaphoreSlim _semaphore = new(1);

        public async void Dispose() {
            _tokenSource?.Cancel();
            await Task.Delay(500);
            if (_serialPort?.IsOpen == true) {
                _serialPort?.Close();
            }
            _serialPort?.Dispose();
            _serialPort = null;
        }

        public WeightAdditionalProperties WeightAdditionalProperties { get; set; } = new();
        public ScaleWeightFormat WeightFormat { get; set; } = ScaleWeightFormat.Ascii;

        public event EventHandler<float>? StabledWeight;

        public event EventHandler<string>? Received;

        public event EventHandler<IScale>? Connected;

        public event EventHandler<IScale>? Disconnected;

        public event EventHandler<Exception>? Excepted;

        public bool SetWeightCalculationParameters(BaseScaleValueParameters param) {
            if (param is DefaultStaticScaleValueParameters parameters) {
                _defaultStaticScaleValueParameters = parameters;
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
                        try {
                            await Task.Delay(_defaultStaticScaleValueParameters.DataInterval);
                            await _semaphore.WaitAsync();
                            if (_serialPort?.IsOpen == true) {
                                if (sender is System.IO.Ports.SerialPort { IsOpen: true, BytesToRead: > 0 } port && !_tokenSource.IsCancellationRequested) {
                                    string receivedData;
                                    if (WeightFormat == ScaleWeightFormat.Ascii) {
                                        // 读取接收到的数据
                                        receivedData = port.ReadExisting() /*.Trim().Replace(" ", string.Empty)*/;
                                    }
                                    else {
                                        //接收十六进制内容
                                        // 接收数据存储的字节数组
                                        var buffer = new byte[port.BytesToRead];

                                        // 读取数据到字节数组
                                        port.Read(buffer, 0, buffer.Length);

                                        // 将字节数组转换为十六进制表示
                                        receivedData = BitConverter.ToString(buffer).Replace("-", "");
                                    }

                                    _character.Enqueue(receivedData);
                                    // 添加到接收数据缓冲区
                                    //receivedDataBuffer += receivedData;

                                    OnReceived(receivedData);
                                }
                            }
                        }
                        catch (TaskCanceledException) { }
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
                    //注册转换事件
                    _tokenSource?.Cancel();
                    _tokenSource = new();
                    Task.Factory.StartNew(ProcessReceivedData, TaskCreationOptions.LongRunning);
                    if (_defaultStaticScaleValueParameters.AccessMode == WeightAccessMode.QuestionAnswer) {
                        //问答式
                        Task.Factory.StartNew(ProcessSending, TaskCreationOptions.LongRunning);
                    }
                    OnConnected(this);
                    return true;
                }
            }
            catch (Exception e) {
                OnExcepted(e);
                return false;
            }
            return false;
        }

        private async void ProcessReceivedData() {
            var dataBuffer = string.Empty;
            while (!_tokenSource.Token.IsCancellationRequested) {
                //根据标识符取出指定长度的字符
                var dequeue = _character.TryDequeue(out var buffResult);
                if (dequeue) {
                    dataBuffer += buffResult;
                    var indexOf = dataBuffer.IndexOf(_defaultStaticScaleValueParameters.Identifier, StringComparison.Ordinal);
                    int identifierPosition = indexOf - _defaultStaticScaleValueParameters.IdentifierPosition;
                    if (identifierPosition >= 0) {
                        if (dataBuffer.Length >= (identifierPosition + _defaultStaticScaleValueParameters.CharacterLength)) {
                            //取出完整一条
                            var substring = dataBuffer.Substring(identifierPosition, _defaultStaticScaleValueParameters.CharacterLength);
                            if (substring.Length <= _defaultStaticScaleValueParameters.IntegerStartPosition ||
                                substring.Length <= _defaultStaticScaleValueParameters.IntegerEndPosition ||
                                substring.Length <= _defaultStaticScaleValueParameters.DecimalStartPosition ||
                                substring.Length <= _defaultStaticScaleValueParameters.DecimalEndPosition ||
                                _defaultStaticScaleValueParameters.IntegerEndPosition - _defaultStaticScaleValueParameters.IntegerStartPosition < 0 ||
                                _defaultStaticScaleValueParameters.DecimalEndPosition - _defaultStaticScaleValueParameters.DecimalStartPosition < 0) {
                                continue;
                            }
                            //取出整数
                            var _integer = substring.Substring(_defaultStaticScaleValueParameters.IntegerStartPosition, _defaultStaticScaleValueParameters.IntegerEndPosition - _defaultStaticScaleValueParameters.IntegerStartPosition + 1);
                            _integer = _defaultStaticScaleValueParameters.IsReversed ? new string(_integer.Reverse().ToArray()) : _integer;
                            //取出小数
                            var _decimal = substring.Substring(_defaultStaticScaleValueParameters.DecimalStartPosition, _defaultStaticScaleValueParameters.DecimalEndPosition - _defaultStaticScaleValueParameters.DecimalStartPosition + 1);
                            _decimal = _defaultStaticScaleValueParameters.IsReversed ? new string(_decimal.Reverse().ToArray()) : _decimal;
                            //组合
                            var data = $"{_integer}.{_decimal}";
                            ProcessDataPackage(data);
                            dataBuffer = string.Empty;
                        }
                    }
                }
                await Task.Delay(1);
            }
        }

        private void ProcessDataPackage(string data) {
            try {
                if (float.TryParse(data, out var result)) {
                    _weightQueue.Enqueue(result);
                    if (_weightQueue.Count > 0 && _weightQueue.Count > _defaultStaticScaleValueParameters.BalanceCount) {
                        //删除一个
                        _weightQueue.TryDequeue(out _);
                    }

                    if (_weightQueue.Max() - _weightQueue.Min() <= _defaultStaticScaleValueParameters.BalanceQty &&
                        _weightQueue.Max() <= _defaultStaticScaleValueParameters.MaxWeight && _weightQueue.Min() >= _defaultStaticScaleValueParameters.MinWeight) {
                        StabledTime = DateTime.Now;
                        OnStabledWeight(_weightQueue.GroupBy(x => x)
                            .OrderByDescending(g => g.Count())
                            .Select(g => g.Key)
                            .FirstOrDefault());
                        _weightQueue.Clear();
                    }

                    if (WeightAdditionalProperties.IsUseMergedWeightTimeout) {
                        if (DateTime.Now.Subtract(StabledTime).TotalMilliseconds > WeightAdditionalProperties.MergedWeightTimeout &&
                            _weightQueue.Max() - _weightQueue.Min() <= _defaultStaticScaleValueParameters.BalanceQty &&
                            _weightQueue.Max() <= _defaultStaticScaleValueParameters.MaxWeight && _weightQueue.Min() >= _defaultStaticScaleValueParameters.MinWeight) {
                            StabledTime = DateTime.Now;
                            OnStabledWeight(_weightQueue.GroupBy(x => x)
                                .OrderByDescending(g => g.Count())
                                .Select(g => g.Key)
                                .FirstOrDefault());
                            _weightQueue.Clear();
                        }
                    }

                    //计算时间返回
                    //返回实时
                    OnCurrentWeight(result);
                }
            }
            catch (Exception e) {
                OnExcepted(e);
            }
        }

        private async void ProcessSending() {
            while (!_tokenSource.Token.IsCancellationRequested) {
                await Task.Delay(20);
                if (_defaultStaticScaleValueParameters.SendingFormat == ScaleWeightFormat.Ascii) {
                    _serialPort?.WriteLine(_defaultStaticScaleValueParameters.SendingContent);
                }
                else {
                    var toByteArray = HexStringToByteArray(_defaultStaticScaleValueParameters.SendingContent);
                    _serialPort?.Write(toByteArray, 0, toByteArray.Length);
                }
            }
        }

        public event EventHandler<float>? CurrentWeight;

        protected virtual async void OnCurrentWeight(float e) {
            await Task.Yield();
            CurrentWeight?.Invoke(this, e);
        }

        protected virtual async void OnConnected(IScale e) {
            Status = ScaleStatus.Running;
            await Task.Yield();
            Connected?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(IScale e) {
            Status = ScaleStatus.Disconnected;
            await Task.Yield();
            Disconnected?.Invoke(this, e);
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
            //判断输出的小数位数
            var position = _defaultStaticScaleValueParameters.DecimalEndPosition -
                _defaultStaticScaleValueParameters.DecimalStartPosition + 1;
            position = position > 3 ? 3 : position;
            e = (float)Math.Round(e, position);

            if (WeightAdditionalProperties.IsUseFixedWeight) {
                //固定重量输出
                e = (float)WeightAdditionalProperties.FixedWeightValue;
            }

            StabledWeight?.Invoke(this, e);
        }

        protected virtual async void OnExcepted(Exception e) {
            await Task.Yield();
            Excepted?.Invoke(this, e);
        }

        protected virtual async void OnReceived(string e) {
            await Task.Yield();
            Received?.Invoke(this, e);
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