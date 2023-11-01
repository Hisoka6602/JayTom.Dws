using Polly;
using System;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols;

namespace JayTom.Dws.Client.Service.Sorting {

    public class DefaultInventoryManagementService : IInventoryManagementService {
        private readonly ISortingSerialPort _sortingSerialPort;
        private readonly ISortingTcp _sortingTcp;
        private readonly IConfigRepository _configRepository;
        private CommunicationsSettingsDto _communicationsSettingsDto = new();
        private IDeviceCommunicationProtocol? _deviceCommunicationProtocol = null;
        private SemaphoreSlim _semaphore = new(1);
        private ConcurrentQueue<string> _replyContentQueue = new();

        public bool IsConnected { get; private set; }

        public event EventHandler<CommunicationMessageInfo>? CommunicationInfoEvent;

        public event EventHandler<Exception>? CommunicationExceptionEvent;

        public event EventHandler<DeviceDecodeResult>? ReceivedInstructionsEvent;

        public event EventHandler<Exception>? HeartbeatError;

        public event EventHandler<ExceptionEventArgs>? SendError;

        public DefaultInventoryManagementService(ISortingSerialPort sortingSerialPort,
            ISortingTcp sortingTcp, IConfigRepository configRepository) {
            _sortingSerialPort = sortingSerialPort;
            _sortingTcp = sortingTcp;
            _configRepository = configRepository;
            //事件
            _sortingSerialPort.Disconnected += delegate (object? sender, ISortingSerialPort port) {
                IsConnected = false;
            };
            _sortingSerialPort.ConnectionChanged += delegate (object? sender, ISortingSerialPort port) {
                IsConnected = true;
            };
            _sortingSerialPort.ErrorOccurred +=
                delegate (object? sender, Communication.SerialComm.ExceptionEventArgs args) {
                    OnCommunicationExceptionEvent(args.Exception);
                };
            _sortingSerialPort.DataReceived += delegate (object? sender, MessageEventArgs args) {
                //接收的数据
                var deviceDecodeResult = _deviceCommunicationProtocol?.DecodeData(args.AsciiMessage);
                if (deviceDecodeResult != null) {
                    OnReceivedInstructionsEvent(deviceDecodeResult);
                }
                _replyContentQueue.Enqueue(args.AsciiMessage);
            };
            _sortingSerialPort.HeartbeatError += delegate (object? sender, Exception exception) {
                OnHeartbeatError(exception);
            };
            _sortingSerialPort.SendError += delegate (object? sender, Communication.SerialComm.ExceptionEventArgs args) {
                OnSendError(new ExceptionEventArgs() {
                    ExceptionMessage = args.Exception.Message
                });
            };
            //TCP
            _sortingTcp.Exception += delegate (object? sender, Exception exception) {
                OnCommunicationExceptionEvent(exception);
            };
            _sortingTcp.Disconnected += delegate (object? sender, string s) {
                IsConnected = false;
            };
            _sortingTcp.Connected += delegate (object? sender, string s) {
                IsConnected = true;
            };
            _sortingTcp.ConnectionException += delegate (object? sender, string s) {
                IsConnected = false;
            };
            _sortingTcp.Communication += delegate (object? sender, CommunicationInfo info) {
                if (info.Type == CommunicationType.Receive) {
                    //接收消息
                    //接收的数据
                    var deviceDecodeResult = _deviceCommunicationProtocol?.DecodeData(info.Content);
                    if (deviceDecodeResult != null) {
                        OnReceivedInstructionsEvent(deviceDecodeResult);
                    }
                    _replyContentQueue.Enqueue(info.Content);
                }
            };
            _sortingTcp.HeartbeatError += delegate (object? sender, Exception exception) {
                OnHeartbeatError(exception);
            };
            _sortingTcp.SendError += delegate (object? sender, Exception exception) {
                OnSendError(new ExceptionEventArgs() {
                    ExceptionMessage = exception.Message
                });
            };
        }

