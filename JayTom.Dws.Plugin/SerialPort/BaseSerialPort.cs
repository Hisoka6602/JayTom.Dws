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
        private readonly System.IO.Ports.SerialPort _serialPort;
        private SerialPortFormat _formatType;
        private readonly object _receiveLock = new();
        private readonly object _sendLock = new();
        private bool _eventHandlersRegistered;

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
            if (_serialPort.IsOpen) {
                return true;
            }

            try {
                _serialPort.BaudRate = baudRate;
                _serialPort.DataBits = dataBits;
                _serialPort.Parity = parity;
                _serialPort.StopBits = stopBits;
                _serialPort.PortName = portName;

                if (!_eventHandlersRegistered) {
                    _serialPort.Disposed += delegate {
                        OnDisconnected(this);
                    };
                    _serialPort.ErrorReceived += delegate (object sender, SerialErrorReceivedEventArgs args) {
                        OnErrorOccurred(new ExceptionEventArgs(new Exception(args.ToString())));
                    };
                    _serialPort.DataReceived += delegate (object sender, SerialDataReceivedEventArgs args) {
                        try {
                            MessageEventArgs? message = null;
                            lock (_receiveLock) {
                                if (sender is System.IO.Ports.SerialPort { IsOpen: true, BytesToRead: > 0 } port &&
                                    _serialPort.IsOpen) {
                                    if (FormatType == SerialPortFormat.Ascii) {
                                        var receivedData = port.ReadExisting().Trim().Replace(" ", string.Empty);
                                        message = new MessageEventArgs {
                                            AsciiMessage = receivedData
                                        };
                                    }
                                    else {
                                        var buffer = new byte[port.BytesToRead];
                                        var bytesRead = port.Read(buffer, 0, buffer.Length);
                                        if (bytesRead != buffer.Length) {
                                            Array.Resize(ref buffer, bytesRead);
                                        }

                                        message = new MessageEventArgs {
                                            AsciiMessage = Convert.ToHexString(buffer),
                                            HexMessage = buffer
                                        };
                                    }
                                }
                            }

                            if (message is not null) {
                                OnDataReceived(message);
                            }
                        }
                        catch (Exception exception) {
                            OnErrorOccurred(new ExceptionEventArgs(exception));
                        }
                    };
                    _eventHandlersRegistered = true;
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
                lock (_sendLock) {
                    if (!_serialPort.IsOpen) {
                        return;
                    }

                    if (FormatType == SerialPortFormat.Ascii) {
                        _serialPort.WriteLine(message);
                    }
                    else {
                        var toByteArray = HexStringToByteArray(message);
                        _serialPort.Write(toByteArray, 0, toByteArray.Length);
                    }
                }

                OnCommunication(new CommunicationInfo {
                    Content = message,
                    FormatType = (FormatType)FormatType,
                    Type = CommunicationType.Send,
                    Time = DateTime.Now
                });
            }
            catch (Exception e) {
                OnSendError(new ExceptionEventArgs(e));
            }
        }

        public void Send(byte[] message) {
            try {
                string formattedMessage;
                lock (_sendLock) {
                    if (!_serialPort.IsOpen) {
                        return;
                    }

                    formattedMessage = BitConverter.ToString(message).Replace("-", " ");
                    if (FormatType == SerialPortFormat.Ascii) {
                        _serialPort.WriteLine(formattedMessage);
                    }
                    else {
                        _serialPort.Write(message, 0, message.Length);
                    }
                }

                OnCommunication(new CommunicationInfo {
                    Content = formattedMessage,
                    FormatType = (FormatType)FormatType,
                    Type = CommunicationType.Send,
                    Time = DateTime.Now
                });
            }
            catch (Exception e) {
                OnSendError(new ExceptionEventArgs(e));
            }
        }

        public void Dispose() {
            if (_serialPort.IsOpen) {
                _serialPort.Close();
            }
            Status = SerialPortStatus.Disconnected;
            _serialPort.Dispose();
        }

        private static byte[] HexStringToByteArray(string hexString) {
            return Convert.FromHexString(hexString.Replace(" ", string.Empty));
        }

        protected virtual void OnConnectionChanged(ISerialPort e) {
            Status = SerialPortStatus.Running;
            ConnectionChanged?.Invoke(this, e);
        }

        protected virtual void OnDataReceived(MessageEventArgs e) {
            OnCommunication(new CommunicationInfo {
                Content = e.AsciiMessage,
                FormatType = (FormatType)FormatType,
                Type = CommunicationType.Receive,
                Time = DateTime.Now
            });
            DataReceived?.Invoke(this, e);
        }

        protected virtual void OnDisconnected(ISerialPort e) {
            Status = SerialPortStatus.Disconnected;
            Disconnected?.Invoke(this, e);
        }

        protected virtual void OnErrorOccurred(ExceptionEventArgs e) {
            ErrorOccurred?.Invoke(this, e);
        }

        protected virtual void OnSendError(ExceptionEventArgs e) {
            SendError?.Invoke(this, e);
        }

        protected virtual void OnCommunication(CommunicationInfo e) {
            Communication?.Invoke(this, e);
        }

        public byte[] ConvertHexStringToByteArray(string hexString) {
            try {
                return HexStringToByteArray(hexString);
            }
            catch (Exception) {
                return Array.Empty<byte>();
            }
        }
    }
}
