using System;
using System.Linq;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace JayTom.Dws.Client.Service.Sorting.Communication.SerialComm {

    public class SortingSerialPort : ISortingSerialPort {
        private System.IO.Ports.SerialPort? _serialPort;
        private SortingSerialPortFormat _dataFormat = SortingSerialPortFormat.Ascii;
        private SemaphoreSlim _semaphore = new(1);
        private static Task? _heartbeatThread;
        private static CancellationTokenSource? _cancellationTokenSource;
        private readonly ConcurrentQueue<string> _heartbeatQueue = new();

        public void Dispose() {
            StopHeartbeat();
            if (_serialPort?.IsOpen == true) {
                _serialPort?.Close();
            }
            _serialPort?.Dispose();
            _serialPort = null;
            OnDisconnected(this);
        }

        public SortingSerialPortFormat FormatType { get; private set; }
        public SortingSerialPortStatus Status { get; private set; } = SortingSerialPortStatus.NotConnected;

        public event EventHandler<ISortingSerialPort>? ConnectionChanged;

        public event EventHandler<MessageEventArgs>? DataReceived;

        public event EventHandler<ISortingSerialPort>? Disconnected;

        public event EventHandler<ExceptionEventArgs>? ErrorOccurred;

        public event EventHandler<ExceptionEventArgs>? SendError;

        public event EventHandler<Exception>? HeartbeatError;

        public bool Connect(string portName, int baudRate, int dataBits, Parity parity,
            StopBits stopBits, SortingSerialPortFormat dataFormat) {
            FormatType = dataFormat;
            if (_serialPort?.IsOpen == true) {
                return true;
            }

            try {
                if (_serialPort is null) {
                    _serialPort = new System.IO.Ports.SerialPort() {
                        BaudRate = baudRate,
                        DataBits = dataBits,
                        Parity = parity,
                        StopBits = stopBits,
                        PortName = portName,
                    };
                    _dataFormat = dataFormat;
                    //注册事件
                    _serialPort.Disposed += delegate {
                        OnDisconnected(this);
                    };
                    _serialPort.ErrorReceived += delegate (object sender, SerialErrorReceivedEventArgs args) {
                        OnErrorOccurred(new ExceptionEventArgs(new Exception(args.ToString())));
                    };
                    _serialPort.DataReceived += async delegate (object sender, SerialDataReceivedEventArgs args) {
                        await Task.Delay(150);
                        try {
                            await _semaphore.WaitAsync();
                            if (sender is System.IO.Ports.SerialPort { IsOpen: true, BytesToRead: > 0 } port &&
                                _serialPort.IsOpen) {
                                string receivedData;
                                if (_dataFormat == SortingSerialPortFormat.Ascii) {
                                    // 读取接收到的数据
                                    receivedData = port.ReadExisting().Trim().Replace(" ", string.Empty);
                                    OnDataReceived(new MessageEventArgs() {
                                        AsciiMessage = receivedData,
                                    });
                                }
                                else {
                                    //接收十六进制内容
                                    // 接收数据存储的字节数组
                                    var buffer = new byte[port.BytesToRead];

                                    // 读取数据到字节数组
                                    port.Read(buffer, 0, buffer.Length);

                                    // 将字节数组转换为十六进制表示
                                    receivedData = BitConverter.ToString(buffer).Replace("-", "");
                                    OnDataReceived(new MessageEventArgs() {
                                        AsciiMessage = receivedData,
                                        HexMessage = buffer
                                    });
                                }
                            }
                        }
                        finally {
                            _semaphore.Release();
                        }
                    };
                }

                _serialPort.Open();
                if (_serialPort.IsOpen) {
                    OnConnectionChanged(this);
                    return true;
                }
            }
            catch (Exception e) {
                Dispose();
                OnErrorOccurred(new ExceptionEventArgs(e));
            }

            return false;
        }

        public void Send(string message) {
            try {
                if (_serialPort?.IsOpen == true) {
                    if (_dataFormat == SortingSerialPortFormat.Ascii) {
                        _serialPort?.WriteLine(message);
                    }
                    else {
                        var toByteArray = HexStringToByteArray(message);
                        _serialPort?.Write(toByteArray, 0, toByteArray.Length);
                    }
                }
            }
            catch (Exception e) {
                OnSendError(new ExceptionEventArgs(e));
            }
        }

        public void StartHeartbeat(string heartbeatData, TimeSpan interval) {
            //开启心跳线程
            if (_heartbeatThread is null) {
                _cancellationTokenSource = new CancellationTokenSource();
                _heartbeatThread = Task.Run(async () => {
                    while (!_cancellationTokenSource.IsCancellationRequested) {
                        await Task.Delay(interval);

                        if (_heartbeatQueue.Any()) {
                            //异常
                            OnHeartbeatError(new Exception("心跳包未接收到回应!"));
                        }

                        if (_serialPort?.IsOpen == true) {
                            _heartbeatQueue.Clear();
                            _heartbeatQueue.Enqueue(heartbeatData);
                            Send(heartbeatData);
                        }
                    }
                });
            }
        }

        public async void StopHeartbeat() {
            //停止心跳线程
            _cancellationTokenSource?.Cancel();
            await Task.Delay(200);
            if (_heartbeatThread != null) {
                await _heartbeatThread;
                _heartbeatThread?.Dispose();
            }

            _heartbeatThread = null;
        }

        protected virtual async void OnConnectionChanged(ISortingSerialPort e) {
            Status = SortingSerialPortStatus.Running;
            await Task.Yield();
            ConnectionChanged?.Invoke(this, e);
        }

        protected virtual async void OnDataReceived(MessageEventArgs e) {
            var tryDequeue = _heartbeatQueue.TryDequeue(out var data);
            if (tryDequeue && !string.IsNullOrEmpty(data)) {
                if (!e.AsciiMessage.Equals(data)) {
                    _heartbeatQueue.Enqueue(data);
                }
            }
            await Task.Yield();
            DataReceived?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(ISortingSerialPort e) {
            Status = SortingSerialPortStatus.Disconnected;
            await Task.Yield();
            Disconnected?.Invoke(this, e);
        }

        protected virtual async void OnErrorOccurred(ExceptionEventArgs e) {
            await Task.Yield();
            ErrorOccurred?.Invoke(this, e);
        }

        private static byte[] HexStringToByteArray(string hexString) {
            hexString = hexString.Replace(" ", ""); // 移除空格

            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < hexString.Length; i += 2) {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return bytes;
        }

        protected virtual async void OnHeartbeatError(Exception e) {
            await Task.Yield();
            HeartbeatError?.Invoke(this, e);
        }

        protected virtual async void OnSendError(ExceptionEventArgs e) {
            await Task.Yield();
            SendError?.Invoke(this, e);
        }
    }
}