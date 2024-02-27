using Polly;
using System;
using DryIoc;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Diagnostics;
using TouchSocket.Sockets;
using JayTom.Dws.Plugin.Tcp;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalLog;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using CommunicationType = JayTom.Dws.Plugin.Tcp.CommunicationType;
using JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Client.Service.Sorting {

    public class DefaultSortingConnectionService : ISortingConnectionService {
        private readonly ICommunicationConnectionConfigRepository _communicationConnectionConfigRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly ISortingInstructionBindingRepository _sortingInstructionBindingRepository;
        private readonly ISortingInstructionRepository _sortingInstructionRepository;
        private readonly ITcpConfigRepository _tcpConfigRepository;
        private ConcurrentDictionary<string, ConnectionInfo> _connectionInfos = new();
        private List<CommunicationConnectionConfigInfoModel> _connectionConfigInfoModels = new();
        private List<PackageExitDefinitionInfoModel> _packageExitDefinitionInfoModels = new();
        private List<SortingInstructionBindingInfoModel> _sortingInstructionBindingInfoModels = new();
        private List<SortingInstructionInfoModel> _sortingInstructionInfoModels = new();
        private List<TcpConfigInfoModel> _tcpConfigInfoModels = new();
        private ConcurrentQueue<KeyValuePair<string, string>> _replyContentQueue = new();

        public DefaultSortingConnectionService(ICommunicationConnectionConfigRepository
            communicationConnectionConfigRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            ISortingInstructionBindingRepository sortingInstructionBindingRepository,
            ISortingInstructionRepository sortingInstructionRepository,
            ITcpConfigRepository tcpConfigRepository) {
            _communicationConnectionConfigRepository = communicationConnectionConfigRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _sortingInstructionBindingRepository = sortingInstructionBindingRepository;
            _sortingInstructionRepository = sortingInstructionRepository;
            _tcpConfigRepository = tcpConfigRepository;

            //_communicationConnectionConfigRepository
            //获取对应连接
        }

        public async Task ConfigurationInitializer() {
            _connectionConfigInfoModels = await _communicationConnectionConfigRepository.CommunicationConnectionConfigItems(
                s =>
                    s.Id > 0);
            _packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s =>
                s.Id > 0, o => o.Id);
            _sortingInstructionBindingInfoModels = await _sortingInstructionBindingRepository.Select(
                s => s.Id > 0,
                o => o.Id);
            _sortingInstructionInfoModels = await _sortingInstructionRepository.Select(
                s => s.Id > 0,
                o => o.Id);
            _tcpConfigInfoModels = await _tcpConfigRepository.Select(s => s.Id > 0,
                o => o.Id);
        }

        public async Task<KeyValuePair<bool, string>> AddConnection(CommunicationsType type, CommunicationProtocol communicationProtocol, string connectionName, object? connectionParam) {
            if (connectionParam is null) {
                return new KeyValuePair<bool, string>(false, "连接参数不匹配");
            }
            if (type == CommunicationsType.SerialPort) {
                if (connectionParam is SerialPortConfigInfoModel info) {
                    //初始化串口
                    var sortingSerialPort = new SortingSerialPort();
                    sortingSerialPort.Disconnected += delegate (object? sender, ISortingSerialPort port) {
                        OnDisconnected(new ConnectionInfo() {
                            SortingSerialPort = sortingSerialPort,
                            Type = type,
                            ConnectionName = connectionName
                        });
                    };
                    sortingSerialPort.Communication += delegate (object? sender, CommunicationInfo info) {
                        OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo() {
                            Type = info.Type,
                            Time = info.Time,
                            Content = info.Content,
                            ConnectionName = connectionName,
                            FormatType = info.FormatType
                        });
                    };
                    sortingSerialPort.HeartbeatError += delegate (object? sender, Exception exception) {
                        OnHeartbeatError(exception);
                    };
                    sortingSerialPort.SendError += delegate (object? sender, Communication.SerialComm.ExceptionEventArgs args) {
                        OnSendError(new ExceptionEventArgs() {
                            ExceptionMessage = args.Exception.Message
                        });
                    };
                    sortingSerialPort.ErrorOccurred +=
                        delegate (object? sender, Communication.SerialComm.ExceptionEventArgs args) {
                            OnCommunicationExceptionEvent(args.Exception);
                        };
                    sortingSerialPort.DataReceived += delegate (object? sender, MessageEventArgs args) {
                        /*var deviceDecodeResult = _deviceCommunicationProtocol?.DecodeData(args.AsciiMessage);
                        if (deviceDecodeResult != null) {
                            OnReceivedInstructionsEvent(deviceDecodeResult);
                        }*/
                        _replyContentQueue.Enqueue(new KeyValuePair<string, string>(connectionName, args.AsciiMessage));
                    };
                    var parity = (Parity)Enum.Parse(typeof(Parity), info.Parity.ToString());
                    var stopBits = (StopBits)Enum.Parse(typeof(StopBits), info.StopBits.ToString());
                    var sortingSerialPortFormat = (SortingSerialPortFormat)Enum.Parse(typeof(SortingSerialPortFormat), info.DataFormat.ToString());
                    var connect = sortingSerialPort.Connect(info.PortName, info.BaudRate, info.DataBits, parity, stopBits,
                        sortingSerialPortFormat);
                    if (connect) {
                        //协议
                        IDeviceCommunicationProtocol? protocol = communicationProtocol switch {
                            CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                            CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                            _ => null
                        };
                        //心跳包
                        var connectionConfigInfoModel = _connectionConfigInfoModels.FirstOrDefault(f => f.ConnectionName.Equals(connectionName));

                        if (connectionConfigInfoModel?.HeartbeatConfigInfo is { IsHeartbeatEnabled: true, IsHeartbeatActive: true }) {
                            sortingSerialPort.StartHeartbeat(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatContent ?? string.Empty, TimeSpan.FromMilliseconds(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatInterval ?? 1000));
                        }
                        _connectionInfos.AddOrUpdate(connectionName, new ConnectionInfo() {
                            ConnectionName = connectionName,
                            Type = type,
                            SortingSerialPort = sortingSerialPort,
                            DeviceCommunicationProtocol = protocol
                        }, (s, connectionInfo) => new ConnectionInfo() {
                            ConnectionName = connectionName,
                            Type = type,
                            SortingSerialPort = sortingSerialPort,
                            DeviceCommunicationProtocol = protocol
                        });
                    }
                    return new KeyValuePair<bool, string>(connect, $"[{connectionName}]连接{(connect ? "成功" : "失败")}");
                }
                else {
                    return new KeyValuePair<bool, string>(false, "连接参数不匹配");
                }
            }
            else if (type == CommunicationsType.TCP) {
                //创建Tcp对象
                if (connectionParam is TcpConnectionConfigInfoModel info) {
                    ISortingTcp? sortingTcp = null;
                    if (info.ConnectionMode == 0) {
                        //创建对象
                        sortingTcp = new SortingTcp(new TouchSocketTcpClient(), new TouchSocketTcpServer());
                        sortingTcp.HeartbeatError += delegate (object? sender, Exception exception) {
                            OnHeartbeatError(exception);
                        };
                        sortingTcp.Exception += delegate (object? sender, Exception exception) {
                            OnCommunicationExceptionEvent(exception);
                        };
                        sortingTcp.Disconnected += delegate (object? sender, string s) {
                            OnDisconnected(new ConnectionInfo() {
                                ConnectionName = connectionName,
                                Type = type,
                            });
                        };
                        sortingTcp.ConnectionException += delegate (object? sender, string s) {
                            OnCommunicationExceptionEvent(new Exception(s));
                        };
                        sortingTcp.SendError += delegate (object? sender, Exception exception) {
                            OnSendError(new ExceptionEventArgs() {
                                ExceptionMessage = exception.Message
                            });
                        };
                        sortingTcp.Communication += delegate (object? sender, CommunicationInfo communicationInfo) {
                            OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo() {
                                Type = communicationInfo.Type,
                                Time = communicationInfo.Time,
                                Content = communicationInfo.Content,
                                ConnectionName = connectionName,
                                FormatType = communicationInfo.FormatType
                            });
                        };
                        sortingTcp.Connected += (sender, s) => {
                            EventAggregator.Instance.Publish(new SortingLogInfoModel {
                                CreateTime = DateTime.Now,
                                Message = $"连接:{connectionName},下位机已连接",
                                Type = LogType.Information
                            });
                        };
                        var tcpConfigInfoModel = info.TcpConfigItems?.FirstOrDefault(f => f.Type == 0);
                        if (tcpConfigInfoModel is not null) {
                            //协议
                            IDeviceCommunicationProtocol? protocol = communicationProtocol switch {
                                CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                                CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                                _ => null
                            };
                            var connect = await sortingTcp.Connect(tcpConfigInfoModel.IpAddress, tcpConfigInfoModel.Port,
                                ConnectionType.Client, 5000, FormatType.Hex, protocol?.DataLen ?? 0);
                            if (connect) {
                                //心跳包
                                var connectionConfigInfoModel = _connectionConfigInfoModels.FirstOrDefault(f => f.ConnectionName.Equals(connectionName));

                                if (connectionConfigInfoModel?.HeartbeatConfigInfo is { IsHeartbeatEnabled: true, IsHeartbeatActive: true }) {
                                    sortingTcp.StartHeartbeat(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatContent ?? string.Empty, TimeSpan.FromMilliseconds(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatInterval ?? 1000));
                                }
                                _connectionInfos.AddOrUpdate(connectionName, new ConnectionInfo() {
                                    ConnectionName = connectionName,
                                    Type = type,
                                    SortingTcp = sortingTcp,
                                    DeviceCommunicationProtocol = protocol
                                }, (s, connectionInfo) => new ConnectionInfo() {
                                    ConnectionName = connectionName,
                                    Type = type,
                                    SortingTcp = sortingTcp,
                                    DeviceCommunicationProtocol = protocol
                                });
                            }
                            return new KeyValuePair<bool, string>(connect, $"[{connectionName}]连接{(connect ? "成功" : "失败")}");
                        }
                        else {
                            return new KeyValuePair<bool, string>(false, "客户端参数为空");
                        }
                    }
                    else {
                        //创建对象
                        sortingTcp = new SortingTcp(new TouchSocketTcpClient(), new TouchSocketTcpServer());
                        sortingTcp.HeartbeatError += delegate (object? sender, Exception exception) {
                            OnHeartbeatError(exception);
                        };
                        sortingTcp.Exception += delegate (object? sender, Exception exception) {
                            OnCommunicationExceptionEvent(exception);
                        };
                        sortingTcp.Disconnected += delegate (object? sender, string s) {
                            OnDisconnected(new ConnectionInfo() {
                                ConnectionName = connectionName,
                                Type = type,
                            });
                        };
                        sortingTcp.ConnectionException += delegate (object? sender, string s) {
                            OnCommunicationExceptionEvent(new Exception(s));
                        };
                        sortingTcp.SendError += delegate (object? sender, Exception exception) {
                            OnSendError(new ExceptionEventArgs() {
                                ExceptionMessage = exception.Message
                            });
                        };
                        sortingTcp.Communication += delegate (object? sender, CommunicationInfo communicationInfo) {
                            OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo() {
                                Type = communicationInfo.Type,
                                Time = communicationInfo.Time,
                                Content = communicationInfo.Content,
                                ConnectionName = connectionName,
                                FormatType = communicationInfo.FormatType
                            });
                            _replyContentQueue.Enqueue(new KeyValuePair<string, string>(connectionName, communicationInfo.Content));
                        };
                        sortingTcp.Connected += (sender, s) => {
                            EventAggregator.Instance.Publish(new SortingLogInfoModel {
                                CreateTime = DateTime.Now,
                                Message = $"连接:{connectionName},下位机已连接",
                                Type = LogType.Information
                            });
                        };
                        var tcpConfigInfoModel = info.TcpConfigItems?.FirstOrDefault(f => f.Type != 0);
                        if (tcpConfigInfoModel is not null) {
                            //协议
                            IDeviceCommunicationProtocol? protocol = communicationProtocol switch {
                                CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                                CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                                _ => null
                            };
                            var connect = await sortingTcp.Connect(tcpConfigInfoModel.IpAddress, tcpConfigInfoModel.Port,
                                ConnectionType.Server, 5000, FormatType.Hex, protocol?.DataLen ?? 0);
                            if (connect) {
                                //心跳包
                                var connectionConfigInfoModel = _connectionConfigInfoModels.FirstOrDefault(f => f.ConnectionName.Equals(connectionName));

                                if (connectionConfigInfoModel?.HeartbeatConfigInfo is { IsHeartbeatEnabled: true, IsHeartbeatActive: true }) {
                                    sortingTcp.StartHeartbeat(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatContent ?? string.Empty, TimeSpan.FromMilliseconds(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatInterval ?? 1000));
                                }
                                _connectionInfos.AddOrUpdate(connectionName, new ConnectionInfo() {
                                    ConnectionName = connectionName,
                                    Type = type,
                                    SortingTcp = sortingTcp,
                                    DeviceCommunicationProtocol = protocol
                                }, (s, connectionInfo) => new ConnectionInfo() {
                                    ConnectionName = connectionName,
                                    Type = type,
                                    SortingTcp = sortingTcp,
                                    DeviceCommunicationProtocol = protocol
                                });
                            }
                            return new KeyValuePair<bool, string>(connect, $"[{connectionName}]连接{(connect ? "成功" : "失败")}");
                        }
                        else {
                            return new KeyValuePair<bool, string>(false, "服务端参数为空");
                        }
                    }
                }
            }
            return new KeyValuePair<bool, string>(false, "连接参数不匹配");
        }

        public async Task<KeyValuePair<bool, string>> ReleaseConnection(string connectionName) {
            await Task.Yield();
            var tryGetValue = _connectionInfos.TryGetValue(connectionName, out var connection);
            if (tryGetValue && connection is not null) {
                switch (connection) {
                    case { Type: CommunicationsType.SerialPort, SortingSerialPort: not null }: {
                            connection.SortingSerialPort.Dispose();
                            var tryRemove = _connectionInfos.TryRemove(connectionName, out _);
                            if (tryRemove) {
                                return new KeyValuePair<bool, string>(true, "连接释放成功");
                            }

                            break;
                        }
                    case { Type: CommunicationsType.TCP, SortingTcp: not null }: {
                            connection.SortingTcp.Dispose();
                            var tryRemove = _connectionInfos.TryRemove(connectionName, out connection);
                            if (tryRemove) {
                                return new KeyValuePair<bool, string>(true, "连接释放成功");
                            }

                            break;
                        }
                }
            }
            return new KeyValuePair<bool, string>(true, "连接释放失败");
        }

        public async Task<KeyValuePair<bool, string>> DisconnectAll() {
            await Task.Yield();
            foreach (var connectionInfo in _connectionInfos) {
                switch (connectionInfo.Value) {
                    case { Type: CommunicationsType.SerialPort, SortingSerialPort: not null }: {
                            connectionInfo.Value.SortingSerialPort.Dispose();
                            break;
                        }
                    case { Type: CommunicationsType.TCP, SortingTcp: not null }: {
                            connectionInfo.Value.SortingTcp.Dispose();
                            break;
                        }
                }
            }
            _connectionInfos.Clear();
            return new KeyValuePair<bool, string>(true, "连接释放成功");
        }

        public event EventHandler<ConnectionCommunicationMessageInfo>? CommunicationInfoEvent;

        public event EventHandler<Exception>? CommunicationExceptionEvent;

        public event EventHandler<DeviceDecodeResult>? ReceivedInstructionsEvent;

        public event EventHandler<Exception>? HeartbeatError;

        public event EventHandler<ExceptionEventArgs>? SendError;

        public event EventHandler<ConnectionInfo>? Disconnected;

        public async void SendInstructions(object tag, long exitId, List<string> instructions, TimeSpan interval, InstructionsAttach attach) {
            var isSend = false;
            if (exitId > 0) {
                var connectionId = _packageExitDefinitionInfoModels.FirstOrDefault(f => f.Id.Equals(exitId))
                    ?.CommunicationConnectionId;
                if (connectionId > 0) {
                    var connectionConfigInfoModel = _connectionConfigInfoModels.FirstOrDefault(f => f.Id.Equals(connectionId));
                    var connectionName = connectionConfigInfoModel?.ConnectionName;
                    if (!string.IsNullOrEmpty(connectionName)) {
                        var tryGetValue = _connectionInfos.TryGetValue(connectionName, out var connection);
                        if (tryGetValue && connection is not null) {
                            //开始发送

                            var sendTime = DateTime.Now;
                            if (connection is { Type: CommunicationsType.SerialPort, SortingSerialPort: not null }) {
                                //串口
                                if (connection.SortingSerialPort.Status == SortingSerialPortStatus.Running
                                    ) {
                                    if (instructions?.Any() == true) {
                                        foreach (var instruction in instructions) {
                                            //效验协议
                                            var message = instruction;
                                            if (connection.DeviceCommunicationProtocol is not null) {
                                                message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                    instruction, attach);
                                            }
                                            connection.SortingSerialPort.Send(message);

                                            OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo() {
                                                ConnectionName = connection.ConnectionName,
                                                BarCode = attach.BarCode,
                                                Content = message,
                                                ExitName = attach.ExitName,
                                                FormatType = (FormatType)connection.SortingSerialPort.FormatType,
                                                Guid = attach.Guid,
                                                Time = DateTime.Now,
                                                Timestamp = attach.Timestamp,
                                                Type = CommunicationType.Send
                                            });
                                            await Task.Delay(interval);
                                        }
                                        isSend = true;
                                    }
                                    else {
                                        OnCommunicationExceptionEvent(new Exception("无发送内容!"));
                                    }
                                }
                                else {
                                    OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                                }
                            }
                            else if (connection is { Type: CommunicationsType.TCP, SortingTcp: not null }) {
                                //tcp
                                if (connection.SortingTcp.ConnectionStatus == ConnectionStatus.Connected
                                    ) {
                                    if (instructions?.Any() == true) {
                                        foreach (var instruction in instructions) {
                                            //效验协议

                                            var message = instruction;
                                            if (connection.DeviceCommunicationProtocol is not null) {
                                                message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                    instruction, attach);
                                            }

                                            var sendMessage = await connection.SortingTcp.SendMessage(HexStringToByteArray(message));
                                            OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo() {
                                                ConnectionName = connection.ConnectionName,
                                                BarCode = attach.BarCode,
                                                Content = message,
                                                ExitName = attach.ExitName,
                                                FormatType = connection.SortingTcp.FormatType,
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
                                        isSend = true;
                                    }
                                    else {
                                        OnCommunicationExceptionEvent(new Exception("无发送内容!"));
                                    }
                                }
                                else {
                                    OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                                }
                            }
                            if (isSend) {
                                EventAggregator.Instance.Publish(new InstructionReceived() {
                                    Timestamp = attach.Timestamp,
                                    BarCode = attach.BarCode ?? string.Empty,
                                    ScanTime = attach.ScanTime,
                                    ExitId = attach.ExitId,
                                    ExitName = attach.ExitName,
                                    //先忽略快递
                                    LogisticsName = attach.LogisticsName,
                                    SortingMode = attach.SortingMode,
                                    SentInstruction = string.Join("\n", instructions?.Select(s => connection.DeviceCommunicationProtocol?.EncodeData(FunctionType.SendExit, tag,
                                        s, attach) ?? s)?.ToList() ?? new List<string>()),
                                    PackageCreationInstruction = attach.PackageCreationInstruction,
                                    PackageCreationTime = attach.PackageCreationTime,
                                    IsCreatedByLowerMachine = attach.IsCreatedByLowerMachine,
                                    CommunicationMethod = connection?.Type ?? CommunicationsType.None,
                                    ChecksumProtocolName = connectionConfigInfoModel?.CommunicationProtocol ?? string.Empty,
                                    SendTime = sendTime
                                });
                            }
                        }
                    }
                }
            }
        }

        public async void SendInstructions(object tag, long exitId, List<SortingInstructionInfoModel> instructions, TimeSpan interval, InstructionsAttach attach) {
            var isSend = false;
            if (exitId > 0) {
                var connectionId = _packageExitDefinitionInfoModels.FirstOrDefault(f => f.Id.Equals(exitId))
                    ?.CommunicationConnectionId;
                if (connectionId > 0) {
                    var connectionConfigInfoModel = _connectionConfigInfoModels.FirstOrDefault(f => f.Id.Equals(connectionId));
                    var connectionName = connectionConfigInfoModel?.ConnectionName;
                    if (!string.IsNullOrEmpty(connectionName)) {
                        var tryGetValue = _connectionInfos.TryGetValue(connectionName, out var connection);
                        if (tryGetValue && connection is not null) {
                            //开始发送

                            var sendTime = DateTime.Now;
                            if (connection is { Type: CommunicationsType.SerialPort, SortingSerialPort: not null }) {
                                //串口
                                if (connection.SortingSerialPort.Status == SortingSerialPortStatus.Running
                                    ) {
                                    if (instructions?.Any() == true) {
                                        foreach (var instruction in instructions) {
                                            if (connectionConfigInfoModel?.DeviceExtensionConfigInfo?.ValidateDeviceResponse == true) {
                                                var retryPolicy = Policy.HandleResult<bool>(result => !result)
                                                    .RetryAsync(connectionConfigInfoModel?.DeviceExtensionConfigInfo?.MaxRetryCount ?? 0, (a, b) => {
                                                    });

                                                var executeAsync = await retryPolicy.ExecuteAsync(async () => {
                                                    //效验协议
                                                    var message = instruction.Instruction;
                                                    if (connection.DeviceCommunicationProtocol is not null) {
                                                        message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                            instruction.Instruction, attach);
                                                    }
                                                    connection.SortingSerialPort.Send(message);
                                                    OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo() {
                                                        ConnectionName = connectionName,
                                                        BarCode = attach.BarCode,
                                                        Content = message,
                                                        ExitName = attach.ExitName,
                                                        FormatType = (FormatType)connection.SortingSerialPort.FormatType,
                                                        Guid = attach.Guid,
                                                        Time = DateTime.Now,
                                                        Timestamp = attach.Timestamp,
                                                        Type = CommunicationType.Send
                                                    });
                                                    return await WaitForReply(connectionName, instruction.ReplyContent,
                                                        TimeSpan.FromMilliseconds(connectionConfigInfoModel?.DeviceExtensionConfigInfo?.ValidationTimeout ?? 1));
                                                });
                                                if (!executeAsync) {
                                                    OnCommunicationExceptionEvent(new Exception("未收到应答信息!"));
                                                    break;
                                                }
                                            }
                                            else {
                                                //不使用应答
                                                var message = instruction.Instruction;
                                                if (connection.DeviceCommunicationProtocol is not null) {
                                                    message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                        instruction.Instruction, attach);
                                                }
                                                connection.SortingSerialPort.Send(message);
                                                OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo() {
                                                    ConnectionName = connectionName,
                                                    BarCode = attach.BarCode,
                                                    Content = message,
                                                    ExitName = attach.ExitName,
                                                    FormatType = (FormatType)connection.SortingSerialPort.FormatType,
                                                    Guid = attach.Guid,
                                                    Time = DateTime.Now,
                                                    Timestamp = attach.Timestamp,
                                                    Type = CommunicationType.Send
                                                });
                                            }
                                            await Task.Delay(interval);
                                        }

                                        isSend = true;
                                    }
                                    else {
                                        OnCommunicationExceptionEvent(new Exception("无发送内容!"));
                                    }
                                }
                                else {
                                    OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                                }
                            }
                            else if (connection is { Type: CommunicationsType.TCP, SortingTcp: not null }) {
                                //tcp
                                if (connection.SortingTcp.ConnectionStatus == ConnectionStatus.Connected) {
                                    if (instructions?.Any() == true) {
                                        foreach (var instruction in instructions) {
                                            //使用应答
                                            if (connectionConfigInfoModel?.DeviceExtensionConfigInfo?.ValidateDeviceResponse == true) {
                                                var retryPolicy = Policy.HandleResult<bool>(result => !result)
                                                   .RetryAsync(connectionConfigInfoModel?.DeviceExtensionConfigInfo?.MaxRetryCount ?? 0, (a, b) => {
                                                   });

                                                var executeAsync = await retryPolicy.ExecuteAsync(async () => {
                                                    //效验协议
                                                    var message = instruction.Instruction;
                                                    if (connection.DeviceCommunicationProtocol is not null) {
                                                        message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                            instruction.Instruction, attach);
                                                    }

                                                    var sendMessage = await connection.SortingTcp.SendMessage(HexStringToByteArray(message));
                                                    OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo() {
                                                        BarCode = attach.BarCode,
                                                        Content = message,
                                                        ExitName = attach.ExitName,
                                                        FormatType = connection.SortingTcp.FormatType,
                                                        Guid = attach.Guid,
                                                        Time = DateTime.Now,
                                                        Timestamp = attach.Timestamp,
                                                        Type = CommunicationType.Send
                                                    });
                                                    if (sendMessage) {
                                                        return await WaitForReply(connectionName, instruction.ReplyContent,
                                                            TimeSpan.FromMilliseconds(connectionConfigInfoModel?.DeviceExtensionConfigInfo?.ValidationTimeout ?? 1));
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
                                                if (connection.DeviceCommunicationProtocol is not null) {
                                                    message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                        instruction.Instruction, attach);
                                                }

                                                var sendMessage = await connection.SortingTcp.SendMessage(HexStringToByteArray(message));
                                                OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo() {
                                                    BarCode = attach.BarCode,
                                                    Content = message,
                                                    ExitName = attach.ExitName,
                                                    FormatType = connection.SortingTcp.FormatType,
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
                                        isSend = true;
                                    }
                                    else {
                                        OnCommunicationExceptionEvent(new Exception("无发送内容!"));
                                    }
                                }
                                else {
                                    OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                                }
                            }

                            if (isSend) {
                                EventAggregator.Instance.Publish(new InstructionReceived() {
                                    Timestamp = attach.Timestamp,
                                    BarCode = attach.BarCode ?? string.Empty,
                                    ScanTime = attach.ScanTime,
                                    ExitId = attach.ExitId,
                                    ExitName = attach.ExitName,
                                    //先忽略快递
                                    LogisticsName = attach.LogisticsName,
                                    SortingMode = attach.SortingMode,
                                    SentInstruction = string.Join("\n", instructions?.Select(s => connection.DeviceCommunicationProtocol?.EncodeData(FunctionType.SendExit, tag,
                                        s.Instruction, attach) ?? s.Instruction)?.ToList() ?? new List<string>()),
                                    PackageCreationInstruction = attach.PackageCreationInstruction,
                                    PackageCreationTime = attach.PackageCreationTime,
                                    IsCreatedByLowerMachine = attach.IsCreatedByLowerMachine,
                                    CommunicationMethod = connection?.Type ?? CommunicationsType.None,
                                    ChecksumProtocolName = connectionConfigInfoModel?.CommunicationProtocol ?? string.Empty,
                                    SendTime = sendTime
                                });
                            }
                        }
                    }
                }
            }
        }

        protected virtual async void OnCommunicationInfoEvent(ConnectionCommunicationMessageInfo e) {
            await Task.Yield();
            CommunicationInfoEvent?.Invoke(this, e);
            if (e is { Type: CommunicationType.Send, ExitName: null }) {
                EventAggregator.Instance.Publish(new SortingLogInfoModel {
                    CreateTime = e.Time,
                    Message = $"连接:{e.ConnectionName},发送内容:{e.Content}",
                    Type = LogType.Information
                });
            }
            else if (e.Type == CommunicationType.Receive) {
                var tryGetValue = _connectionInfos.TryGetValue(e.ConnectionName, out var connection);
                if (tryGetValue && connection is not null) {
                    if (connection.DeviceCommunicationProtocol is not null) {
                        var deviceDecodeResult = connection.DeviceCommunicationProtocol.DecodeData(e.Content);
                        if (deviceDecodeResult is not null) {
                            OnReceivedInstructionsEvent(new DeviceDecodeResult() {
                                ProtocolName = deviceDecodeResult.ProtocolName,
                                KeywordPosition = deviceDecodeResult.KeywordPosition,
                                Description = deviceDecodeResult.Description,
                                ExceptionMessage = deviceDecodeResult.ExceptionMessage,
                                IsException = deviceDecodeResult.IsException,
                                RawContent = deviceDecodeResult.RawContent,
                                Keyword = deviceDecodeResult.Keyword,
                                Type = deviceDecodeResult.Type
                            });
                        }
                    }
                }
                EventAggregator.Instance.Publish(new SortingLogInfoModel {
                    CreateTime = e.Time,
                    Message = $"连接:{e.ConnectionName},接收内容:{e.Content}",
                    Type = LogType.Information
                });
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
            EventAggregator.Instance.Publish(new SortingLogInfoModel {
                CreateTime = DateTime.Now,
                Message = $"心跳包异常",
                Type = LogType.Exception
            });
        }

        protected virtual async void OnSendError(ExceptionEventArgs e) {
            await Task.Yield();
            SendError?.Invoke(this, e);
            EventAggregator.Instance.Publish(new SortingLogInfoModel {
                CreateTime = DateTime.Now,
                Message = $"发送异常:{e.ExceptionMessage}",
                Type = LogType.Exception
            });
        }

        protected virtual async void OnDisconnected(ConnectionInfo e) {
            await Task.Yield();
            Disconnected?.Invoke(this, e);
            EventAggregator.Instance.Publish(new SortingLogInfoModel {
                CreateTime = DateTime.Now,
                Message = $"连接:{e.ConnectionName},断开",
                Type = LogType.Warning
            });
        }

        private byte[] HexStringToByteArray(string hexString) {
            try {
                hexString = hexString.Replace(" ", ""); // 移除空格

                var bytes = new byte[hexString.Length / 2];
                for (var i = 0; i < hexString.Length; i += 2) {
                    bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
                }

                return bytes;
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                OnSendError(new ExceptionEventArgs() {
                    ExceptionMessage = $"{e.Message}"
                });
            }

            return Array.Empty<byte>();
        }

        private async Task<bool> WaitForReply(string connection, string replyContent, TimeSpan timeOut) {
            await Task.Yield();
            var startTime = DateTime.Now;
            do {
                var tryDequeue = _replyContentQueue.TryDequeue(out var result);
                if (tryDequeue &&
                    result.Key.Equals(connection) && result.Value.Equals(replyContent)) {
                    return true;
                }
                await Task.Delay(5);
            } while (DateTime.Now.Subtract(startTime) < timeOut);
            return false;
        }
    }
}