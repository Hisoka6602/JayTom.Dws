using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Client.Models.ContentInputSettingsModels;

namespace JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm {

    public class ClusterTcpInputManager : IClusterTcpInputManager {
        private static List<ITcpCommClient> _tcpCommClients = new();

        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        public event EventHandler<TcpInputBindingInfoModel>? ConnectionSuccessful;

        public event EventHandler<TcpInputBindingInfoModel>? ConnectionFailed;

        public event EventHandler<TcpInputBindingInfoModel>? Disconnected;

        public async Task<bool> Connect(TcpInputBindingInfoModel tcpInput) {
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

        public void Disconnect(TcpInputBindingInfoModel tcpInput) {
            var tcpCommClient = _tcpCommClients.FirstOrDefault(f => f.IpAddress.Equals(tcpInput.IpAddress) &&
                                                                    f.Port.Equals(tcpInput.Port));
            if (tcpCommClient is not null) {
                tcpCommClient.Close();
                _tcpCommClients.Remove(tcpCommClient);
            }
        }

        public async Task<bool> Reconnect(TcpInputBindingInfoModel tcpInput) {
            Disconnect(tcpInput);

            await Task.Delay(500);

            return await Connect(tcpInput);
        }

        public async Task ConnectBatch(List<TcpInputBindingInfoModel> tcpInputs) {
            await DisconnectAll();
            _tcpCommClients.Clear();

            var tasks = tcpInputs.Select(async s => {
                await Task.Delay(80);
                await Connect(s);
            }).ToList();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);
        }

        public async Task<bool> ConnectAll() {
            // 用于处理异步任务调度
            await Task.Yield();

            // 创建所有连接任务
            var tasks = _tcpCommClients.Select(async s => {
                var connect = await s.Connect();
                if (!connect) {
                    // 连接失败，触发失败事件
                    OnConnectionFailed(new TcpInputBindingInfoModel {
                        IpAddress = s.IpAddress,
                        Port = s.Port,
                    });
                }
                else {
                    // 连接成功，触发成功事件
                    OnConnectionSuccessful(new TcpInputBindingInfoModel {
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
            await Task.Yield();
            _tcpCommClients.ForEach(f => {
                f?.Close();
            });
        }

        protected virtual async void OnConnectionSuccessful(TcpInputBindingInfoModel e) {
            await Task.Yield();
            ConnectionSuccessful?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(TcpInputBindingInfoModel e) {
            await Task.Yield();
            Disconnected?.Invoke(this, e);
        }

        protected virtual async void OnMessageReceived(MessageReceivedEventArgs e) {
            await Task.Yield();
            MessageReceived?.Invoke(this, e);
        }

        protected virtual async void OnConnectionFailed(TcpInputBindingInfoModel e) {
            await Task.Yield();
            ConnectionFailed?.Invoke(this, e);
        }
    }
}