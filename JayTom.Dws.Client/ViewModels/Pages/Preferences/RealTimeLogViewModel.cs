using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.IO.Ports;
using System.Threading;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Models.LogsItemModels;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class RealTimeLogViewModel : BindableBase {
        private ObservableCollection<BaseLogItemModel> _logItems = new();
        private SemaphoreSlim _addSlim = new(1);

        public RealTimeLogViewModel() {
            //相机日志
            EventAggregator.Instance.Subscribe<CameraLogInfoModel>(item => {
                if (item is CameraLogInfoModel model) {
                    //添加

                    OnAddLog(model.CreateTime, $"[相机]-{model.Message}");
                }
            });
            //分拣日志
            EventAggregator.Instance.Subscribe<SortingLogInfoModel>(item => {
                if (item is SortingLogInfoModel model) {
                    //添加
                    var type = string.Empty;
                    if (model.CommunicationType is not null) {
                        type = model.CommunicationType switch {
                            CommunicationType.Receive => "(接收)",
                            CommunicationType.Send => "(发送)",
                            _ => type
                        };
                    }
                    OnAddLog(model.CreateTime, $"[分拣]-{type}{model.Message}");
                }
            });
            //称重日志队列
            EventAggregator.Instance.Subscribe<WeighingLogInfoModel>(item => {
                if (item is WeighingLogInfoModel model) {
                    //添加

                    OnAddLog(model.CreateTime, $"[称重]-{model.Message}");
                }
            });
            //体积日志队列
            EventAggregator.Instance.Subscribe<VolumeLogInfoModel>(item => {
                if (item is VolumeLogInfoModel model) {
                    //添加

                    OnAddLog(model.CreateTime, $"[体积]-{model.Message}");
                }
            });
            //Api日志队列
            EventAggregator.Instance.Subscribe<ApiLogInfoModel>(item => {
                if (item is ApiLogInfoModel model) {
                    //添加
                    OnAddLog(model.CreateTime, $"[Api]-{($"Url:{model.Url}\n耗时:{model.Duration * 1000:F2}ms")}");
                }
            });
            //Ocr日志
            EventAggregator.Instance.Subscribe<OcrLogInfoModel>(item => {
                if (item is OcrLogInfoModel model) {
                    //添加
                    OnAddLog(model.CreateTime, $"[Ocr]-{model.Message}");
                }
            });
            /*//输出日志队列
            EventAggregator.Instance.Subscribe<OutputLogInfoModel>(item => {
                if (item is OutputLogInfoModel model) {
                    //添加
                    OnAddLog(model.CreateTime, $"[输出]-{model.OutputContent}");
                }
            });*/
            //Ftp日志队列
            /*EventAggregator.Instance.Subscribe<FtpLogInfoModel>(item => {
                if (item is FtpLogInfoModel model) {
                    //添加
                    OnAddLog(model.CreateTime, $"[Ftp信息]-{model.Message}");
                }
            });*/
        }

        public ObservableCollection<BaseLogItemModel> LogItems {
            get => _logItems;
            set => SetProperty(ref _logItems, value);
        }

        public async void OnAddLog(DateTime createTime, string message) {
            try {
                await _addSlim.WaitAsync();
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    LogItems.Insert(0, new BaseLogItemModel() {
                        CreateTime = createTime,
                        Message = message
                    });
                    if (LogItems.Count > 100) {
                        LogItems.RemoveAt(LogItems.Count - 1);
                    }
                });
            }
            finally {
                _addSlim.Release();
            }
        }

        public ICommand ClearLogCommand {
            get => new DelegateCommand<object>(ClearLogDelegate);
        }

        private async void ClearLogDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                LogItems.Clear();
            });
        }
    }
}