        public async void SendInstructions(object tag, List<string> instructions, TimeSpan interval, InstructionsAttach attach) {
            //判断是否连接，如果未连接则连接
            if (!IsConnected) {
                await Connect();
            }
            if (_communicationsSettingsDto.Type == CommunicationsType.SerialPort) {
                //串口
                if (_sortingSerialPort.Status == SortingSerialPortStatus.Running &&
                    instructions?.Any() == true) {
                    foreach (var instruction in instructions) {
                        //效验协议

                        var message = instruction;
                        if (_deviceCommunicationProtocol is not null) {
                            message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                instruction, attach);
                        }
                        _sortingSerialPort.Send(message);
                        OnCommunicationInfoEvent(new CommunicationMessageInfo() {
                            BarCode = attach.BarCode,
                            Content = message,
                            ExitName = attach.ExitName,
                            FormatType = (FormatType)_sortingSerialPort.FormatType,
                            Guid = attach.Guid,
                            Time = DateTime.Now,
                            Timestamp = attach.Timestamp,
                            Type = CommunicationType.Send
                        });
                        await Task.Delay(interval);
                    }
                }
                else {
                    OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                }
            }
            else if (_communicationsSettingsDto.Type == CommunicationsType.TCP) {
                //tcp
                if (_sortingTcp.ConnectionStatus == ConnectionStatus.Connected &&
                    instructions?.Any() == true) {
                    foreach (var instruction in instructions) {
                        //效验协议

                        var message = instruction;
                        if (_deviceCommunicationProtocol is not null) {
                            message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                instruction, attach);
                        }

                        var sendMessage = await _sortingTcp.SendMessage(HexStringToByteArray(message));
                        OnCommunicationInfoEvent(new CommunicationMessageInfo() {
                            BarCode = attach.BarCode,
                            Content = message,
                            ExitName = attach.ExitName,
                            FormatType = _sortingTcp.FormatType,
                            Guid = attach.Guid,
                            Time = DateTime.Now,
                            Timestamp = attach.Timestamp,
                            Type = CommunicationType.Send
                        });
                        if (!sendMessage) {
                            OnCommunicationExceptionEvent(new Exception("发送失败!"));
                        }
                        await Task.Delay(interval);
                    }
                }
                else {
                    OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                }
            }
        }

        public async void SendInstructions(object tag, List<SortingInstructionInfoModel> instructions, TimeSpan interval, InstructionsAttach attach) {
            //判断是否连接，如果未连接则连接
            if (!IsConnected) {
                await Connect();
            }
            if (_communicationsSettingsDto.Type == CommunicationsType.SerialPort) {
                //串口
                if (_sortingSerialPort.Status == SortingSerialPortStatus.Running &&
                    instructions?.Any() == true) {
                    foreach (var instruction in instructions) {
                        if (_communicationsSettingsDto.MachineReplyInfo.IsVerificationEnabled) {
                            var retryPolicy = Policy.HandleResult<bool>(result => !result)
                                .RetryAsync(_communicationsSettingsDto.MachineReplyInfo.MaxRetryCount, (a, b) => {
                                });

                            var executeAsync = await retryPolicy.ExecuteAsync(async () => {
                                //效验协议
                                var message = instruction.Instruction;
                                if (_deviceCommunicationProtocol is not null) {
                                    message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                        instruction.Instruction, attach);
                                }
                                _sortingSerialPort.Send(message);
                                OnCommunicationInfoEvent(new CommunicationMessageInfo() {
                                    BarCode = attach.BarCode,
                                    Content = message,
                                    ExitName = attach.ExitName,
                                    FormatType = (FormatType)_sortingSerialPort.FormatType,
                                    Guid = attach.Guid,
                                    Time = DateTime.Now,
                                    Timestamp = attach.Timestamp,
                                    Type = CommunicationType.Send
                                });
                                return await WaitForReply(instruction.ReplyContent,
                                    TimeSpan.FromMilliseconds(_communicationsSettingsDto.MachineReplyInfo.Timeout));
                            });
                            if (!executeAsync) {
                                OnCommunicationExceptionEvent(new Exception("未收到应答信息!"));
                                break;
                            }
                        }
                        else {
                            //不使用应答
                            var message = instruction.Instruction;
                            if (_deviceCommunicationProtocol is not null) {
                                message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                    instruction.Instruction, null);
                            }
                            _sortingSerialPort.Send(message);
                            OnCommunicationInfoEvent(new CommunicationMessageInfo() {
                                BarCode = attach.BarCode,
                                Content = message,
                                ExitName = attach.ExitName,
                                FormatType = (FormatType)_sortingSerialPort.FormatType,
                                Guid = attach.Guid,
                                Time = DateTime.Now,
                                Timestamp = attach.Timestamp,
                                Type = CommunicationType.Send
                            });
                        }
                        await Task.Delay(interval);
                    }
                }
                else {
                    OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                }
            }
            else if (_communicationsSettingsDto.Type == CommunicationsType.TCP) {
                //tcp
                if (_sortingTcp.ConnectionStatus == ConnectionStatus.Connected &&
                    instructions?.Any() == true) {
                    foreach (var instruction in instructions) {
                        //使用应答
                        if (_communicationsSettingsDto.MachineReplyInfo.IsVerificationEnabled) {
                            var retryPolicy = Policy.HandleResult<bool>(result => !result)
                               .RetryAsync(_communicationsSettingsDto.MachineReplyInfo.MaxRetryCount, (a, b) => {
                               });

                            var executeAsync = await retryPolicy.ExecuteAsync(async () => {
                                //效验协议
                                var message = instruction.Instruction;
                                if (_deviceCommunicationProtocol is not null) {
                                    message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                        instruction.Instruction, null);
                                }

                                var sendMessage = await _sortingTcp.SendMessage(HexStringToByteArray(message));
                                OnCommunicationInfoEvent(new CommunicationMessageInfo() {
                                    BarCode = attach.BarCode,
                                    Content = message,
                                    ExitName = attach.ExitName,
                                    FormatType = _sortingTcp.FormatType,
                                    Guid = attach.Guid,
                                    Time = DateTime.Now,
                                    Timestamp = attach.Timestamp,
                                    Type = CommunicationType.Send
                                });
                                if (sendMessage) {
                                    return await WaitForReply(instruction.ReplyContent,
                                        TimeSpan.FromMilliseconds(_communicationsSettingsDto.MachineReplyInfo.Timeout));
                                }
                                return false;
                            });
                            if (!executeAsync) {
                                OnCommunicationExceptionEvent(new Exception("未收到应答信息!"));
                                break;
                            }
                        }
                        else {
                            //不使用应答

                            //效验协议
                            var message = instruction.Instruction;
                            if (_deviceCommunicationProtocol is not null) {
                                message = _deviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                    instruction.Instruction, null);
                            }

                            var sendMessage = await _sortingTcp.SendMessage(HexStringToByteArray(message));
                            OnCommunicationInfoEvent(new CommunicationMessageInfo() {
                                BarCode = attach.BarCode,
                                Content = message,
                                ExitName = attach.ExitName,
                                FormatType = _sortingTcp.FormatType,
                                Guid = attach.Guid,
                                Time = DateTime.Now,
                                Timestamp = attach.Timestamp,
                                Type = CommunicationType.Send
                            });
                            if (!sendMessage) {
                                OnCommunicationExceptionEvent(new Exception("发送失败!"));
                                break;
                            }
                        }
                        await Task.Delay(interval);
                    }
                }
                else {
                    OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                }
            }
        }

        public async Task<KeyValuePair<bool, string>> Connect(CancellationToken token = default) {
            try {
                //从数据库读取_communicationsSettingsDto
                var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("CommunicationsSettings"), token);
                _communicationsSettingsDto =
                    JsonConvert.DeserializeObject<CommunicationsSettingsDto>(configInfoModel?.Value ?? string.Empty) ?? new CommunicationsSettingsDto();
                //效验协议
                _deviceCommunicationProtocol = null;
                //读协议
                if (_communicationsSettingsDto.Protocol == CommunicationProtocol.Wxkc) {
                    //无限科创协议
                    _deviceCommunicationProtocol = new WxkcCommunicationProtocol();
                }
                else if (_communicationsSettingsDto.Protocol == CommunicationProtocol.JT_ST) {
                    //江腾窄带协议
                    _deviceCommunicationProtocol = new JtstCommunicationProtocol();
                }
                //其他协议

                if (_communicationsSettingsDto.Type == CommunicationsType.SerialPort) {
                    //连接
                    _sortingSerialPort.Connect(_communicationsSettingsDto.SerialPortSettingsInfo.PortName,
                        _communicationsSettingsDto.SerialPortSettingsInfo.BaudRate,
                        _communicationsSettingsDto.SerialPortSettingsInfo.DataBits,
                        _communicationsSettingsDto.SerialPortSettingsInfo.Parity,
                        _communicationsSettingsDto.SerialPortSettingsInfo.StopBits,
                        (SortingSerialPortFormat)_communicationsSettingsDto.SerialPortSettingsInfo
                            .DataFormat);
                    //心跳包
                    if (_communicationsSettingsDto.HeartbeatInfo is { IsHeartbeatEnabled: true, IsHeartbeatActive: true }) {
                        _sortingSerialPort.StartHeartbeat(_communicationsSettingsDto.HeartbeatInfo.HeartbeatData, TimeSpan.FromMilliseconds(_communicationsSettingsDto.HeartbeatInfo.HeartbeatInterval));
                    }
                }
                else if (_communicationsSettingsDto.Type == CommunicationsType.TCP) {
                    if (_communicationsSettingsDto.TcpSettingsInfo.ConnectionMode ==
                        TcpConnectionMode.Server) {
                        await _sortingTcp.Connect(
                            _communicationsSettingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                            _communicationsSettingsDto.TcpSettingsInfo.ServerConfig.Port,
                            ConnectionType.Server, 2000, FormatType.Hex, token);
                    }
                    else {
                        await _sortingTcp.Connect(
                            _communicationsSettingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                            _communicationsSettingsDto.TcpSettingsInfo.ClientConfig.Port,
                            ConnectionType.Client, 2000, FormatType.Hex, token);
                    }
                    //心跳包
                    if (_communicationsSettingsDto.HeartbeatInfo is { IsHeartbeatEnabled: true, IsHeartbeatActive: true }) {
                        _sortingTcp.StartHeartbeat(_communicationsSettingsDto.HeartbeatInfo.HeartbeatData, TimeSpan.FromMilliseconds(_communicationsSettingsDto.HeartbeatInfo.HeartbeatInterval));
                    }
                }

                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            catch (Exception e) {
                OnCommunicationExceptionEvent(e);
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public async Task<KeyValuePair<bool, string>> Disconnect(CancellationToken token = default) {
            try {
                //断开全部通讯
                //串口通讯
                if (_sortingSerialPort.Status == SortingSerialPortStatus.Running) {
                    _sortingSerialPort.Dispose();
                    await Task.Delay(600, token);
                }
                //Tcp通讯
                if (_sortingTcp.ConnectionStatus == ConnectionStatus.Connected) {
                    _sortingTcp.Dispose();
                    await Task.Delay(600, token);
                }
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            catch (Exception e) {
                OnCommunicationExceptionEvent(e);
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        protected virtual async void OnCommunicationExceptionEvent(Exception e) {
            await Task.Yield();
            CommunicationExceptionEvent?.Invoke(this, e);
        }

        protected virtual async void OnReceivedInstructionsEvent(DeviceDecodeResult e) {
            await Task.Yield();
            ReceivedInstructionsEvent?.Invoke(this, e);
        }

        protected virtual async void OnHeartbeatError(Exception e) {
            await Task.Yield();
            HeartbeatError?.Invoke(this, e);
        }

        protected virtual async void OnSendError(ExceptionEventArgs e) {
            await Task.Yield();
            SendError?.Invoke(this, e);
        }

        private byte[] HexStringToByteArray(string hexString) {
            hexString = hexString.Replace(" ", ""); // 移除空格

            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < hexString.Length; i += 2) {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return bytes;
        }

        private async Task<bool> WaitForReply(string replyContent, TimeSpan timeOut) {
            await Task.Yield();
            var startTime = DateTime.Now;
            do {
                var tryDequeue = _replyContentQueue.TryDequeue(out var result);
                if (tryDequeue &&
                    replyContent.Equals(result)) {
                    return true;
                }
                await Task.Delay(5);
            } while (DateTime.Now.Subtract(startTime) < timeOut);
            return false;
        }

        protected virtual async void OnCommunicationInfoEvent(CommunicationMessageInfo e) {
            await Task.Yield();
            CommunicationInfoEvent?.Invoke(this, e);
        }
    }
}