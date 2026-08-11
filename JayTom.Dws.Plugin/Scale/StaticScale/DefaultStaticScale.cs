using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.SerialPort;
using System.Runtime.Serialization;
using System.Collections.Concurrent;
using System.Globalization;
using JayTom.Dws.Plugin.WeighingScale;
using JayTom.Dws.Plugin.Scale.ScaleValueParameters;
using static JayTom.Dws.Plugin.WeighingScale.WeighingScale;

namespace JayTom.Dws.Plugin.Scale.StaticScale {

    public class DefaultStaticScale : IStaticScale {
        private System.IO.Ports.SerialPort? _serialPort { get; set; }
        private readonly Queue<decimal> _weightQueue = new();
        private readonly ConcurrentQueue<string> _character = new();
        /// <summary>
        /// 串口数据到达信号，避免接收工作器空闲轮询。
        /// </summary>
        private readonly SemaphoreSlim _receiveSignal = new(0);
        private DateTime StabledTime { get; set; } = DateTime.Now;
        private CancellationTokenSource _tokenSource = new();
        private DefaultStaticScaleValueParameters _defaultStaticScaleValueParameters = new();
        private BaseScaleConnectParam _baseScaleConnectParam = new();
        private readonly System.Threading.Lock _receiveLock = new();
        private Task? _receiveTask;
        private Task? _sendTask;

        //增加一个稳定重量推送间隔
        private bool _isZeroed = true;

        private DateTime _lasTime = DateTime.Now;

        /// <summary>
        /// 稳定重量累计次数
        /// </summary>
        private readonly Queue<decimal> _oldStableWeight = new();

        private decimal _lastweight;

        //private static int _stableWeightCount = 0;

        public void Dispose() {
            _tokenSource.Cancel();
            try {
                var tasks = new List<Task>(2);
                if (_receiveTask is not null) {
                    tasks.Add(_receiveTask);
                }
                if (_sendTask is not null) {
                    tasks.Add(_sendTask);
                }
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) {
            }

            if (_serialPort?.IsOpen == true) {
                _serialPort.Close();
            }
            _serialPort?.Dispose();
            _serialPort = null;
            _character.Clear();
            _weightQueue.Clear();
            _oldStableWeight.Clear();
            Status = ScaleStatus.Disconnected;
        }

        public WeightAdditionalProperties WeightAdditionalProperties { get; set; } = new();
        public ScaleWeightFormat WeightFormat { get; set; } = ScaleWeightFormat.Ascii;

        public event EventHandler<decimal>? StabledWeight;

        public event EventHandler<WeightChangedEventArgs>? WeightStabilized;

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

