using Polly;
using System;
using DryIoc;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using TouchSocket.Sockets;
using JayTom.Dws.Plugin.Tcp;
using JayTom.Dws.Plugin;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalLog;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using JayTom.Dws.Plugin.SerialPort;
using System.Collections.Concurrent;
using System.Threading.Channels;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Workflows;
using JayTom.Dws.Domain.EventMediators;
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

namespace JayTom.Dws.Client.Service.Sorting
{

    public class DefaultSortingConnectionService : ISortingConnectionService
    {
        private readonly ICommunicationConnectionConfigRepository _communicationConnectionConfigRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly ISortingInstructionBindingRepository _sortingInstructionBindingRepository;
        private readonly ISortingInstructionRepository _sortingInstructionRepository;
        private readonly ITcpConfigRepository _tcpConfigRepository;
        private readonly ConcurrentDictionary<string, ConnectionInfo> _connectionInfos = new();
        private List<CommunicationConnectionConfigInfoModel> _connectionConfigInfoModels = new();
        private List<PackageExitDefinitionInfoModel> _packageExitDefinitionInfoModels = new();
        private List<SortingInstructionBindingInfoModel> _sortingInstructionBindingInfoModels = new();
        private List<SortingInstructionInfoModel> _sortingInstructionInfoModels = new();
        private List<TcpConfigInfoModel> _tcpConfigInfoModels = new();
        /// <summary>
        /// 按连接隔离的应答通道，防止并发连接互相消费回包。
        /// </summary>
        private readonly ConcurrentDictionary<string, Channel<string>> _replyChannels =
            new(StringComparer.Ordinal);
        private readonly SemaphoreSlim _connectionLifecycleGate = new(1, 1);
        /// <summary>按物理连接串行发送，同一连接保持请求应答顺序，不同连接可以并行。</summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _connectionSendGates =
            new(StringComparer.Ordinal);
        /// <summary>
        /// 严格按到达顺序处理接收报文。TCP/串口回调只入队，不执行协议、包裹、日志或 UI 代码。
        /// </summary>
        private readonly NonBlockingOrderedDispatcher<(
            string ConnectionName,
            string Content,
            DateTime Time,
            CommunicationType Type,
            FormatType FormatType)>
            _receivedCommunicationDispatcher;
        /// <summary>
        /// 通信日志和界面通知独立排队，任何订阅者耗时都不能反压协议解析与指令分发。
        /// </summary>
        private readonly NonBlockingOrderedDispatcher<ConnectionCommunicationMessageInfo>
            _communicationNotificationDispatcher;

        /// <summary>
        /// 判断至少一个已配置的分拣连接当前是否可用。
        /// </summary>
        public bool IsConnected => _connectionInfos.Values.Any(connection => connection switch
        {
            { Type: CommunicationsType.SerialPort, SortingSerialPort: not null } =>
                connection.SortingSerialPort.Status == SerialPortStatus.Running,
            { Type: CommunicationsType.TCP, SortingTcp: not null } =>
                connection.SortingTcp.ConnectionStatus == ConnectionStatus.Connected,
            _ => false
        });

        public DefaultSortingConnectionService(ICommunicationConnectionConfigRepository
            communicationConnectionConfigRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            ISortingInstructionBindingRepository sortingInstructionBindingRepository,
            ISortingInstructionRepository sortingInstructionRepository,
            ITcpConfigRepository tcpConfigRepository)
        {
            _communicationConnectionConfigRepository = communicationConnectionConfigRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _sortingInstructionBindingRepository = sortingInstructionBindingRepository;
            _sortingInstructionRepository = sortingInstructionRepository;
            _tcpConfigRepository = tcpConfigRepository;
            _communicationNotificationDispatcher =
                new NonBlockingOrderedDispatcher<ConnectionCommunicationMessageInfo>(
                    PublishCommunicationNotification,
                    (_, exception) => OnCommunicationExceptionEvent(exception));
            _receivedCommunicationDispatcher = new NonBlockingOrderedDispatcher<(
                string ConnectionName,
                string Content,
                DateTime Time,
                CommunicationType Type,
                FormatType FormatType)>(
                ProcessIncomingCommunication,
                (_, exception) => OnCommunicationExceptionEvent(exception));

            //_communicationConnectionConfigRepository
            //获取对应连接
        }

        public event EventHandler<ConnectionInfo>? Connected;

