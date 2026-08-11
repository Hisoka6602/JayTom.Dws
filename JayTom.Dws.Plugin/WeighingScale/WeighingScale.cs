using System.IO.Ports;
using System.Globalization;
using TouchSocket.Core;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Plugin.WeighingScale {

    public class WeighingScale : IWeighingScale {
        private ConnectInfo? _connectInfo { get; set; } = new();
        private System.IO.Ports.SerialPort? _serialPort { get; set; }
        private readonly Queue<decimal> _weightQueue = new();
        private readonly ConcurrentQueue<string> _character = new();
        /// <summary>
        /// 串口数据到达信号，避免接收工作器空闲轮询。
        /// </summary>
        private readonly SemaphoreSlim _receiveSignal = new(0);
        private CancellationTokenSource _tokenSource = new();
        private readonly System.Threading.Lock _receiveLock = new();
        private Task? _receiveTask;
        //private string receivedDataBuffer = string.Empty;

        //private const string DataEndMarker = "=";

        public DeviceStatus Status { get; private set; } = DeviceStatus.Uninitialized;
        public WeightCalculationParameters WeightCalculationParameters { get; private set; } = new();
        private DateTime _stabledTime { get; set; } = DateTime.Now;

        public bool Reconnect() {
            if (_connectInfo is not null &&
                _serialPort is not null) {
                try {
                    if (_serialPort.IsOpen) {
                        _serialPort.Close();
                    }
                    _serialPort.BaudRate = _connectInfo.BaudRate;
                    _serialPort.DataBits = _connectInfo.DataBits;
                    _serialPort.Parity = (Parity)_connectInfo.Parity;
                    _serialPort.StopBits = (StopBits)_connectInfo.StopBits;
                    _serialPort.PortName = _connectInfo.PortName;
                    _serialPort.Open();
                    if (_serialPort.IsOpen) {
                        OnReconnected(this);
                        return true;
                    }
                }
                catch (Exception e) {
                    OnExcepted(e);
                    return false;
                }
            }

            return false;
        }

        public bool Connect<T>(T connectParam) {
            if (connectParam is ConnectInfo info) {
                if (_serialPort?.IsOpen == true) {
                    return true;
                }
                try {
                    if (_serialPort is null) {
                        _serialPort = new System.IO.Ports.SerialPort() {
                            BaudRate = info.BaudRate,
                            DataBits = info.DataBits,
                            Parity = (Parity)info.Parity,
                            StopBits = (StopBits)info.StopBits,
                            PortName = info.PortName,
                        };
                        //注册事件
                        _serialPort.DataReceived += delegate (object sender, SerialDataReceivedEventArgs args) {
                            try {
                                string receivedData;
                                lock (_receiveLock) {
                                    var port = (System.IO.Ports.SerialPort)sender;
                                    receivedData = port.ReadExisting();
                                }
                                _character.Enqueue(receivedData);
                                _receiveSignal.Release();
                                OnReceived(receivedData);
                            }
                            catch (Exception e) {
                                OnExcepted(e);
                            }
                        };
                        _serialPort.Disposed += delegate (object? sender, EventArgs args) {
                            OnDisconnected(this);
                        };
                        _serialPort.ErrorReceived += delegate (object sender, SerialErrorReceivedEventArgs args) {
                            OnExcepted(new Exception(args.ToString()));
                        };
                    }
                    else {
                        _serialPort.BaudRate = info.BaudRate;
                        _serialPort.DataBits = info.DataBits;
                        _serialPort.Parity = (Parity)info.Parity;
                        _serialPort.StopBits = (StopBits)info.StopBits;
                        _serialPort.PortName = info.PortName;
                    }

                    _serialPort.Open();
                    if (_serialPort.IsOpen) {
                        _connectInfo = info;
                        _tokenSource.Cancel();
                        _tokenSource = new();
                        _receiveTask = Task.Run(() => ProcessReceivedData(_tokenSource.Token));
                        OnConnected(this);
                        return true;
                    }
                }
                catch (Exception e) {
                    OnExcepted(e);
                    return false;
                }
            }

            return false;
        }

        public void Dispose() {
            _tokenSource.Cancel();
            try {
                _receiveTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) {
            }
            _serialPort?.Close();
            _serialPort?.Dispose();
        }

        public bool Initialization() {
            Initialized?.Invoke(this, this);
            return true;
        }

        private async Task ProcessReceivedData(CancellationToken token) {
            var dataBuffer = string.Empty;
            while (!token.IsCancellationRequested) {
                //根据标识符取出指定长度的字符
                await _receiveSignal.WaitAsync(token).ConfigureAwait(false);
                var dequeue = _character.TryDequeue(out var buffResult);
                if (dequeue) {
                    dataBuffer += buffResult;
                    var characterLength = WeightCalculationParameters.CharacterLength;
                    var identifier = WeightCalculationParameters.Identifier;
                    if (characterLength <= 0 || string.IsNullOrEmpty(identifier)) {
                        dataBuffer = string.Empty;
                        continue;
                    }

                    while (true) {
                        var indexOf = dataBuffer.IndexOf(identifier, StringComparison.Ordinal);
                        var identifierPosition = indexOf - WeightCalculationParameters.IdentifierPosition;
                        if (indexOf < 0) {
                            var retainedLength = Math.Min(
                                dataBuffer.Length,
                                Math.Max(characterLength * 2, 256));
                            dataBuffer = dataBuffer[^retainedLength..];
                            break;
                        }
                        if (identifierPosition < 0) {
                            dataBuffer = dataBuffer[(indexOf + identifier.Length)..];
                            continue;
                        }
                        if (dataBuffer.Length < identifierPosition + characterLength) {
                            break;
                        }

                            //取出完整一条
                            var substring = dataBuffer.Substring(identifierPosition, characterLength);
                            dataBuffer = dataBuffer[(identifierPosition + characterLength)..];
                            if (substring.Length <= WeightCalculationParameters.IntegerStartPosition ||
                                substring.Length <= WeightCalculationParameters.IntegerEndPosition ||
                                substring.Length <= WeightCalculationParameters.DecimalStartPosition ||
                                substring.Length <= WeightCalculationParameters.DecimalEndPosition ||
                                WeightCalculationParameters.IntegerEndPosition - WeightCalculationParameters.IntegerStartPosition < 0 ||
                                WeightCalculationParameters.DecimalEndPosition - WeightCalculationParameters.DecimalStartPosition < 0) {
                                continue;
                            }
                            //取出整数
                            var _integer = substring.Substring(WeightCalculationParameters.IntegerStartPosition, WeightCalculationParameters.IntegerEndPosition - WeightCalculationParameters.IntegerStartPosition + 1);
                            _integer = WeightCalculationParameters.IsReversed ? new string([.. _integer.Reverse()]) : _integer;
                            //取出小数
                            var _decimal = substring.Substring(WeightCalculationParameters.DecimalStartPosition, WeightCalculationParameters.DecimalEndPosition - WeightCalculationParameters.DecimalStartPosition + 1);
                            _decimal = WeightCalculationParameters.IsReversed ? new string([.. _decimal.Reverse()]) : _decimal;
                            //组合
                            string data = $"{_integer}.{_decimal}";
                            ProcessDataPackage(data);
                    }
                }
                /*var indexOf = receivedDataBuffer.IndexOf(Identifier, StringComparison.Ordinal);
                if (IsReversed) {
                    //如果反转
                    var position = receivedDataBuffer.Reverse().ToString();
                }

                int endIndex = receivedDataBuffer.IndexOf(DataEndMarker, StringComparison.Ordinal);

                // 检查是否存在完整的数据包
                if (endIndex >= 0) {
                    // 提取完整的数据包
                    var completeDataPackage = receivedDataBuffer[..(endIndex + DataEndMarker.Length)];
                    // 处理完整的数据包
                    ProcessDataPackage(completeDataPackage);

                    // 从接收数据缓冲区中移除处理过的数据
                    receivedDataBuffer = receivedDataBuffer.Length > endIndex + DataEndMarker.Length
                        ? receivedDataBuffer[..(endIndex + DataEndMarker.Length)]
                        : string.Empty;
                    // receivedDataBuffer[(endIndex + DataEndMarker.Length)..]
                }
                else {
                    // 不存在完整的数据包，等待下一次数据到达
                    break;
                }*/
            }
        }

        private void ProcessDataPackage(string data) {
            try {
                if (decimal.TryParse(
                        data,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var result)) {
                    /*lock (_weightCapacity) {
                        _weightCapacity.Add(result);
                        if (_weightCapacity.Count > info.BalanceCount) {
                            _weightCapacity.RemoveAt(0);
                        }
                    }

                    if (_weightCapacity.Max() - _weightCapacity.Min() <= info.BalanceQty &&
                        _weightCapacity.Max() <= info.MaxWeight && _weightCapacity.Min() >= info.MinWeight) {
                        _stabledTime = DateTime.Now;
                        OnStabledWeight(_weightCapacity.Last());
                    }*/
                    _weightQueue.Enqueue(result);
                    var requiredBalanceCount = Math.Max(1, WeightCalculationParameters.BalanceCount);
                    if (_weightQueue.Count > requiredBalanceCount) {
                        //删除一个
                        _weightQueue.Dequeue();
                    }

                    if (_weightQueue.Count >= requiredBalanceCount) {
                        var minimumWeight = _weightQueue.Min();
                        var maximumWeight = _weightQueue.Max();
                        if (maximumWeight - minimumWeight <= WeightCalculationParameters.BalanceQty &&
                            maximumWeight <= WeightCalculationParameters.MaxWeight &&
                            minimumWeight >= WeightCalculationParameters.MinWeight) {
                            _stabledTime = DateTime.Now;
                            OnStabledWeight(_weightQueue.Last());
                        }
                    }
                    if (DateTime.Now.Subtract(_stabledTime).TotalMilliseconds > WeightCalculationParameters.Delay) {
                        _stabledTime = DateTime.Now;
                        OnStabledWeight(_weightQueue.GroupBy(x => x)
                            .OrderByDescending(g => g.Count())
                            .Select(g => g.Key)
                            .FirstOrDefault());
                    }
                    //计算时间返回
                    //返回实时
                    OnCurrentWeight(result);
                }
            }
            catch (Exception e) {
                OnExcepted(e);
            }
            /*try {
                //var trim = _serialPort.ReadExisting().Trim();
                if (!string.IsNullOrEmpty(WeightRegex)) {
                    //new Regex(@"^=\D*(\d+\.\d+)\D*$");
                    var regex = new Regex(WeightRegex);
                    if (regex.IsMatch(data)) {
                        var match = regex.Match(data);
                        if (match is { Success: true, Groups.Count: > 1 }) {
                            var value = match.Groups[1].Value;

                            //var value = match.Value; // 提取出匹配的浮点数部分
                            if (decimal.TryParse(value, out var result)) {
                                /*lock (_weightCapacity) {
                                    _weightCapacity.Add(result);
                                    if (_weightCapacity.Count > info.BalanceCount) {
                                        _weightCapacity.RemoveAt(0);
                                    }
                                }

                                if (_weightCapacity.Max() - _weightCapacity.Min() <= info.BalanceQty &&
                                    _weightCapacity.Max() <= info.MaxWeight && _weightCapacity.Min() >= info.MinWeight) {
                                    _stabledTime = DateTime.Now;
                                    OnStabledWeight(_weightCapacity.Last());
                                }#1#
                                _weightQueue.Enqueue(result);
                                if (_weightQueue.Count > 0 && _weightQueue.Count > BalanceCount) {
                                    //删除一个
                                    _weightQueue.TryDequeue(out _);
                                }

                                if (_weightQueue.Max() - _weightQueue.Min() <= BalanceQty &&
                                    _weightQueue.Max() <= MaxWeight && _weightQueue.Min() >= MinWeight) {
                                    _stabledTime = DateTime.Now;
                                    OnStabledWeight(_weightQueue.Last());
                                }
                                if (DateTime.Now.Subtract(_stabledTime).TotalMilliseconds > Delay) {
                                    _stabledTime = DateTime.Now;
                                    OnStabledWeight(_weightQueue.Last());
                                }
                                //计算时间返回
                                //返回实时
                                OnCurrentWeight(result);
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                OnExcepted(e);
            }*/
        }

        public event EventHandler<IDevice>? Initialized;

        public event EventHandler<IDevice>? Connected;

        public event EventHandler<IDevice>? Disconnected;

        public event EventHandler<IDevice>? Reconnected;

        public event EventHandler<Exception>? Excepted;

        public event EventHandler<decimal>? StabledWeight;

        public event EventHandler<decimal>? CurrentWeight;

        public event EventHandler<string>? Received;

        public bool SetWeightCalculationParameters<T>(T param) {
            if (param is WeightCalculationParameters parameters) {
                this.WeightCalculationParameters = parameters;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 连接参数
        /// </summary>
        public class ConnectInfo {
            public int BaudRate { get; set; }
            public int DataBits { get; set; }
            public int Parity { get; set; }
            public int StopBits { get; set; }
            public string PortName { get; set; } = string.Empty;
        }

        protected virtual void OnExcepted(Exception e) {
            Excepted?.Invoke(this, e);
        }

        protected virtual void OnConnected(IDevice e) {
            Status = DeviceStatus.Connected;
            Connected?.Invoke(this, e);
        }

        protected virtual void OnDisconnected(IDevice e) {
            Status = DeviceStatus.Disconnected;
            Disconnected?.Invoke(this, e);
        }

        protected virtual void OnReconnected(IDevice e) {
            Status = DeviceStatus.Connected;
            Reconnected?.Invoke(this, e);
        }

        protected virtual void OnCurrentWeight(decimal e) {
            CurrentWeight?.Invoke(this, e);
        }

        protected virtual void OnReceived(string e) {
            Received?.Invoke(this, e);
        }

        protected virtual void OnStabledWeight(decimal e) {
            StabledWeight?.Invoke(this, e);
        }
    }

    public class WeightCalculationParameters {

        /// <summary>
        /// 每条数据间隔时间(采样频率)
        /// </summary>
        public TimeSpan DataInterval { get; set; } = TimeSpan.FromMilliseconds(20);

        /// <summary>
        /// 是否反转
        /// </summary>
        public bool IsReversed { get; set; }

        /// <summary>
        /// 获取方式
        /// </summary>
        public WeightAccessMode AccessMode { get; set; } = WeightAccessMode.Readonly;

        /// <summary>
        /// 稳定个数
        /// </summary>
        public int BalanceCount { get; set; } = 10;

        /// <summary>
        /// 稳定精度(误差范围)
        /// </summary>
        public decimal BalanceQty { get; set; } = 0.002m;

        /// <summary>
        /// 最大稳定时间
        /// </summary>
        public int Delay { get; set; } = 300;

        /// <summary>
        /// 最小可用重量
        /// </summary>
        public decimal MinWeight { get; set; } = 0.002m;

        /// <summary>
        /// 最大可用重量
        /// </summary>
        public decimal MaxWeight { get; set; } = 30;

        /// <summary>
        /// 标识符
        /// </summary>
        public string Identifier { get; set; } = "=";

        /// <summary>
        /// 字符长度
        /// </summary>
        public int CharacterLength { get; set; } = 8;

        /// <summary>
        /// 标识符位置
        /// </summary>
        public int IdentifierPosition { get; set; } = 0;

        /// <summary>
        /// 整数起始位置
        /// </summary>
        public int IntegerStartPosition { get; set; }

        /// <summary>
        /// 整数结束位置
        /// </summary>
        public int IntegerEndPosition { get; set; }

        /// <summary>
        /// 小数起始位置
        /// </summary>
        public int DecimalStartPosition { get; set; }

        /// <summary>
        /// 小数结束位置
        /// </summary>
        public int DecimalEndPosition { get; set; }
    }
}
