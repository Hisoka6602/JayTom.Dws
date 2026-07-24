using System;
using System.IO.Ports;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using JayTom.Dws.Plugin.SerialPort;
using System.Collections.Concurrent;

namespace JayTom.Dws.Client.Service.Sorting.Communication.SerialComm {

    public class SortingSerialPort : BaseSerialPort, ISortingSerialPort {
        private readonly object _heartbeatSync = new();
        private readonly ConcurrentQueue<string> _heartbeatQueue = new();
        private Task? _heartbeatTask;
        private CancellationTokenSource? _heartbeatCancellation;

        public SortingSerialPort(SerialPort serialPort) : base(serialPort) {
            base.DataReceived += (sender, args) => {
                var tryDequeue = _heartbeatQueue.TryDequeue(out var data);
                if (tryDequeue && !string.IsNullOrEmpty(data)) {
                    if (!args.AsciiMessage.Equals(data)) {
                        _heartbeatQueue.Enqueue(data);
                    }
                }
            };
        }

        public new void Dispose() {
            StopHeartbeat();
            base.Dispose();
        }

        public event EventHandler<Exception>? HeartbeatError;

        public void StartHeartbeat(string heartbeatData, SerialPortFormat formatType, TimeSpan interval) {
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

        protected virtual void OnHeartbeatError(Exception e) {
            HeartbeatError?.Invoke(this, e);
        }

        private async Task RunHeartbeatAsync(string heartbeatData, SerialPortFormat formatType,
            TimeSpan interval, CancellationToken cancellationToken) {
            try {
                while (true) {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);

                    if (!_heartbeatQueue.IsEmpty) {
                        OnHeartbeatError(new Exception("心跳包未接收到回应!"));
                    }

                    if (base.Status != SerialPortStatus.Running) {
                        continue;
                    }

                    _heartbeatQueue.Clear();
                    _heartbeatQueue.Enqueue(heartbeatData);
                    if (formatType == SerialPortFormat.Ascii) {
                        Send(heartbeatData);
                    }
                    else {
                        Send(ConvertHexStringToByteArray(heartbeatData));
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