        public async Task ConfigurationInitializer()
        {
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

        public async Task<KeyValuePair<bool, string>> AddConnection(
            CommunicationsType type,
            CommunicationProtocol communicationProtocol,
            string connectionName,
            object? connectionParam)
        {
            await _connectionLifecycleGate.WaitAsync();
            try
            {
                return await AddConnectionCoreAsync(
                    type,
                    communicationProtocol,
                    connectionName,
                    connectionParam);
            }
            finally
            {
                _connectionLifecycleGate.Release();
            }
        }

        private async Task<KeyValuePair<bool, string>> AddConnectionCoreAsync(
            CommunicationsType type,
            CommunicationProtocol communicationProtocol,
            string connectionName,
            object? connectionParam)
        {
            if (connectionParam is null)
            {
                return new KeyValuePair<bool, string>(false, "连接参数不匹配");
            }
            if (_connectionInfos.ContainsKey(connectionName))
            {
                ReleaseConnectionCore(connectionName);
            }
            if (type == CommunicationsType.SerialPort)
            {
                if (connectionParam is SerialPortConfigInfoModel info)
                {
                    //初始化串口
                    var sortingSerialPort = new SortingSerialPort(new SerialPort());
                    sortingSerialPort.Disconnected += delegate (object? sender, ISerialPort port)
                    {
                        OnDisconnected(new ConnectionInfo()
                        {
                            SortingSerialPort = sortingSerialPort,
                            Type = type,
                            ConnectionName = connectionName
                        });
                    };
                    sortingSerialPort.Communication += delegate (object? sender, CommunicationInfo info)
                    {
                        EnqueueIncomingCommunication(connectionName, info);
                    };
                    sortingSerialPort.HeartbeatError += delegate (object? sender, Exception exception)
                    {
                        OnHeartbeatError(exception);
                    };

                    sortingSerialPort.SendError += (sender, args) =>
                    {
                        OnSendError(new ExceptionEventArgs()
                        {
                            ExceptionMessage = args.Exception.Message
                        });
                    };
                    sortingSerialPort.ErrorOccurred += (sender, args) =>
                    {
                        OnCommunicationExceptionEvent(args.Exception);
                    };

                    sortingSerialPort.DataReceived += delegate (object? sender, MessageEventArgs args)
                    {
                        /*var deviceDecodeResult = _deviceCommunicationProtocol?.DecodeData(args.AsciiMessage);
                        if (deviceDecodeResult != null) {
                            OnReceivedInstructionsEvent(deviceDecodeResult);
                        }*/
                        EnqueueReply(connectionName, args.AsciiMessage);
                    };
                    var parity = (Parity)Enum.Parse(typeof(Parity), info.Parity.ToString());
                    var stopBits = (StopBits)Enum.Parse(typeof(StopBits), info.StopBits.ToString());
                    var sortingSerialPortFormat = (SerialPortFormat)Enum.Parse(typeof(SerialPortFormat), info.DataFormat.ToString());
                    var connect = sortingSerialPort.Connect(info.PortName, info.BaudRate, info.DataBits, parity, stopBits,
                        sortingSerialPortFormat);
                    if (connect)
                    {
                        //协议
                        IDeviceCommunicationProtocol? protocol = communicationProtocol switch
                        {
                            CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                            CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                            CommunicationProtocol.CaiNiao => new CaiNiaoCommunicationProtocol(),
                            _ => null
                        };
                        //心跳包
                        var connectionConfigInfoModel = _connectionConfigInfoModels.FirstOrDefault(f => f.ConnectionName.Equals(connectionName));

                        if (connectionConfigInfoModel?.HeartbeatConfigInfo is { IsHeartbeatEnabled: true, IsHeartbeatActive: true })
                        {
                            sortingSerialPort.StartHeartbeat(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatContent ?? string.Empty, sortingSerialPortFormat, TimeSpan.FromMilliseconds(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatInterval ?? 1000));
                        }
                        _connectionInfos.AddOrUpdate(connectionName, new ConnectionInfo()
                        {
                            ConnectionName = connectionName,
                            Type = type,
                            SortingSerialPort = sortingSerialPort,
                            DeviceCommunicationProtocol = protocol
                        }, (s, connectionInfo) => new ConnectionInfo()
                        {
                            ConnectionName = connectionName,
                            Type = type,
                            SortingSerialPort = sortingSerialPort,
                            DeviceCommunicationProtocol = protocol
                        });
                    }
                    else
                    {
                        sortingSerialPort.Dispose();
                    }

                    if (connect)
                    {
                        OnConnected(new ConnectionInfo()
                        {
                            SortingSerialPort = sortingSerialPort,
                            Type = type,
                            ConnectionName = connectionName
                        });
                    }
                    return new KeyValuePair<bool, string>(connect, $"[{connectionName}]连接{(connect ? "成功" : "失败")}");
                }
                else
                {
                    OnDisconnected(new ConnectionInfo()
                    {
                        Type = type,
                        ConnectionName = connectionName
                    });
                    return new KeyValuePair<bool, string>(false, "连接参数不匹配");
                }
            }
            else if (type == CommunicationsType.TCP)
            {
                //创建Tcp对象
                if (connectionParam is TcpConnectionConfigInfoModel info)
                {
                    ISortingTcp? sortingTcp = null;
                    if (info.ConnectionMode == 0)
                    {
                        //创建对象
                        sortingTcp = new SortingTcp(new TouchSocketTcpClient(), new TouchSocketTcpServer());
                        sortingTcp.HeartbeatError += delegate (object? sender, Exception exception)
                        {
                            OnHeartbeatError(exception);
                        };
                        sortingTcp.Exception += delegate (object? sender, Exception exception)
                        {
                            OnCommunicationExceptionEvent(exception);
                        };
                        sortingTcp.Disconnected += delegate (object? sender, string s)
                        {
                            OnDisconnected(new ConnectionInfo()
                            {
                                ConnectionName = connectionName,
                                Type = type,
                            });
                        };
                        sortingTcp.ConnectionException += delegate (object? sender, string s)
                        {
                            OnCommunicationExceptionEvent(new Exception(s));
                        };
                        sortingTcp.SendError += delegate (object? sender, Exception exception)
                        {
                            OnSendError(new ExceptionEventArgs()
                            {
                                ExceptionMessage = exception.Message
                            });
                        };
                        sortingTcp.Communication += delegate (object? sender, CommunicationInfo communicationInfo)
                        {
                            EnqueueIncomingCommunication(connectionName, communicationInfo);
                        };
                        sortingTcp.Connected += (sender, s) =>
                        {
                            EventAggregator.Instance.Publish(new SortingLogInfoModel
                            {
                                CreateTime = DateTime.Now,
                                Message = $"连接:{connectionName},下位机已连接",
                                Type = LogType.Information
                            });
                            OnConnected(new ConnectionInfo()
                            {
                                ConnectionName = connectionName,
                                Type = type,
                            });
                        };
                        var tcpConfigInfoModel = info.TcpConfigItems?.FirstOrDefault(f => f.Type == 0);
                        if (tcpConfigInfoModel is not null)
                        {
                            //协议
                            IDeviceCommunicationProtocol? protocol = communicationProtocol switch
                            {
                                CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                                CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                                CommunicationProtocol.CaiNiao => new CaiNiaoCommunicationProtocol(),
                                _ => null
                            };
                            var connect = await sortingTcp.Connect(tcpConfigInfoModel.IpAddress, tcpConfigInfoModel.Port,
                                ConnectionType.Client, 5000, (FormatType)(tcpConfigInfoModel.TcpConnectionConfigInfoInfo?.DataFormat ?? 0), protocol?.DataLen ?? 0);
                            if (connect)
                            {
                                //心跳包
                                var connectionConfigInfoModel = _connectionConfigInfoModels.FirstOrDefault(f => f.ConnectionName.Equals(connectionName));

                                if (connectionConfigInfoModel?.HeartbeatConfigInfo is { IsHeartbeatEnabled: true, IsHeartbeatActive: true })
                                {
                                    sortingTcp.StartHeartbeat(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatContent ?? string.Empty, (FormatType)(tcpConfigInfoModel.TcpConnectionConfigInfoInfo?.DataFormat ?? 0), TimeSpan.FromMilliseconds(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatInterval ?? 1000));
                                }
                                _connectionInfos.AddOrUpdate(connectionName, new ConnectionInfo()
                                {
                                    ConnectionName = connectionName,
                                    Type = type,
                                    SortingTcp = sortingTcp,
                                    DeviceCommunicationProtocol = protocol
                                }, (s, connectionInfo) => new ConnectionInfo()
                                {
                                    ConnectionName = connectionName,
                                    Type = type,
                                    SortingTcp = sortingTcp,
                                    DeviceCommunicationProtocol = protocol
                                });
                            }
                            else
                            {
                                sortingTcp.Dispose();
                            }
                            return new KeyValuePair<bool, string>(connect, $"[{connectionName}]连接{(connect ? "成功" : "失败")}");
                        }
                        else
                        {
                            return new KeyValuePair<bool, string>(false, "客户端参数为空");
                        }
                    }
                    else
                    {
                        //创建对象
                        sortingTcp = new SortingTcp(new TouchSocketTcpClient(), new TouchSocketTcpServer());
                        sortingTcp.HeartbeatError += delegate (object? sender, Exception exception)
                        {
                            OnHeartbeatError(exception);
                        };
                        sortingTcp.Exception += delegate (object? sender, Exception exception)
                        {
                            OnCommunicationExceptionEvent(exception);
                        };
                        sortingTcp.Disconnected += delegate (object? sender, string s)
                        {
                            OnDisconnected(new ConnectionInfo()
                            {
                                ConnectionName = connectionName,
                                Type = type,
                            });
                        };
                        sortingTcp.ConnectionException += delegate (object? sender, string s)
                        {
                            OnCommunicationExceptionEvent(new Exception(s));
                        };
                        sortingTcp.SendError += delegate (object? sender, Exception exception)
                        {
                            OnSendError(new ExceptionEventArgs()
                            {
                                ExceptionMessage = exception.Message
                            });
                        };
                        sortingTcp.Communication += delegate (object? sender, CommunicationInfo communicationInfo)
                        {
                            EnqueueIncomingCommunication(connectionName, communicationInfo);
                        };
                        sortingTcp.Connected += (sender, s) =>
                        {
                            EventAggregator.Instance.Publish(new SortingLogInfoModel
                            {
                                CreateTime = DateTime.Now,
                                Message = $"连接:{connectionName},下位机已连接",
                                Type = LogType.Information
                            });
                            OnConnected(new ConnectionInfo()
                            {
                                ConnectionName = connectionName,
                                Type = type,
                            });
                        };
                        var tcpConfigInfoModel = info.TcpConfigItems?.FirstOrDefault(f => f.Type != 0);
                        if (tcpConfigInfoModel is not null)
                        {
                            //协议
                            IDeviceCommunicationProtocol? protocol = communicationProtocol switch
                            {
                                CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                                CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                                CommunicationProtocol.CaiNiao => new CaiNiaoCommunicationProtocol(),
                                _ => null
                            };
                            var connect = await sortingTcp.Connect(tcpConfigInfoModel.IpAddress, tcpConfigInfoModel.Port,
                                ConnectionType.Server, 5000, (FormatType)(tcpConfigInfoModel.TcpConnectionConfigInfoInfo?.DataFormat ?? 0), protocol?.DataLen ?? 0);
                            if (connect)
                            {
                                //心跳包
                                var connectionConfigInfoModel = _connectionConfigInfoModels.FirstOrDefault(f => f.ConnectionName.Equals(connectionName));

                                if (connectionConfigInfoModel?.HeartbeatConfigInfo is { IsHeartbeatEnabled: true, IsHeartbeatActive: true })
                                {
                                    sortingTcp.StartHeartbeat(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatContent ?? string.Empty, (FormatType)(tcpConfigInfoModel.TcpConnectionConfigInfoInfo?.DataFormat ?? 0), TimeSpan.FromMilliseconds(connectionConfigInfoModel?.HeartbeatConfigInfo?.HeartbeatInterval ?? 1000));
                                }
                                _connectionInfos.AddOrUpdate(connectionName, new ConnectionInfo()
                                {
                                    ConnectionName = connectionName,
                                    Type = type,
                                    SortingTcp = sortingTcp,
                                    DeviceCommunicationProtocol = protocol
                                }, (s, connectionInfo) => new ConnectionInfo()
                                {
                                    ConnectionName = connectionName,
                                    Type = type,
                                    SortingTcp = sortingTcp,
                                    DeviceCommunicationProtocol = protocol
                                });
                            }
                            else
                            {
                                sortingTcp.Dispose();
                            }
                            return new KeyValuePair<bool, string>(connect, $"[{connectionName}]连接{(connect ? "成功" : "失败")}");
                        }
                        else
                        {
                            return new KeyValuePair<bool, string>(false, "服务端参数为空");
                        }
                    }
                }
            }
            return new KeyValuePair<bool, string>(false, "连接参数不匹配");
        }

        public async Task<KeyValuePair<bool, string>> ReleaseConnection(string connectionName)
        {
            await _connectionLifecycleGate.WaitAsync();
            try
            {
                var sendGate = _connectionSendGates.GetOrAdd(
                    connectionName,
                    static _ => new SemaphoreSlim(1, 1));
                await sendGate.WaitAsync();
                try
                {
                    return ReleaseConnectionCore(connectionName);
                }
                finally
                {
                    sendGate.Release();
                }
            }
            finally
            {
                _connectionLifecycleGate.Release();
            }
        }

        private KeyValuePair<bool, string> ReleaseConnectionCore(string connectionName)
        {
            var tryGetValue = _connectionInfos.TryGetValue(connectionName, out var connection);
            if (tryGetValue && connection is not null)
            {
                switch (connection)
                {
                    case { Type: CommunicationsType.SerialPort, SortingSerialPort: not null }:
                        {
                            connection.SortingSerialPort.Dispose();
                            var tryRemove = _connectionInfos.TryRemove(connectionName, out _);
                            if (tryRemove)
                            {
                                RemoveReplyChannel(connectionName);
                                return new KeyValuePair<bool, string>(true, "连接释放成功");
                            }

                            break;
                        }
                    case { Type: CommunicationsType.TCP, SortingTcp: not null }:
                        {
                            connection.SortingTcp.Dispose();
                            var tryRemove = _connectionInfos.TryRemove(connectionName, out connection);
                            if (tryRemove)
                            {
                                RemoveReplyChannel(connectionName);
                                return new KeyValuePair<bool, string>(true, "连接释放成功");
                            }

                            break;
                        }
                }
            }
            return new KeyValuePair<bool, string>(false, "连接释放失败");
        }

        public async Task<KeyValuePair<bool, string>> DisconnectAll()
        {
            await _connectionLifecycleGate.WaitAsync();
            try
            {
                var sendGates = _connectionInfos.Keys
                    .Select(connectionName => _connectionSendGates.GetOrAdd(
                        connectionName,
                        static _ => new SemaphoreSlim(1, 1)))
                    .Distinct()
                    .ToArray();
                foreach (var sendGate in sendGates)
                {
                    await sendGate.WaitAsync();
                }

                try
                {
                    foreach (var connectionInfo in _connectionInfos)
                    {
                        switch (connectionInfo.Value)
                        {
                            case { Type: CommunicationsType.SerialPort, SortingSerialPort: not null }:
                                connectionInfo.Value.SortingSerialPort.Dispose();
                                break;
                            case { Type: CommunicationsType.TCP, SortingTcp: not null }:
                                connectionInfo.Value.SortingTcp.Dispose();
                                break;
                        }
                    }
                    _connectionInfos.Clear();
                    foreach (var connectionName in _replyChannels.Keys)
                    {
                        RemoveReplyChannel(connectionName);
                    }
                    return new KeyValuePair<bool, string>(true, "连接释放成功");
                }
                finally
                {
                    foreach (var sendGate in sendGates)
                    {
                        sendGate.Release();
                    }
                }
            }
            finally
            {
                _connectionLifecycleGate.Release();
            }
        }

        public event EventHandler<ConnectionCommunicationMessageInfo>? CommunicationInfoEvent;

        public event EventHandler<Exception>? CommunicationExceptionEvent;

        public event EventHandler<DeviceDecodeResult>? ReceivedInstructionsEvent;

        public event EventHandler<Exception>? HeartbeatError;

        public event EventHandler<ExceptionEventArgs>? SendError;

        public event EventHandler<ConnectionInfo>? Disconnected;

        /// <summary>
        /// 将设备通信事件复制到关键接收队列；该方法不等待锁、协议、日志或业务订阅者。
        /// </summary>
        private void EnqueueIncomingCommunication(
            string connectionName,
            CommunicationInfo communicationInfo)
        {
            var incoming = (
                connectionName,
                communicationInfo.Content,
                communicationInfo.Time,
                communicationInfo.Type,
                communicationInfo.FormatType);
            if (!_receivedCommunicationDispatcher.TryEnqueue(incoming))
            {
                OnCommunicationExceptionEvent(new InvalidOperationException(
                    $"分拣接收队列已停止，连接 {connectionName} 的报文未能入队"));
            }
        }

        /// <summary>由单消费者严格按收到顺序执行协议解析和后续事件。</summary>
        private void ProcessIncomingCommunication((
            string ConnectionName,
            string Content,
            DateTime Time,
            CommunicationType Type,
            FormatType FormatType) incoming)
        {
            if (incoming.Type == CommunicationType.Receive)
            {
                EnqueueReply(incoming.ConnectionName, incoming.Content);
            }

            OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo
            {
                Type = incoming.Type,
                Time = incoming.Time,
                Content = incoming.Content,
                ConnectionName = incoming.ConnectionName,
                FormatType = incoming.FormatType
            });

        }

