using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.SerialPort {

    public class BaseSerialPort : ISerialPort {
        private System.IO.Ports.SerialPort _serialPort;
        private SerialPortFormat _formatType;
        private SemaphoreSlim _semaphore = new(1);
        private SemaphoreSlim _sendSlim = new(1);

        public BaseSerialPort(System.IO.Ports.SerialPort serialPort) {
            _serialPort = serialPort;
        }

        public SerialPortFormat FormatType {
            get => _formatType;
            private set => _formatType = value;
        }

        public SerialPortStatus Status { get; private set; }

        public event EventHandler<ISerialPort>? ConnectionChanged;

        public event EventHandler<MessageEventArgs>? DataReceived;

        public event EventHandler<ISerialPort>? Disconnected;

        public event EventHandler<ExceptionEventArgs>? ErrorOccurred;

        public event EventHandler<ExceptionEventArgs>? SendError;

        public event EventHandler<CommunicationInfo>? Communication;

        public bool Connect(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits,
            SerialPortFormat dataFormat) {
            FormatType = dataFormat;
            _serialPort ??= new System.IO.Ports.SerialPort();
            if (_serialPort.IsOpen == true) {
                return true;
            }

            try {
                _serialPort.BaudRate = baudRate;
                _serialPort.DataBits = dataBits;
                _serialPort.Parity = parity;
                _serialPort.StopBits = stopBits;
                _serialPort.PortName = portName;

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
                            if (FormatType == SerialPortFormat.Ascii) {
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

        public async void Send(string message) {
            try {
                await _sendSlim.WaitAsync();
                if (_serialPort?.IsOpen == true) {
                    if (FormatType == SerialPortFormat.Ascii) {
                        _serialPort?.WriteLine(message);
                    }
                    else {
                        var toByteArray = HexStringToByteArray(message);
                        _serialPort?.Write(toByteArray, 0, toByteArray.Length);
                    }

                    OnCommunication(new CommunicationInfo() {
                        Content = message,
                        FormatType = (FormatType)FormatType,
                        Type = CommunicationType.Send,
                        Time = DateTime.Now
                    });
                }
            }
            catch (Exception e) {
                OnSendError(new ExceptionEventArgs(e));
            }
            finally {
                await Task.Delay(10);
                _sendSlim.Release();
            }
        }

        public async void Send(byte[] message) {
            try {
                await _sendSlim.WaitAsync();
                if (_serialPort?.IsOpen == true) {
                    var replace = BitConverter.ToString(message).Replace("-", " ");
                    if (FormatType == SerialPortFormat.Ascii) {
                        _serialPort?.WriteLine(replace);
                    }
                    else {
                        _serialPort?.Write(message, 0, message.Length);
                    }

                    OnCommunication(new CommunicationInfo() {
                        Content = replace,
                        FormatType = (FormatType)FormatType,
                        Type = CommunicationType.Send,
                        Time = DateTime.Now
                    });
                }
            }
            catch (Exception e) {
                OnSendError(new ExceptionEventArgs(e));
            }
            finally {
                await Task.Delay(10);
                _sendSlim.Release();
            }
        }

        public void Dispose() {
            _serialPort?.Close();
        }

        private static byte[] HexStringToByteArray(string hexString) {
            hexString = hexString.Replace(" ", ""); // 移除空格

            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < hexString.Length; i += 2) {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return bytes;
        }

        protected virtual async void OnConnectionChanged(ISerialPort e) {
            Status = SerialPortStatus.Running;
            await Task.Yield();
            ConnectionChanged?.Invoke(this, e);
        }

        protected virtual async void OnDataReceived(MessageEventArgs e) {
            await Task.Yield();
            OnCommunication(new CommunicationInfo() {
                Content = e.AsciiMessage,
                FormatType = (FormatType)FormatType,
                Type = CommunicationType.Receive,
                Time = DateTime.Now
            });
            DataReceived?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(ISerialPort e) {
            Status = SerialPortStatus.Disconnected;
            await Task.Yield();
            Disconnected?.Invoke(this, e);
        }

        protected virtual async void OnErrorOccurred(ExceptionEventArgs e) {
            await Task.Yield();
            ErrorOccurred?.Invoke(this, e);
        }

        protected virtual async void OnSendError(ExceptionEventArgs e) {
            await Task.Yield();
            SendError?.Invoke(this, e);
        }

        protected virtual async void OnCommunication(CommunicationInfo e) {
            await Task.Yield();
            Communication?.Invoke(this, e);
        }
    }
}