using System;
using System.Linq;
using System.IO.Ports;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using JayTom.Dws.Plugin.SerialPort;
using System.Collections.Concurrent;

namespace JayTom.Dws.Client.Service.Sorting.Communication.SerialComm {

    public class SortingSerialPort : BaseSerialPort, ISortingSerialPort {
        private SemaphoreSlim _semaphore = new(1);
        private static Task? _heartbeatThread;
        private static CancellationTokenSource? _cancellationTokenSource;
        private readonly ConcurrentQueue<string> _heartbeatQueue = new();

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

        public void StartHeartbeat(string heartbeatData, TimeSpan interval) {
            //开启心跳线程
            if (_heartbeatThread is null) {
                _cancellationTokenSource = new CancellationTokenSource();
                _heartbeatThread = Task.Run(async () => {
                    while (!_cancellationTokenSource.IsCancellationRequested) {
                        await Task.Delay(interval);

                        if (_heartbeatQueue.Any()) {
                            //异常
                            OnHeartbeatError(new Exception("心跳包未接收到回应!"));
                        }

                        if (base.Status == SerialPortStatus.Running) {
                            _heartbeatQueue.Clear();
                            _heartbeatQueue.Enqueue(heartbeatData);
                            Send(heartbeatData);
                        }
                    }
                });
            }
        }

        public async void StopHeartbeat() {
            //停止心跳线程
            _cancellationTokenSource?.Cancel();
            await Task.Delay(200);
            if (_heartbeatThread != null) {
                await _heartbeatThread;
                _heartbeatThread?.Dispose();
            }

            _heartbeatThread = null;
        }

        protected virtual async void OnHeartbeatError(Exception e) {
            await Task.Yield();
            HeartbeatError?.Invoke(this, e);
        }
    }
}