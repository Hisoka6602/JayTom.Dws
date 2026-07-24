using System;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;

namespace JayTom.Dws.Client.Service.Sorting.Communication.TcpComm {

    public class SortingTcp : BaseTcpOperations, ISortingTcp {
        private readonly object _heartbeatSync = new();
        private readonly ConcurrentQueue<string> _heartbeatQueue = new();
        private Task? _heartbeatTask;
        private CancellationTokenSource? _heartbeatCancellation;

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

        public void StartHeartbeat(string heartbeatData, FormatType formatType, TimeSpan interval) {
            lock (_heartbeatSync) {
                if (_heartbeatTask is { IsCompleted: false }) {
                    return;
                }

                _heartbeatCancellation?.Dispose();
                _heartbeatCancellation = new CancellationTokenSource();
                _heartbeatTask = RunHeartbeatAsync(heartbeatData, formatType, interval,
                    _heartbeatCancellation.Token);
            }
        }

        public void StopHeartbeat() {
            CancellationTokenSource? cancellation;
            Task? heartbeatTask;
            lock (_heartbeatSync) {
                cancellation = _heartbeatCancellation;
                heartbeatTask = _heartbeatTask;
                _heartbeatCancellation = null;
                _heartbeatTask = null;
            }

            cancellation?.Cancel();
            _heartbeatQueue.Clear();

            if (cancellation is not null) {
                if (heartbeatTask is null) {
                    cancellation.Dispose();
                }
                else {
                    _ = heartbeatTask.ContinueWith(_ => cancellation.Dispose(),
                        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
            }
        }

        public void Dispose() {
            StopHeartbeat();
            Close();
        }

        protected virtual void OnHeartbeatError(Exception e) {
            HeartbeatError?.Invoke(this, e);
        }

        private async Task RunHeartbeatAsync(string heartbeatData, FormatType formatType,
            TimeSpan interval, CancellationToken cancellationToken) {
            try {
                while (true) {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);

                    if (!_heartbeatQueue.IsEmpty) {
                        OnHeartbeatError(new Exception("心跳包未接收到回应!"));
                    }

                    if (ConnectionStatus != ConnectionStatus.Connected) {
                        continue;
                    }

                    _heartbeatQueue.Clear();
                    _heartbeatQueue.Enqueue(heartbeatData);
                    if (formatType == FormatType.Ascii) {
                        await SendMessage(heartbeatData).ConfigureAwait(false);
                    }
                    else if (formatType == FormatType.Hex) {
                        await SendMessage(ConvertHexStringToByteArray(heartbeatData)).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                // 正常停止心跳。
            }
            catch (Exception exception) {
                OnHeartbeatError(exception);
            }
        }
    }
}
