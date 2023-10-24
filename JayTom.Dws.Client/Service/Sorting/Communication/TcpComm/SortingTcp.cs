using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;

namespace JayTom.Dws.Client.Service.Sorting.Communication.TcpComm {

    public class SortingTcp : BaseTcpOperations, ISortingTcp {
        private static Task? _heartbeatThread;
        private static CancellationTokenSource? _cancellationTokenSource;
        private readonly ConcurrentQueue<string> _heartbeatQueue = new();

        public SortingTcp(ITcpCommClient tcpCommClient, ITcpCommServer tcpCommServer) : base(tcpCommClient, tcpCommServer) {
            tcpCommClient.Communication += delegate (object? sender, CommunicationInfo info) {
                if (info.Type == CommunicationType.Receive) {
                    var tryDequeue = _heartbeatQueue.TryDequeue(out var data);
                    if (tryDequeue && !string.IsNullOrEmpty(data)) {
                        if (!info.Content.Equals(data)) {
                            _heartbeatQueue.Enqueue(data);
                        }
                    }
                }
                //后面需要删除这行
                NLog.LogManager.GetCurrentClassLogger().Error($"{JsonConvert.SerializeObject(info)}");
            };
            tcpCommServer.Communication += delegate (object? sender, CommunicationInfo info) {
                if (info.Type == CommunicationType.Receive) {
                    var tryDequeue = _heartbeatQueue.TryDequeue(out var data);
                    if (tryDequeue && !string.IsNullOrEmpty(data)) {
                        if (!info.Content.Equals(data)) {
                            _heartbeatQueue.Enqueue(data);
                        }
                    }
                }
            };
        }

        public event EventHandler<Exception>? HeartbeatError;

        public void StartHeartbeat(string heartbeatData, TimeSpan interval) {
            if (_heartbeatThread is null) {
                _cancellationTokenSource = new CancellationTokenSource();
                _heartbeatThread = Task.Run(async () => {
                    while (!_cancellationTokenSource.IsCancellationRequested) {
                        await Task.Delay(interval);
                        if (_heartbeatQueue.Any()) {
                            //异常
                            OnHeartbeatError(new Exception("心跳包未接收到回应!"));
                        }

                        if (ConnectionStatus == ConnectionStatus.Connected) {
                            _heartbeatQueue.Clear();
                            _heartbeatQueue.Enqueue(heartbeatData);
                            await SendMessage(heartbeatData);
                        }
                    }
                });
            }
        }

        public async void StopHeartbeat() {
            _cancellationTokenSource?.Cancel();
            if (_heartbeatThread != null) {
                await _heartbeatThread;
                _heartbeatThread?.Dispose();
                _heartbeatQueue.Clear();
            }

            _heartbeatThread = null;
        }

        public void Dispose() {
            StopHeartbeat();
            Close();
        }

        protected virtual async void OnHeartbeatError(Exception e) {
            await Task.Yield();
            HeartbeatError?.Invoke(this, e);
        }
    }
}