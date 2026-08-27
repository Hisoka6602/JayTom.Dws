using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.IO.Ports;
using System.Threading;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalLog;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Client.Models.LogsItemModels;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences
{

    public class RealTimeLogViewModel : BindableBase
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private ObservableCollection<BaseLogItemModel> _logItems = new();
        /// <summary>
        /// 待显示日志的有界缓冲队列。
        /// </summary>
        private readonly Queue<BaseLogItemModel> _pendingLogs = new(500);
        /// <summary>
        /// 待显示日志队列同步锁。
        /// </summary>
        private readonly System.Threading.Lock _pendingLogsLock = new();
        /// <summary>
        /// 日志批量刷新信号。
        /// </summary>
        private readonly SemaphoreSlim _pendingLogSignal = new(0, 1);
        /// <summary>
        /// 日志批量刷新信号是否已经置位。
        /// </summary>
        private int _pendingLogSignalArmed;
        /// <summary>
        /// 日志刷新任务取消源。
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        /// <summary>
        /// 合并日志刷新的后台任务。
        /// </summary>
        private readonly Task _logUpdateWorker;

        public RealTimeLogViewModel(
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            //相机日志
            _eventBus.Subscribe<CameraLogInfoModel>(item =>
            {
                if (item is CameraLogInfoModel model)
                {
                    //添加

                    OnAddLog(model.CreateTime, $"[相机]-{model.Message}");
                }
            });
            //分拣日志
            _eventBus.Subscribe<SortingLogInfoModel>(item =>
            {
                if (item is SortingLogInfoModel model)
                {
                    //添加
                    var type = string.Empty;
                    if (model.CommunicationType is not null)
                    {
                        type = model.CommunicationType switch
                        {
                            CommunicationType.Receive => "(接收)",
                            CommunicationType.Send => "(发送)",
                            _ => type
                        };
                    }
                    OnAddLog(model.CreateTime, $"[分拣]-{type}{model.Message}");
                }
            });
            //称重日志队列
            _eventBus.Subscribe<WeighingLogInfoModel>(item =>
            {
                if (item is WeighingLogInfoModel model)
                {
                    //添加

                    OnAddLog(model.CreateTime, $"[称重]-{model.Message}");
                }
            });
            //体积日志队列
            _eventBus.Subscribe<VolumeLogInfoModel>(item =>
            {
                if (item is VolumeLogInfoModel model)
                {
                    //添加

                    OnAddLog(model.CreateTime, $"[体积]-{model.Message}");
                }
            });
            //Api日志队列
            _eventBus.Subscribe<ApiLogInfoModel>(item =>
            {
                if (item is ApiLogInfoModel model)
                {
                    //添加
                    OnAddLog(model.CreateTime, $"[Api]-{($"Url:{model.Url}\n耗时:{model.Duration * 1000:F2}ms")}");
                }
            });
            //Ocr日志
            _eventBus.Subscribe<OcrLogInfoModel>(item =>
            {
                if (item is OcrLogInfoModel model)
                {
                    //添加
                    OnAddLog(model.CreateTime, $"[Ocr]-{model.Message}");
                }
            });
            _eventBus.Subscribe<InputLogInfoModel>(item =>
            {
                if (item is InputLogInfoModel model)
                {
                    //添加
                    OnAddLog(model.CreateTime, $"[输入]-{model.Message}");
                }
            });
            _eventBus.Subscribe<JayTom.Dws.Client.Events.WindowsAction>(item =>
            {
                if (item is { Type: JayTom.Dws.Client.Events.WindowsActionType.Close })
                {
                    _cancellationTokenSource.Cancel();
                }
            });
            _logUpdateWorker = Task.Run(ProcessPendingLogs);

            /*//输出日志队列
            _eventBus.Subscribe<OutputLogInfoModel>(item => {
                if (item is OutputLogInfoModel model) {
                    //添加
                    OnAddLog(model.CreateTime, $"[输出]-{model.OutputContent}");
                }
            });*/
            //Ftp日志队列
            /*_eventBus.Subscribe<FtpLogInfoModel>(item => {
                if (item is FtpLogInfoModel model) {
                    //添加
                    OnAddLog(model.CreateTime, $"[Ftp信息]-{model.Message}");
                }
            });*/
        }

        public ObservableCollection<BaseLogItemModel> LogItems
        {
            get => _logItems;
            set => SetProperty(ref _logItems, value);
        }

        public void OnAddLog(DateTime createTime, string message)
        {
            lock (_pendingLogsLock)
            {
                if (_pendingLogs.Count >= 500)
                {
                    //日志突发时丢弃最旧的待显示项，避免阻塞主界面。
                    _pendingLogs.Dequeue();
                }
                _pendingLogs.Enqueue(new BaseLogItemModel
                {
                    CreateTime = createTime,
                    Message = message
                });
            }

            SignalPendingLogs();
        }

        /// <summary>
        /// 批量刷新实时日志，限制每帧对 UI 集合的修改数量。
        /// </summary>
        private async Task ProcessPendingLogs()
        {
            try
            {
                var cancellationToken = _cancellationTokenSource.Token;
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _pendingLogSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
                    Interlocked.Exchange(ref _pendingLogSignalArmed, 0);
                    await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).ConfigureAwait(false);

                    var batch = new List<BaseLogItemModel>(32);
                    lock (_pendingLogsLock)
                    {
                        while (batch.Count < 32 && _pendingLogs.Count > 0)
                        {
                            batch.Add(_pendingLogs.Dequeue());
                        }
                    }

                    var dispatcher = System.Windows.Application.Current?.Dispatcher;
                    if (dispatcher is null)
                    {
                        break;
                    }
                    await dispatcher.InvokeAsync(() =>
                    {
                        foreach (var logItem in batch)
                        {
                            LogItems.Insert(0, logItem);
                        }
                        while (LogItems.Count > 100)
                        {
                            LogItems.RemoveAt(LogItems.Count - 1);
                        }
                    }, DispatcherPriority.Background);

                    if (HasPendingLogs())
                    {
                        SignalPendingLogs();
                    }
                }
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
            {
                //应用关闭时正常退出。
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, "实时日志 UI 更新任务异常");
            }
        }

        public ICommand ClearLogCommand
        {
            get => new DelegateCommand<object>(ClearLogDelegate);
        }

        private void ClearLogDelegate(object obj)
        {
            LogItems.Clear();
            lock (_pendingLogsLock)
            {
                _pendingLogs.Clear();
            }
        }

        /// <summary>
        /// 判断是否仍有待显示日志。
        /// </summary>
        /// <returns>日志队列非空时返回 <see langword="true"/>。</returns>
        private bool HasPendingLogs()
        {
            lock (_pendingLogsLock)
            {
                return _pendingLogs.Count > 0;
            }
        }

        /// <summary>
        /// 置位日志刷新信号，并合并高频重复通知。
        /// </summary>
        private void SignalPendingLogs()
        {
            if (Interlocked.Exchange(ref _pendingLogSignalArmed, 1) == 0)
            {
                _pendingLogSignal.Release();
            }
        }
    }
}