        public void SendInstructions(
            object tag,
            long exitId,
            List<string> instructions,
            TimeSpan interval,
            InstructionsAttach attach)
        {
            QueueConnectionWork(ResolveConnectionName(exitId), () => SendInstructionsAsync(
                tag,
                exitId,
                instructions,
                interval,
                attach));
        }

        private async Task SendInstructionsAsync(
            object tag,
            long exitId,
            List<string> instructions,
            TimeSpan interval,
            InstructionsAttach attach)
        {
            EnsurePackageIdentityBeforeSend(attach, exitId);

            if (exitId > 0)
            {
                long? connectionId = _packageExitDefinitionInfoModels.FirstOrDefault(f => f.Id.Equals(exitId))
                    ?.CommunicationConnectionId;
                if (connectionId > 0)
                {
                    var connectionConfigInfoModel = _connectionConfigInfoModels.FirstOrDefault(f => f.Id.Equals(connectionId));
                    var connectionName = connectionConfigInfoModel?.ConnectionName;
                    if (!string.IsNullOrEmpty(connectionName))
                    {
                        var tryGetValue = _connectionInfos.TryGetValue(connectionName, out var connection);
                        if (tryGetValue && connection is not null)
                        {
                            //开始发送
                            if (instructions?.Any() == true)
                            {
                                foreach (var instruction in instructions)
                                {
                                    EnsurePackageIdentityBeforeSend(attach, exitId);
                                    var isSend = false;
                                    var sendTime = DateTime.Now;
                                    if (connection is { Type: CommunicationsType.SerialPort, SortingSerialPort: not null })
                                    {
                                        //串口
                                        if (connection.SortingSerialPort.Status == SerialPortStatus.Running
                                            )
                                        {
                                            //效验协议
                                            var message = instruction;
                                            if (connection.DeviceCommunicationProtocol is not null)
                                            {
                                                message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                    instruction, attach);
                                            }
                                            EnsurePackageIdentityBeforeSend(attach, exitId);
                                            connection.SortingSerialPort.Send(message);

                                            OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                                            {
                                                ConnectionName = connection.ConnectionName,
                                                BarCode = attach.BarCode,
                                                Content = message,
                                                ExitName = attach.ExitName,
                                                FormatType = (FormatType)connection.SortingSerialPort.FormatType,
                                                Guid = attach.Guid,
                                                Time = sendTime = DateTime.Now,
                                                Timestamp = attach.Timestamp,
                                                Type = CommunicationType.Send
                                            });
                                            isSend = true;
                                        }
                                        else
                                        {
                                            OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                                        }
                                    }
                                    else if (connection is { Type: CommunicationsType.TCP, SortingTcp: not null })
                                    {
                                        //tcp
                                        if (connection.SortingTcp.ConnectionStatus == ConnectionStatus.Connected
                                            )
                                        {
                                            var message = instruction;
                                            if (connection.DeviceCommunicationProtocol is not null)
                                            {
                                                message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                    instruction, attach);
                                            }

                                            EnsurePackageIdentityBeforeSend(attach, exitId);
                                            var sendMessage = await SendTcpMessage(connection, message);
                                            OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                                            {
                                                ConnectionName = connection.ConnectionName,
                                                BarCode = attach.BarCode,
                                                Content = message,
                                                ExitName = attach.ExitName,
                                                FormatType = connection.SortingTcp.FormatType,
                                                Guid = attach.Guid,
                                                Time = sendTime = DateTime.Now,
                                                Timestamp = attach.Timestamp,
                                                Type = CommunicationType.Send
                                            });
                                            if (!sendMessage)
                                            {
                                                OnCommunicationExceptionEvent(new Exception("发送失败!"));
                                            }
                                            else
                                            {
                                                isSend = true;
                                            }
                                        }
                                        else
                                        {
                                            OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                                        }
                                    }

                                    //记录
                                    if (isSend)
                                    {
                                        EventAggregator.Instance.Publish(new InstructionReceived()
                                        {
                                            Timestamp = attach.Timestamp,
                                            BarCode = attach.BarCode ?? string.Empty,
                                            ScanTime = attach.ScanTime,
                                            ExitId = attach.ExitId,
                                            ExitName = attach.ExitName,
                                            //先忽略快递
                                            LogisticsName = attach.LogisticsName,
                                            SortingMode = attach.SortingMode,
                                            IsCreatedByLowerMachine = attach.IsCreatedByLowerMachine,
                                            CommunicationMethod = connection?.Type ?? CommunicationsType.None,
                                            ChecksumProtocolName = connectionConfigInfoModel?.CommunicationProtocol ?? string.Empty,
                                            SortingCode = attach.Guid.ToString(),
                                            InstructionInfos = new List<InstructionInfoModel>()
                                            {
                                                new()
                                                {
                                                    InstructionContent = FormatInstructionContent(connection,
                                                        connection?.DeviceCommunicationProtocol?.EncodeData(FunctionType.SendExit, tag,
                                                            instruction, attach) ?? instruction),
                                                    InstructionGeneratedTime =sendTime,
                                                    InstructionType = InstructionType.SendSorting
                                                }
                                            },
                                            ConnectionName = connection?.ConnectionName ?? string.Empty
                                        });
                                        await Task.Delay(interval);
                                    }
                                }
                            }
                            else
                            {
                                OnCommunicationExceptionEvent(new Exception("无发送内容!"));
                            }
                        }
                    }
                }
            }
        }

        public void SendInstructions(
            object tag,
            long exitId,
            List<SortingInstructionInfoModel> instructions,
            TimeSpan interval,
            InstructionsAttach attach)
        {
            QueueConnectionWork(ResolveConnectionName(exitId), () => SendInstructionsAsync(
                tag,
                exitId,
                instructions,
                interval,
                attach));
        }

        private async Task SendInstructionsAsync(
            object tag,
            long exitId,
            List<SortingInstructionInfoModel> instructions,
            TimeSpan interval,
            InstructionsAttach attach)
        {
            EnsurePackageIdentityBeforeSend(attach, exitId);

            if (exitId > 0)
            {
                long? connectionId = _packageExitDefinitionInfoModels.FirstOrDefault(f => f.Id.Equals(exitId))
                    ?.CommunicationConnectionId;
                if (connectionId > 0)
                {
                    var connectionConfigInfoModel = _connectionConfigInfoModels.FirstOrDefault(f => f.Id.Equals(connectionId));
                    var connectionName = connectionConfigInfoModel?.ConnectionName;
                    if (!string.IsNullOrEmpty(connectionName))
                    {
                        var tryGetValue = _connectionInfos.TryGetValue(connectionName, out var connection);
                        if (tryGetValue && connection is not null)
                        {
                            //开始发送
                            if (instructions?.Any() == true)
                            {
                                foreach (var instruction in instructions)
                                {
                                    EnsurePackageIdentityBeforeSend(attach, exitId);
                                    var isSend = false;
                                    var sendTime = DateTime.Now;
                                    if (connection is { Type: CommunicationsType.SerialPort, SortingSerialPort: not null })
                                    {
                                        //串口
                                        if (connection.SortingSerialPort.Status == SerialPortStatus.Running
                                            )
                                        {
                                            if (connectionConfigInfoModel?.DeviceExtensionConfigInfo?.ValidateDeviceResponse == true)
                                            {
                                                var retryPolicy = Policy.HandleResult<bool>(result => !result)
                                                    .WaitAndRetryAsync(
                                                        connectionConfigInfoModel?.DeviceExtensionConfigInfo?.MaxRetryCount ?? 0,
                                                        retryAttempt => TimeSpan.FromMilliseconds(
                                                            Math.Min(50 * retryAttempt, 500)));

                                                var executeAsync = await retryPolicy.ExecuteAsync(async () =>
                                                {
                                                    EnsurePackageIdentityBeforeSend(attach, exitId);
                                                    //效验协议
                                                    var message = instruction.Instruction;
                                                    if (connection.DeviceCommunicationProtocol is not null)
                                                    {
                                                        message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                            instruction.Instruction, attach);
                                                    }
                                                    EnsurePackageIdentityBeforeSend(attach, exitId);
                                                    connection.SortingSerialPort.Send(message);
                                                    OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                                                    {
                                                        ConnectionName = connectionName,
                                                        BarCode = attach.BarCode,
                                                        Content = message,
                                                        ExitName = attach.ExitName,
                                                        FormatType = (FormatType)connection.SortingSerialPort.FormatType,
                                                        Guid = attach.Guid,
                                                        Time = sendTime = DateTime.Now,
                                                        Timestamp = attach.Timestamp,
                                                        Type = CommunicationType.Send
                                                    });
                                                    return await WaitForReply(connectionName, instruction.ReplyContent,
                                                        TimeSpan.FromMilliseconds(connectionConfigInfoModel?.DeviceExtensionConfigInfo?.ValidationTimeout ?? 1));
                                                });
                                                if (!executeAsync)
                                                {
                                                    OnCommunicationExceptionEvent(new Exception("未收到应答信息!"));
                                                    break;
                                                }

                                                isSend = true;
                                            }
                                            else
                                            {
                                                //不使用应答
                                                var message = instruction.Instruction;
                                                if (connection.DeviceCommunicationProtocol is not null)
                                                {
                                                    message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                        instruction.Instruction, attach);
                                                }
                                                EnsurePackageIdentityBeforeSend(attach, exitId);
                                                connection.SortingSerialPort.Send(message);
                                                OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                                                {
                                                    ConnectionName = connectionName,
                                                    BarCode = attach.BarCode,
                                                    Content = message,
                                                    ExitName = attach.ExitName,
                                                    FormatType = (FormatType)connection.SortingSerialPort.FormatType,
                                                    Guid = attach.Guid,
                                                    Time = sendTime = DateTime.Now,
                                                    Timestamp = attach.Timestamp,
                                                    Type = CommunicationType.Send
                                                });
                                                isSend = true;
                                            }
                                        }
                                        else
                                        {
                                            OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                                        }
                                    }
                                    else if (connection is { Type: CommunicationsType.TCP, SortingTcp: not null })
                                    {
                                        //tcp
                                        if (connection.SortingTcp.ConnectionStatus == ConnectionStatus.Connected)
                                        {
                                            if (connectionConfigInfoModel?.DeviceExtensionConfigInfo?.ValidateDeviceResponse == true)
                                            {
                                                var retryPolicy = Policy.HandleResult<bool>(result => !result)
                                                   .WaitAndRetryAsync(
                                                       connectionConfigInfoModel?.DeviceExtensionConfigInfo?.MaxRetryCount ?? 0,
                                                       retryAttempt => TimeSpan.FromMilliseconds(
                                                           Math.Min(50 * retryAttempt, 500)));

                                                var executeAsync = await retryPolicy.ExecuteAsync(async () =>
                                                {
                                                    EnsurePackageIdentityBeforeSend(attach, exitId);
                                                    //效验协议
                                                    var message = instruction.Instruction;
                                                    if (connection.DeviceCommunicationProtocol is not null)
                                                    {
                                                        message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                            instruction.Instruction, attach);
                                                    }

                                                    EnsurePackageIdentityBeforeSend(attach, exitId);
                                                    var sendMessage = await SendTcpMessage(connection, message);
                                                    OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                                                    {
                                                        BarCode = attach.BarCode,
                                                        Content = message,
                                                        ExitName = attach.ExitName,
                                                        FormatType = connection.SortingTcp.FormatType,
                                                        Guid = attach.Guid,
                                                        Time = sendTime = DateTime.Now,
                                                        Timestamp = attach.Timestamp,
                                                        Type = CommunicationType.Send
                                                    });
                                                    if (sendMessage)
                                                    {
                                                        return await WaitForReply(connectionName, instruction.ReplyContent,
                                                            TimeSpan.FromMilliseconds(connectionConfigInfoModel?.DeviceExtensionConfigInfo?.ValidationTimeout ?? 1));
                                                    }
                                                    return false;
                                                });
                                                if (!executeAsync)
                                                {
                                                    OnCommunicationExceptionEvent(new Exception("未收到应答信息!"));
                                                    break;
                                                }
                                                isSend = true;
                                            }
                                            else
                                            {
                                                //不使用应答

                                                //效验协议
                                                var message = instruction.Instruction;
                                                if (connection.DeviceCommunicationProtocol is not null)
                                                {
                                                    message = connection.DeviceCommunicationProtocol.EncodeData(FunctionType.SendExit, tag,
                                                        instruction.Instruction, attach);
                                                }

                                                EnsurePackageIdentityBeforeSend(attach, exitId);
                                                var sendMessage = await SendTcpMessage(connection, message);
                                                OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                                                {
                                                    BarCode = attach.BarCode,
                                                    Content = message,
                                                    ExitName = attach.ExitName,
                                                    FormatType = connection.SortingTcp.FormatType,
                                                    Guid = attach.Guid,
                                                    Time = sendTime = DateTime.Now,
                                                    Timestamp = attach.Timestamp,
                                                    Type = CommunicationType.Send
                                                });
                                                if (!sendMessage)
                                                {
                                                    OnCommunicationExceptionEvent(new Exception("发送失败!"));
                                                    break;
                                                }
                                                isSend = true;
                                            }
                                        }
                                        else
                                        {
                                            OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                                        }
                                    }

                                    //记录
                                    if (isSend)
                                    {
                                        EventAggregator.Instance.Publish(new InstructionReceived()
                                        {
                                            Timestamp = attach.Timestamp,
                                            BarCode = attach.BarCode ?? string.Empty,
                                            ScanTime = attach.ScanTime,
                                            ExitId = attach.ExitId,
                                            ExitName = attach.ExitName,
                                            //先忽略快递
                                            LogisticsName = attach.LogisticsName,
                                            SortingMode = attach.SortingMode,
                                            IsCreatedByLowerMachine = attach.IsCreatedByLowerMachine,
                                            CommunicationMethod = connection?.Type ?? CommunicationsType.None,
                                            ChecksumProtocolName = connectionConfigInfoModel?.CommunicationProtocol ?? string.Empty,
                                            SortingCode = attach.Guid.ToString(),
                                            InstructionInfos = new List<InstructionInfoModel>()
                                            {
                                                new()
                                                {
                                                    InstructionContent = FormatInstructionContent(connection,
                                                        connection?.DeviceCommunicationProtocol?.EncodeData(FunctionType.SendExit, tag,
                                                            instruction.Instruction, attach) ?? instruction.Instruction),
                                                    InstructionGeneratedTime = sendTime,
                                                    InstructionType = InstructionType.SendSorting
                                                }
                                            },
                                            ConnectionName = connection?.ConnectionName ?? string.Empty
                                        });
                                        await Task.Delay(interval);
                                    }
                                }
                            }
                            else
                            {
                                OnCommunicationExceptionEvent(new Exception("无发送内容!"));
                            }
                        }
                    }
                }
            }
        }

        public void SendPreSignal(
            int num,
            InstructionsAttach attach,
            CancellationToken token = default)
        {
            var connectionName = _connectionInfos.Keys.FirstOrDefault();
            QueueConnectionWork(
                connectionName,
                () => SendPreSignalAsync(connectionName, num, attach, token));
        }

        private async Task SendPreSignalAsync(
            string? connectionName,
            int num,
            InstructionsAttach attach,
            CancellationToken token)
        {
            if (!string.IsNullOrEmpty(connectionName) &&
                _connectionInfos.TryGetValue(connectionName, out var value))
            {
                var key = connectionName;
                var isSend = false;
                var sendTime = DateTime.Now;
                if (value is { Type: CommunicationsType.SerialPort, SortingSerialPort: not null })
                {
                    //串口
                    if (value.SortingSerialPort.Status == SerialPortStatus.Running)
                    {
                        var message = string.Empty;
                        if (value.DeviceCommunicationProtocol is not null)
                        {
                            message = value.DeviceCommunicationProtocol.EncodeData(FunctionType.SendPreSignal, new object(),
                                string.Empty, attach);
                        }
                        value.SortingSerialPort.Send(message);
                        OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                        {
                            ConnectionName = key,
                            BarCode = attach.BarCode,
                            Content = message,
                            ExitName = string.Empty,
                            FormatType = (FormatType)value.SortingSerialPort.FormatType,
                            Guid = num,
                            Time = sendTime = DateTime.Now,
                            Type = CommunicationType.Send,
                            Timestamp = attach.Timestamp,
                        });
                        isSend = true;
                    }
                    else
                    {
                        OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                    }
                }
                else if (value is { Type: CommunicationsType.TCP, SortingTcp: not null })
                {
                    if (value.SortingTcp.ConnectionStatus == ConnectionStatus.Connected)
                    {
                        var message = string.Empty;
                        if (value.DeviceCommunicationProtocol is not null)
                        {
                            message = value.DeviceCommunicationProtocol.EncodeData(FunctionType.SendPreSignal, new object(),
                                string.Empty, attach);
                        }

                        var sendMessage = await SendTcpMessage(value, message, token);
                        OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                        {
                            BarCode = attach.BarCode,
                            Content = message,
                            ExitName = attach.ExitName,
                            FormatType = value.SortingTcp.FormatType,
                            Guid = num,
                            Time = sendTime = DateTime.Now,
                            Timestamp = attach.Timestamp,
                            Type = CommunicationType.Send,
                            ConnectionName = key
                        });
                        if (!sendMessage)
                        {
                            OnCommunicationExceptionEvent(new Exception("发送失败!"));
                        }
                        else
                        {
                            isSend = true;
                        }
                    }
                    else
                    {
                        OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                    }
                }

                //记录
                if (isSend)
                {
                    EventAggregator.Instance.Publish(new InstructionReceived()
                    {
                        Timestamp = attach.Timestamp,
                        BarCode = attach.BarCode ?? string.Empty,
                        ScanTime = attach.ScanTime,
                        ExitId = attach.ExitId,
                        ExitName = attach.ExitName,
                        //先忽略快递
                        LogisticsName = attach.LogisticsName,
                        SortingMode = attach.SortingMode,
                        IsCreatedByLowerMachine = attach.IsCreatedByLowerMachine,
                        CommunicationMethod = value?.Type ?? CommunicationsType.None,
                        SortingCode = attach.Guid.ToString(),
                        InstructionInfos = new List<InstructionInfoModel>()
                        {
                            new()
                            {
                                InstructionContent = FormatInstructionContent(value,
                                    value?.DeviceCommunicationProtocol?.EncodeData(FunctionType.SendPreSignal, new object(),
                                        string.Empty, attach) ?? string.Empty),
                                InstructionGeneratedTime = sendTime,
                                InstructionType = InstructionType.SendPreSignal
                            }
                        },
                        ConnectionName = key ?? string.Empty
                    });
                }
            }
        }

        public void SendPackageInfoCompletedSignal(
            int num,
            InstructionsAttach attach,
            CancellationToken token = default)
        {
            var connectionName = _connectionInfos.Keys.FirstOrDefault();
            QueueConnectionWork(
                connectionName,
                () => SendPackageInfoCompletedSignalAsync(connectionName, num, attach, token));
        }

        private async Task SendPackageInfoCompletedSignalAsync(
            string? connectionName,
            int num,
            InstructionsAttach attach,
            CancellationToken token)
        {
            if (!string.IsNullOrEmpty(connectionName) &&
                _connectionInfos.TryGetValue(connectionName, out var value))
            {
                var key = connectionName;
                var isSend = false;
                var sendTime = DateTime.Now;
                if (value is { Type: CommunicationsType.SerialPort, SortingSerialPort: not null })
                {
                    //串口
                    if (value.SortingSerialPort.Status == SerialPortStatus.Running)
                    {
                        var message = string.Empty;
                        if (value.DeviceCommunicationProtocol is not null)
                        {
                            message = value.DeviceCommunicationProtocol.EncodeData(FunctionType.PackageInfoCompletedSignal, new object(),
                                string.Empty, attach);
                        }
                        value.SortingSerialPort.Send(message);
                        OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                        {
                            ConnectionName = key,
                            BarCode = attach.BarCode,
                            Content = message,
                            ExitName = string.Empty,
                            FormatType = (FormatType)value.SortingSerialPort.FormatType,
                            Guid = num,
                            Time = sendTime = DateTime.Now,
                            Type = CommunicationType.Send,
                            Timestamp = attach.Timestamp,
                        });
                        isSend = true;
                    }
                    else
                    {
                        OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                    }
                }
                else if (value is { Type: CommunicationsType.TCP, SortingTcp: not null })
                {
                    if (value.SortingTcp.ConnectionStatus == ConnectionStatus.Connected)
                    {
                        var message = string.Empty;
                        if (value.DeviceCommunicationProtocol is not null)
                        {
                            message = value.DeviceCommunicationProtocol.EncodeData(FunctionType.PackageInfoCompletedSignal, new object(),
                                string.Empty, attach);
                        }

                        var sendMessage = await SendTcpMessage(value, message, token);
                        OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                        {
                            BarCode = attach.BarCode,
                            Content = message,
                            ExitName = attach.ExitName,
                            FormatType = value.SortingTcp.FormatType,
                            Guid = num,
                            Time = sendTime = DateTime.Now,
                            Timestamp = attach.Timestamp,
                            Type = CommunicationType.Send,
                            ConnectionName = key
                        });
                        if (!sendMessage)
                        {
                            OnCommunicationExceptionEvent(new Exception("发送失败!"));
                        }
                        else
                        {
                            isSend = true;
                        }
                    }
                    else
                    {
                        OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                    }
                }

                //记录
                if (isSend)
                {
                    EventAggregator.Instance.Publish(new InstructionReceived()
                    {
                        Timestamp = attach.Timestamp,
                        BarCode = attach.BarCode ?? string.Empty,
                        ScanTime = attach.ScanTime,
                        ExitId = attach.ExitId,
                        ExitName = attach.ExitName,
                        //先忽略快递
                        LogisticsName = attach.LogisticsName,
                        SortingMode = attach.SortingMode,
                        IsCreatedByLowerMachine = attach.IsCreatedByLowerMachine,
                        CommunicationMethod = value?.Type ?? CommunicationsType.None,
                        SortingCode = attach.Guid.ToString(),
                        InstructionInfos = new List<InstructionInfoModel>()
                        {
                            new()
                            {
                                InstructionContent = FormatInstructionContent(value,
                                    value?.DeviceCommunicationProtocol?.EncodeData(FunctionType.PackageInfoCompletedSignal, new object(),
                                        string.Empty, attach) ?? string.Empty),
                                InstructionGeneratedTime = sendTime,
                                InstructionType = InstructionType.PackageInfoCompletedSignal
                            }
                        },
                        ConnectionName = key
                    });
                }
            }
        }

        public void SendPackageCenter(
            int num,
            InstructionsAttach info,
            CancellationToken token = default)
        {
            var connectionName = _connectionInfos.Keys.FirstOrDefault();
            QueueConnectionWork(
                connectionName,
                () => SendPackageCenterAsync(connectionName, num, info, token));
        }

        private async Task SendPackageCenterAsync(
            string? connectionName,
            int num,
            InstructionsAttach info,
            CancellationToken token)
        {
            if (!string.IsNullOrEmpty(connectionName) &&
                _connectionInfos.TryGetValue(connectionName, out var value))
            {
                var key = connectionName;
                var isSend = false;
                var sendTime = DateTime.Now;
                if (value is { Type: CommunicationsType.SerialPort, SortingSerialPort: not null })
                {
                    //串口
                    if (value.SortingSerialPort.Status == SerialPortStatus.Running)
                    {
                        var message = string.Empty;
                        if (value.DeviceCommunicationProtocol is not null)
                        {
                            message = value.DeviceCommunicationProtocol.EncodeData(FunctionType.PackageCenter, new object(),
                                string.Empty, info);
                        }
                        value.SortingSerialPort.Send(message);
                        OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                        {
                            ConnectionName = key,
                            BarCode = info.BarCode,
                            Content = message,
                            ExitName = string.Empty,
                            FormatType = (FormatType)value.SortingSerialPort.FormatType,
                            Guid = num,
                            Time = sendTime = DateTime.Now,
                            Type = CommunicationType.Send,
                            Timestamp = info.Timestamp,
                        });
                        isSend = true;
                    }
                    else
                    {
                        OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                    }
                }
                else if (value is { Type: CommunicationsType.TCP, SortingTcp: not null })
                {
                    if (value.SortingTcp.ConnectionStatus == ConnectionStatus.Connected)
                    {
                        var message = string.Empty;
                        if (value.DeviceCommunicationProtocol is not null)
                        {
                            message = value.DeviceCommunicationProtocol.EncodeData(FunctionType.PackageCenter, new object(),
                                string.Empty, info);
                        }

                        var sendMessage = await SendTcpMessage(value, message, token);
                        OnCommunicationInfoEvent(new ConnectionCommunicationMessageInfo()
                        {
                            BarCode = info.BarCode,
                            Content = message,
                            ExitName = info.ExitName,
                            FormatType = value.SortingTcp.FormatType,
                            Guid = num,
                            Time = sendTime = DateTime.Now,
                            Timestamp = info.Timestamp,
                            Type = CommunicationType.Send,
                            ConnectionName = key
                        });
                        if (!sendMessage)
                        {
                            OnCommunicationExceptionEvent(new Exception("发送失败!"));
                        }
                        else
                        {
                            isSend = true;
                        }
                    }
                    else
                    {
                        OnCommunicationExceptionEvent(new Exception("下位机未连接!"));
                    }
                }

                //记录
                if (isSend)
                {
                    EventAggregator.Instance.Publish(new InstructionReceived()
                    {
                        Timestamp = info.Timestamp,
                        BarCode = info.BarCode ?? string.Empty,
                        ScanTime = info.ScanTime,
                        ExitId = info.ExitId,
                        ExitName = info.ExitName,
                        //先忽略快递
                        LogisticsName = info.LogisticsName,
                        SortingMode = info.SortingMode,
                        IsCreatedByLowerMachine = info.IsCreatedByLowerMachine,
                        CommunicationMethod = value?.Type ?? CommunicationsType.None,
                        SortingCode = info.Guid.ToString(),
                        InstructionInfos = new List<InstructionInfoModel>()
                        {
                            new()
                            {
                                InstructionContent = FormatInstructionContent(value,
                                    value?.DeviceCommunicationProtocol?.EncodeData(FunctionType.PackageCenter, new object(),
                                        string.Empty, info) ?? string.Empty),
                                InstructionGeneratedTime = sendTime,
                                InstructionType = InstructionType.PackageCenter
                            }
                        },
                        ConnectionName = key ?? string.Empty
                    });
                }
            }
        }

        /// <summary>根据格口解析其绑定的下位机连接名称。</summary>
        private string? ResolveConnectionName(long exitId)
        {
            if (exitId <= 0)
            {
                return null;
            }

            long? connectionId = _packageExitDefinitionInfoModels
                .FirstOrDefault(exit => exit.Id == exitId)?
                .CommunicationConnectionId;
            return connectionId > 0
                ? _connectionConfigInfoModels
                    .FirstOrDefault(connection => connection.Id == connectionId)?
                    .ConnectionName
                : null;
        }

        /// <summary>在每次下位机写入和重试前执行调用方提供的包裹身份复核。</summary>
        private static void EnsurePackageIdentityBeforeSend(InstructionsAttach attach, long exitId)
        {
            if (attach.ValidateBeforeSend is not null &&
                (exitId <= 0 || attach.ExitId != exitId))
            {
                throw new InvalidOperationException(
                    $"发送前格口复核失败，已禁止格口指令:目标格口={attach.ExitId},实际格口={exitId}");
            }

            var rejectionReason = attach.ValidateBeforeSend?.Invoke();
            if (!string.IsNullOrEmpty(rejectionReason))
            {
                throw new InvalidOperationException(
                    $"发送前包裹身份复核失败，已禁止格口指令:{rejectionReason}");
            }
        }

        private void QueueConnectionWork(string? connectionName, Func<Task> work)
        {
            _ = RunConnectionWorkAsync(connectionName, work);
        }

        private async Task RunConnectionWorkAsync(string? connectionName, Func<Task> work)
        {
            SemaphoreSlim sendGate;
            await _connectionLifecycleGate.WaitAsync();
            try
            {
                sendGate = _connectionSendGates.GetOrAdd(
                    connectionName ?? string.Empty,
                    static _ => new SemaphoreSlim(1, 1));
            }
            finally
            {
                _connectionLifecycleGate.Release();
            }

            await sendGate.WaitAsync();
            try
            {
                await work();
            }
            catch (OperationCanceledException)
            {
                // 调用方取消时停止当前发送，后续发送仍可继续。
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(e, "分拣连接任务执行失败");
                OnCommunicationExceptionEvent(e);
            }
            finally
            {
                sendGate.Release();
            }
        }

        protected virtual void OnCommunicationInfoEvent(ConnectionCommunicationMessageInfo e)
        {
            try
            {
                if (e.FormatType == FormatType.Hex)
                {
                    e.Content = HexDataFormatter.Normalize(e.Content);
                }
                if (e.Type == CommunicationType.Receive)
                {
                    var tryGetValue = _connectionInfos.TryGetValue(e.ConnectionName, out var connection);
                    if (tryGetValue && connection is not null)
                    {
                        if (connection.DeviceCommunicationProtocol is not null)
                        {
                            var deviceDecodeResult = connection.DeviceCommunicationProtocol.DecodeData(e.Content);
                            if (deviceDecodeResult is not null)
                            {
                                OnReceivedInstructionsEvent(new DeviceDecodeResult()
                                {
                                    ProtocolName = deviceDecodeResult.ProtocolName,
                                    KeywordPosition = deviceDecodeResult.KeywordPosition,
                                    Description = deviceDecodeResult.Description,
                                    ExceptionMessage = deviceDecodeResult.ExceptionMessage,
                                    IsException = deviceDecodeResult.IsException,
                                    RawContent = deviceDecodeResult.RawContent,
                                    Keyword = deviceDecodeResult.Keyword,
                                    Type = deviceDecodeResult.Type,
                                    Time = e.Time,
                                    ConnectionName = e.ConnectionName,
                                    CommandParsing = deviceDecodeResult.CommandParsing,
                                    SortingExceptionReturnType = deviceDecodeResult.SortingExceptionReturnType
                                });
                            }
                        }
                    }
                }

                if (!_communicationNotificationDispatcher.TryEnqueue(e))
                {
                    OnCommunicationExceptionEvent(new InvalidOperationException(
                        $"通信通知队列已停止，连接 {e.ConnectionName} 的日志未能入队"));
                }
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger()
                    .Error(exception, $"处理分拣通信数据失败:{e.ConnectionName}");
                OnCommunicationExceptionEvent(exception);
            }
        }

        /// <summary>在非关键队列中执行通信事件订阅者、日志和界面通知。</summary>
        private void PublishCommunicationNotification(ConnectionCommunicationMessageInfo e)
        {
            CommunicationInfoEvent?.Invoke(this, e);
            if (e is { Type: CommunicationType.Send, ExitName: null })
            {
                EventAggregator.Instance.Publish(new SortingLogInfoModel
                {
                    CreateTime = e.Time,
                    Message = $"连接:{e.ConnectionName},发送内容:{e.Content}",
                    Type = LogType.Information,
                });
            }
            else if (e.Type == CommunicationType.Receive)
            {
                EventAggregator.Instance.Publish(new SortingLogInfoModel
                {
                    CreateTime = e.Time,
                    Message = $"连接:{e.ConnectionName},接收内容:{e.Content}",
                    Type = LogType.Information,
                });
            }

        }

        protected virtual void OnCommunicationExceptionEvent(Exception e)
        {
            CommunicationExceptionEvent?.Invoke(this, e);
        }

        protected virtual void OnReceivedInstructionsEvent(DeviceDecodeResult e)
        {
            ReceivedInstructionsEvent?.Invoke(this, e);
        }

        protected virtual void OnHeartbeatError(Exception e)
        {
            HeartbeatError?.Invoke(this, e);
            EventAggregator.Instance.Publish(new SortingLogInfoModel
            {
                CreateTime = DateTime.Now,
                Message = $"心跳包异常",
                Type = LogType.Exception
            });
        }

        protected virtual void OnSendError(ExceptionEventArgs e)
        {
            SendError?.Invoke(this, e);
            EventAggregator.Instance.Publish(new SortingLogInfoModel
            {
                CreateTime = DateTime.Now,
                Message = $"发送异常:{e.ExceptionMessage}",
                Type = LogType.Exception
            });
        }

        protected virtual void OnDisconnected(ConnectionInfo e)
        {
            Disconnected?.Invoke(this, e);
            EventAggregator.Instance.Publish(new SortingLogInfoModel
            {
                CreateTime = DateTime.Now,
                Message = $"连接:{e.ConnectionName},断开",
                Type = LogType.Warning
            });
        }

        private byte[] HexStringToByteArray(string hexString)
        {
            if (HexDataFormatter.TryParse(hexString, out var bytes))
            {
                return bytes;
            }

            OnSendError(new ExceptionEventArgs()
            {
                ExceptionMessage = $"无效的十六进制数据:{hexString}"
            });
            return [];
        }

        /// <summary>
        /// 按连接的数据格式规范化需要展示和持久化的指令内容。
        /// </summary>
        private static string FormatInstructionContent(ConnectionInfo? connection, string content)
        {
            var formatType = connection switch
            {
                { Type: CommunicationsType.SerialPort, SortingSerialPort: not null } =>
                    (FormatType)connection.SortingSerialPort.FormatType,
                { Type: CommunicationsType.TCP, SortingTcp: not null } =>
                    connection.SortingTcp.FormatType,
                _ => FormatType.Ascii
            };
            return formatType == FormatType.Hex
                ? HexDataFormatter.Normalize(content)
                : content;
        }

        /// <summary>
        /// 在指定连接的应答通道中等待匹配内容。
        /// </summary>
        private async Task<bool> WaitForReply(string connection, string replyContent, TimeSpan timeOut)
        {
            if (timeOut <= TimeSpan.Zero)
            {
                return false;
            }

            _connectionInfos.TryGetValue(connection, out var connectionInfo);
            var expectedReplyContent = FormatInstructionContent(connectionInfo, replyContent);
            var channel = _replyChannels.GetOrAdd(connection, static _ => CreateReplyChannel());
            var deferredReplies = new List<string>();
            using var timeoutCancellation = new CancellationTokenSource(timeOut);
            try
            {
                while (await channel.Reader.WaitToReadAsync(timeoutCancellation.Token))
                {
                    while (channel.Reader.TryRead(out var result))
                    {
                        if (FormatInstructionContent(connectionInfo, result)
                            .Equals(expectedReplyContent, StringComparison.Ordinal))
                        {
                            return true;
                        }
                        deferredReplies.Add(result);
                    }
                }
            }
            catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested)
            {
                return false;
            }
            finally
            {
                foreach (var deferredReply in deferredReplies)
                {
                    channel.Writer.TryWrite(deferredReply);
                }
            }

            return false;
        }

        /// <summary>
        /// 创建容量受限的单连接应答通道。
        /// </summary>
        private static Channel<string> CreateReplyChannel()
        {
            return Channel.CreateBounded<string>(new BoundedChannelOptions(256)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest,
                AllowSynchronousContinuations = false
            });
        }

        /// <summary>
        /// 将接收内容写入对应连接的应答通道。
        /// </summary>
        private void EnqueueReply(string connectionName, string content)
        {
            var channel = _replyChannels.GetOrAdd(connectionName, static _ => CreateReplyChannel());
            channel.Writer.TryWrite(content);
        }

        /// <summary>
        /// 移除并完成指定连接的应答通道。
        /// </summary>
        private void RemoveReplyChannel(string connectionName)
        {
            if (_replyChannels.TryRemove(connectionName, out var channel))
            {
                channel.Writer.TryComplete();
            }
        }

        private Task<bool> SendTcpMessage(
            ConnectionInfo connection,
            string message,
            CancellationToken token = default)
        {
            if (connection.SortingTcp is null)
            {
                return Task.FromResult(false);
            }

            return connection.SortingTcp.FormatType == FormatType.Hex
                ? connection.SortingTcp.SendMessage(HexStringToByteArray(message), token)
                : connection.SortingTcp.SendMessage(message, token);
        }

        protected virtual void OnConnected(ConnectionInfo e)
        {
            Connected?.Invoke(this, e);
        }
    }
}
