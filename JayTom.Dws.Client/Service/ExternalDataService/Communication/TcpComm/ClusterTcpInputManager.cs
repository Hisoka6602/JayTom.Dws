using System;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Client.Models.ContentInputSettingsModels;

namespace JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm {

    public class ClusterTcpInputManager : IClusterTcpInputManager {
        private static List<ITcpCommClient> _tcpCommClients = new();
        private static readonly SemaphoreSlim ConnectionSlim = new(1);

        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        public event EventHandler<TcpInputBindingInfo>? ConnectionSuccessful;

        public event EventHandler<TcpInputBindingInfo>? ConnectionFailed;

        public event EventHandler<TcpInputBindingInfo>? Disconnected;

        public async Task<bool> Connect(TcpInputBindingInfo tcpInput) {
            var tcpCommClient = _tcpCommClients.FirstOrDefault(f => f.IpAddress.Equals(tcpInput.IpAddress) &&
                                                                     f.Port.Equals(tcpInput.Port));

            if (tcpCommClient is not null) {
                tcpCommClient.Close();
                await Task.Delay(500);
                _tcpCommClients.Remove(tcpCommClient);
            }
            var tcpClient = new TouchSocketTcpClient() {
                FormatType = FormatType.Ascii
            };
            tcpClient.Connected += (sender, s) => {
                OnConnectionSuccessful(tcpInput);
            };
            tcpClient.Communication += (sender, info) => {
                //通讯事件
                OnMessageReceived(new MessageReceivedEventArgs() {
                    Info = tcpInput,
                    Message = info.Content
                });
            };
            tcpClient.Disconnected += (sender, s) => {
                OnDisconnected(tcpInput);
            };
            _tcpCommClients.Add(tcpClient);
            var connect = await tcpClient.Connect(tcpInput.IpAddress, tcpInput.Port);
            if (!connect) {
                OnConnectionFailed(tcpInput);
            }

            return connect;
        }

        public void Disconnect(TcpInputBindingInfo tcpInput) {
            var tcpCommClient = _tcpCommClients.FirstOrDefault(f => f.IpAddress.Equals(tcpInput.IpAddress) &&
                                                                    f.Port.Equals(tcpInput.Port));
            if (tcpCommClient is not null) {
                tcpCommClient.Close();
                _tcpCommClients.Remove(tcpCommClient);
            }
        }

        public async Task<bool> Reconnect(TcpInputBindingInfo tcpInput) {
            Disconnect(tcpInput);

            await Task.Delay(500);

            return await Connect(tcpInput);
        }

        public async Task<KeyValuePair<bool, string>> ConnectBatch(List<TcpInputBindingInfo> tcpInputs) {
            try {
                await ConnectionSlim.WaitAsync();
                var lockObj = new object();
                var successfulCount = 0;
                //await DisconnectAll();
                _tcpCommClients.Clear();

                var tasks = tcpInputs.Select(async s => {
                    await Task.Delay(80);
                    var tcpCommClient = _tcpCommClients.FirstOrDefault(f => f.IpAddress.Equals(s.IpAddress) &&
                                                                             f.Port.Equals(s.Port));

                    if (tcpCommClient is null || tcpCommClient.ConnectionStatus != ConnectionStatus.Connected) {
                        var connect = await Connect(s);
                        if (connect) {
                            lock (lockObj) {
                                successfulCount++;
                            }
                        }
                    }
                }).ToList();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);
                return new KeyValuePair<bool, string>(successfulCount > 0, $"成功连接数量:{successfulCount}");
            }
            finally {
                ConnectionSlim.Release();
            }
        }

        public async Task<bool> ConnectAll() {
            // 用于处理异步任务调度
            await Task.Yield();

            // 创建所有连接任务
            var tasks = _tcpCommClients.Select(async s => {
                var connect = await s.Connect();
                if (!connect) {
                    // 连接失败，触发失败事件
                    OnConnectionFailed(new TcpInputBindingInfo {
                        IpAddress = s.IpAddress,
                        Port = s.Port,
                    });
                }
                else {
                    // 连接成功，触发成功事件
                    OnConnectionSuccessful(new TcpInputBindingInfo {
                        IpAddress = s.IpAddress,
                        Port = s.Port,
                    });
                }
            }).ToList();

            // 添加超时机制
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));

            // 等待所有任务完成或超时
            var allTasks = Task.WhenAll(tasks);
            var completedTask = await Task.WhenAny(allTasks, timeoutTask);

            // 如果是超时任务先完成，返回 false
            if (completedTask == timeoutTask) {
                return false;
            }

            // 如果所有任务完成，返回 true
            return allTasks.IsCompletedSuccessfully;
        }

        public async Task DisconnectAll() {
            _tcpCommClients.ForEach(f => {
                f?.Close();
            });
            await Task.Delay(2000);
        }

        public ITcpCommClient? GetTcpInputInfo(string ipAddress, int port) {
            return _tcpCommClients.FirstOrDefault(f => f.IpAddress.Equals(ipAddress) &&
                                                 f.Port.Equals(port));
        }

        protected virtual async void OnConnectionSuccessful(TcpInputBindingInfo e) {
            await Task.Yield();
            ConnectionSuccessful?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(TcpInputBindingInfo e) {
            await Task.Yield();
            Disconnected?.Invoke(this, e);
        }

        protected virtual async void OnMessageReceived(MessageReceivedEventArgs e) {
            await Task.Yield();
            MessageReceived?.Invoke(this, e);
        }

        protected virtual async void OnConnectionFailed(TcpInputBindingInfo e) {
            await Task.Yield();
            ConnectionFailed?.Invoke(this, e);
        }
    }
}