        public async Task<bool> Connect(BaseScaleConnectParam connectParam) {
            await Task.Yield();
            _baseScaleConnectParam = connectParam;
            if (_serialPort?.IsOpen == true) {
                return true;
            }
            try {
                if (_baseScaleConnectParam.SerialPortInfo is not null) {
                    if (_serialPort is null) {
                        _serialPort = new System.IO.Ports.SerialPort() {
                            BaudRate = _baseScaleConnectParam.SerialPortInfo.BaudRate,
                            DataBits = _baseScaleConnectParam.SerialPortInfo.DataBits,
                            Parity = _baseScaleConnectParam.SerialPortInfo.Parity,
                            StopBits = _baseScaleConnectParam.SerialPortInfo.StopBits,
                            PortName = _baseScaleConnectParam.SerialPortInfo.PortName,
                        };
                        //注册事件
                        _serialPort.DataReceived += delegate (object sender, SerialDataReceivedEventArgs args) {
                            try {
                                string? receivedData = null;
                                lock (_receiveLock) {
                                    if (_serialPort?.IsOpen == true &&
                                        sender is System.IO.Ports.SerialPort { IsOpen: true, BytesToRead: > 0 } port &&
                                        !_tokenSource.IsCancellationRequested) {
                                        if (WeightFormat == ScaleWeightFormat.Ascii) {
                                            receivedData = port.ReadExisting();
                                        }
                                        else {
                                            var buffer = new byte[port.BytesToRead];
                                            var bytesRead = port.Read(buffer, 0, buffer.Length);
                                            receivedData = HexDataFormatter.Format(buffer.AsSpan(0, bytesRead));
                                        }
                                    }
                                }

                                if (!string.IsNullOrEmpty(receivedData)) {
                                    _character.Enqueue(receivedData);
                                    _receiveSignal.Release();
                                    OnReceived(receivedData);
                                }
                            }
                            catch (Exception e) {
                                OnExcepted(e);
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
                    }

                    _serialPort.Open();
                    if (_serialPort.IsOpen) {
                        _tokenSource.Cancel();
                        _tokenSource = new();
                        _receiveTask = Task.Run(() => ProcessReceivedData(_tokenSource.Token));
                        if (_defaultStaticScaleValueParameters.AccessMode == WeightAccessMode.QuestionAnswer) {
                            _sendTask = Task.Run(() => ProcessSending(_tokenSource.Token));
                        }
                        OnConnected(this);
                        _isZeroed = true;
                        return true;
                    }
                }
            }
            catch (Exception e) {
                Dispose();
                OnExcepted(e);
                OnDisconnected(this);
                return false;
            }
            return false;
        }

        private async Task ProcessReceivedData(CancellationToken token) {
            if (!HasValidFrameConfiguration(out var configurationError)) {
                OnExcepted(new InvalidOperationException(configurationError));
                return;
            }

            var dataBuffer = string.Empty;
            while (!token.IsCancellationRequested) {
                //根据标识符取出指定长度的字符
                await _receiveSignal.WaitAsync(token).ConfigureAwait(false);
                var dequeue = _character.TryDequeue(out var buffResult);
                if (dequeue) {
                    dataBuffer += buffResult;
                    while (true) {
                        var indexOf = dataBuffer.IndexOf(
                            _defaultStaticScaleValueParameters.Identifier,
                            StringComparison.Ordinal);
                        var identifierPosition =
                            indexOf - _defaultStaticScaleValueParameters.IdentifierPosition;
                        if (indexOf < 0 || identifierPosition < 0 ||
                            dataBuffer.Length < identifierPosition +
                            _defaultStaticScaleValueParameters.CharacterLength) {
                            var maximumBufferedCharacters = Math.Max(
                                _defaultStaticScaleValueParameters.CharacterLength * 4,
                                4096);
                            if (dataBuffer.Length > maximumBufferedCharacters) {
                                dataBuffer = dataBuffer[^maximumBufferedCharacters..];
                            }
                            break;
                        }

                            //取出完整一条
                            var substring = dataBuffer.Substring(identifierPosition, _defaultStaticScaleValueParameters.CharacterLength);
                            dataBuffer = dataBuffer[
                                (identifierPosition + _defaultStaticScaleValueParameters.CharacterLength)..];
                            //取出整数
                            var integer = substring.Substring(_defaultStaticScaleValueParameters.IntegerStartPosition, _defaultStaticScaleValueParameters.IntegerEndPosition - _defaultStaticScaleValueParameters.IntegerStartPosition + 1);
                            integer = _defaultStaticScaleValueParameters.IsReversed ? new string([.. integer.Reverse()]) : integer;
                            //取出小数
                            var _decimal = substring.Substring(_defaultStaticScaleValueParameters.DecimalStartPosition, _defaultStaticScaleValueParameters.DecimalEndPosition - _defaultStaticScaleValueParameters.DecimalStartPosition + 1);
                            _decimal = _defaultStaticScaleValueParameters.IsReversed ? new string([.. _decimal.Reverse()]) : _decimal;
                            //组合
                            var data = $"{integer}.{_decimal}";
                            ProcessDataPackage(data);
                        }
                }
            }
        }

        private void ProcessDataPackage(string data) {
            try {
                if (decimal.TryParse(
                        data.Replace(" ", string.Empty),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var result)) {
                    _weightQueue.Enqueue(result);
                    if (_weightQueue.Count > 0 && _weightQueue.Count > _defaultStaticScaleValueParameters.BalanceCount) {
                        //删除一个
                        _weightQueue.Dequeue();
                    }

                    if (_weightQueue.Count >= _defaultStaticScaleValueParameters.BalanceCount) {
                        var maximumWeight = _weightQueue.Max();
                        var minimumWeight = _weightQueue.Min();
                        if (maximumWeight - minimumWeight <= _defaultStaticScaleValueParameters.BalanceQty &&
                            maximumWeight <= _defaultStaticScaleValueParameters.MaxWeight &&
                            minimumWeight >= _defaultStaticScaleValueParameters.MinWeight) {
                            StabledTime = DateTime.Now;
                            var weight = _weightQueue.GroupBy(x => x)
                                .OrderByDescending(g => g.Count())
                                .Select(g => g.Key)
                                .FirstOrDefault();
                            if (_isZeroed   /*|| Math.Abs(_lastweight - weight) > _defaultStaticScaleValueParameters.BalanceQty * 2*/) {
                                OnStabledWeight(weight);
                                //返回原文
                                OnWeightStabilized(new WeightChangedEventArgs() {
                                    Format = WeightFormat,
                                    FormattedWeight = weight,
                                    OriginalContent = string.Join(",", _weightQueue.ToList()),
                                    Type = WeightType.Static
                                });
                                _serialPort?.DiscardInBuffer();
                                _isZeroed = false;
                                _lastweight = weight;
                                _oldStableWeight.Clear();
                                _lasTime = DateTime.Now;
                            }
                            /*else if (_oldStableWeight.Count > 2 && _oldStableWeight.Max() - _oldStableWeight.Min() <= _defaultStaticScaleValueParameters.BalanceQty) {
                                OnWeightCleared(new WeightChangedEventArgs() {
                                    Format = WeightFormat,
                                    FormattedWeight = 0,
                                    OriginalContent = string.Join(",", _weightQueue.ToList()),
                                    Type = WeightType.Static
                                });
                                _oldStableWeight.Clear();
                                _serialPort?.DiscardInBuffer();
                                _isZeroed = true;
                            }
                            else if (_oldStableWeight.Count > 2) {
                                _oldStableWeight.Dequeue();
                            }
                            else {
                                _oldStableWeight.Enqueue(weight);
                            }*/

                            _weightQueue.Clear();
                        }
                        else if (!_isZeroed && (_weightQueue.All(item => item == 0) ||
                                           (_weightQueue.Count >= 2 && _weightQueue.Reverse().Take(2).All(w => w < _defaultStaticScaleValueParameters.MinWeight)) ||
                                           (_weightQueue.Count >= 3 && _weightQueue.Reverse().Take(3).All(w => w < _lastweight * 0.85m)) ||
                                           (_oldStableWeight.Count > 0 && _oldStableWeight.All(a => a < _defaultStaticScaleValueParameters.MinWeight)) ||
                                           (_oldStableWeight.Count > 2 && _oldStableWeight.Max() - _oldStableWeight.Min() <= _defaultStaticScaleValueParameters.BalanceQty))) {
                            OnWeightCleared(new WeightChangedEventArgs() {
                                Format = WeightFormat,
                                FormattedWeight = 0,
                                OriginalContent = string.Join(",", _weightQueue.ToList()),
                                Type = WeightType.Static
                            });
                            _weightQueue.Clear();
                            _oldStableWeight.Clear();
                            _serialPort?.DiscardInBuffer();
                            _isZeroed = true;
                        }
                    }

                    if (WeightAdditionalProperties.IsUseMergedWeightTimeout &&
                        _weightQueue.Count >= _defaultStaticScaleValueParameters.BalanceCount) {
                        if (DateTime.Now.Subtract(StabledTime).TotalMilliseconds > WeightAdditionalProperties.MergedWeightTimeout &&
                            _weightQueue.Max() - _weightQueue.Min() <= _defaultStaticScaleValueParameters.BalanceQty &&
                            _weightQueue.Max() <= _defaultStaticScaleValueParameters.MaxWeight && _weightQueue.Min() >= _defaultStaticScaleValueParameters.MinWeight) {
                            StabledTime = DateTime.Now;
                            var weight = _weightQueue.GroupBy(x => x)
                                .OrderByDescending(g => g.Count())
                                .Select(g => g.Key)
                                .FirstOrDefault();
                            OnStabledWeight(weight);
                            OnWeightStabilized(new WeightChangedEventArgs() {
                                Format = WeightFormat,
                                FormattedWeight = weight,
                                OriginalContent = string.Join(",", _weightQueue.ToList()),
                                Type = WeightType.Static
                            });
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

        private async Task ProcessSending(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                var sendInterval = _defaultStaticScaleValueParameters.DataInterval;
                await Task.Delay(
                    sendInterval > TimeSpan.Zero ? sendInterval : TimeSpan.FromMilliseconds(1),
                    token);
                if (_serialPort?.IsOpen == true) {
                    if (_defaultStaticScaleValueParameters.SendingFormat == ScaleWeightFormat.Ascii) {
                        _serialPort?.WriteLine(_defaultStaticScaleValueParameters.SendingContent);
                    }
                    else {
                        var toByteArray = HexStringToByteArray(_defaultStaticScaleValueParameters.SendingContent);
                        _serialPort?.Write(toByteArray, 0, toByteArray.Length);
                    }
                }
            }
        }

        public event EventHandler<decimal>? CurrentWeight;

        public event EventHandler<WeightChangedEventArgs>? WeightCleared;

        protected virtual void OnCurrentWeight(decimal e) {
            CurrentWeight?.Invoke(this, e);
        }

        protected virtual void OnConnected(IScale e) {
            Status = ScaleStatus.Running;
            Connected?.Invoke(this, e);
        }

        protected virtual void OnDisconnected(IScale e) {
            Status = ScaleStatus.Disconnected;
            Disconnected?.Invoke(this, e);
        }

        protected virtual void OnStabledWeight(decimal e) {
            //使用附加属性
            if (WeightAdditionalProperties.IsUseActualWeightConversionRate) {
                //使用重量转换率
                e = (decimal)(e * (WeightAdditionalProperties.WeightConversionRate / 100));
            }
            if (WeightAdditionalProperties.IsUseAppendedWeight) {
                //追加重量
                e = (decimal)(e + WeightAdditionalProperties.AppendedWeightValue);
            }
            //判断输出的小数位数
            var position = _defaultStaticScaleValueParameters.DecimalEndPosition -
                _defaultStaticScaleValueParameters.DecimalStartPosition + 1;
            position = position > 3 ? 3 : position;
            e = (decimal)Math.Round(e, position);

            if (WeightAdditionalProperties.IsUseFixedWeight) {
                //固定重量输出
                e = (decimal)WeightAdditionalProperties.FixedWeightValue;
            }

            StabledWeight?.Invoke(this, e);
        }

        protected virtual void OnExcepted(Exception e) {
            Excepted?.Invoke(this, e);
        }

        protected virtual void OnReceived(string e) {
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

        /// <summary>
        /// 校验静态秤数据帧配置是否可用于安全解析。
        /// </summary>
        private bool HasValidFrameConfiguration(out string error) {
            var parameters = _defaultStaticScaleValueParameters;
            if (parameters.BalanceCount <= 0) {
                error = "稳定重量采样数量必须大于0";
                return false;
            }
            if (parameters.CharacterLength <= 0 ||
                string.IsNullOrEmpty(parameters.Identifier) ||
                parameters.IdentifierPosition < 0 ||
                parameters.IntegerStartPosition < 0 ||
                parameters.DecimalStartPosition < 0 ||
                parameters.IntegerEndPosition < parameters.IntegerStartPosition ||
                parameters.DecimalEndPosition < parameters.DecimalStartPosition ||
                parameters.IntegerEndPosition >= parameters.CharacterLength ||
                parameters.DecimalEndPosition >= parameters.CharacterLength) {
                error = "静态秤数据帧配置无效";
                return false;
            }

            error = string.Empty;
            return true;
        }

        protected virtual void OnWeightStabilized(WeightChangedEventArgs e) {
            WeightStabilized?.Invoke(this, e);
        }

        protected virtual void OnWeightCleared(WeightChangedEventArgs e) {
            WeightCleared?.Invoke(this, e);
        }
    }
}
