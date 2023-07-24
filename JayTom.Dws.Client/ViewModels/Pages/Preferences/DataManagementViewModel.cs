using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Client.ViewModels.Editors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {
    public class DataManagementViewModel : BindableBase {
        private DateTime _startTime;
        private DateTime _endTime;
        private int _pageCount;
        private int _pageIndex;
        private SnackbarMessageQueue _dataManagementMessageQueue = new(TimeSpan.FromSeconds(2));
        private long _timestampedGuid;
        private string _barCode = string.Empty;
        private float _minWeight;
        private float _maxWeight;

        private ObservableCollection<BarCodeItemModel> _barCodeItems = new()
        {
            new BarCodeItemModel()
            {
                Num = 1,
                TimestampedGuid=13800138000,
                Barcode = "SF12345678",
                Weight=(float)1.1,
                Volume=(float)2.2,
                Length=(float)3.3,
                Width=(float)4.4,
                Height=(float)5.5,
                ScanTime=DateTime.Now,
                RequestStatus= UploadStatus.Succeeded,
                RequestTime=DateTime.Now,
                RequestContent="上传内容",
                ResponseTime=DateTime.Now,
                ResponseContent="响应内容",
                BarcodeImagePath=@"C:\Users\77051\Desktop\15.jpg",
                PanoramaImagePath=@"C:\Users\77051\Desktop\16.jpg",
            },
            new BarCodeItemModel()
            {
                Num = 1,
                TimestampedGuid=13800138000,
                Barcode = "43333856561",
                Weight=(float)1.1,
                Volume=(float)2.2,
                Length=(float)3.3,
                Width=(float)4.4,
                Height=(float)5.5,
                ScanTime=DateTime.Now,
                RequestStatus= UploadStatus.Succeeded,
                RequestTime=DateTime.Now,
                RequestContent="上传内容",
                ResponseTime=DateTime.Now,
                ResponseContent="响应内容",
                BarcodeImagePath=@"C:\Users\77051\Desktop\15.jpg",
                PanoramaImagePath=@"C:\Users\77051\Desktop\16.jpg",
            },
        };

        /// <summary>
        /// 提示内容
        /// </summary>
        public SnackbarMessageQueue DataManagementMessageQueue {
            get => _dataManagementMessageQueue;
            set => SetProperty(ref _dataManagementMessageQueue, value);
        }

        #region 搜索工具栏条件

        public DateTime StartTime {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime EndTime {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        /// <summary>
        /// 时间戳
        /// </summary>
        public long TimestampedGuid {
            get => _timestampedGuid;
            set => SetProperty(ref _timestampedGuid, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode {
            get => _barCode;
            set => SetProperty(ref _barCode, value);
        }

        /// <summary>
        /// 最小重量
        /// </summary>
        public float MinWeight {
            get => _minWeight;
            set => SetProperty(ref _minWeight, value);
        }

        /// <summary>
        /// 最大重量
        /// </summary>
        public float MaxWeight {
            get => _maxWeight;
            set => SetProperty(ref _maxWeight, value);
        }

        /// <summary>
        /// 验证浮点数
        /// </summary>
        public ICommand ValidateInputCommand {
            get => new DelegateCommand<object>(ValidateInputDelegate);
        }

        private void ValidateInputDelegate(object args) {
            var type = args.GetType();
            Console.WriteLine(type);
            /*if (!Regex.IsMatch(args.Text, @"^[0-9]*(?:\.[0-9]*)?$")) {
                args.Handled = true; // 阻止字符输入
            }*/
        }

        #endregion 搜索工具栏条件

        #region 翻页变量

        /// <summary>
        /// 页数
        /// </summary>
        public int PageCount {
            get => _pageCount;
            set => SetProperty(ref _pageCount, value);
        }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageIndex {
            get => _pageIndex;
            set => SetProperty(ref _pageIndex, value);
        }

        #endregion 翻页变量

        #region 翻页执行方法

        /// <summary>
        /// 上一页
        /// </summary>
        public ICommand PreviousPageCommand {
            get => new DelegateCommand<object>(PreviousPageDelegate);
        }

        private void PreviousPageDelegate(object obj) {
        }

        /// <summary>
        /// 下一页
        /// </summary>
        public ICommand NextPageCommand {
            get => new DelegateCommand<object>(NextPageDelegate);
        }

        private void NextPageDelegate(object obj) {
        }

        /// <summary>
        /// 首页
        /// </summary>
        public ICommand FirstPageCommand {
            get => new DelegateCommand<object>(FirstPageDelegate);
        }

        private void FirstPageDelegate(object obj) {
        }

        /// <summary>
        /// 尾页
        /// </summary>
        public ICommand LastPageCommand {
            get => new DelegateCommand<object>(LastPageDelegate);
        }

        private void LastPageDelegate(object obj) {
        }

        //跳转
        public ICommand JumpPageCommand {
            get => new DelegateCommand<object>(JumpPageDelegate);
        }

        private void JumpPageDelegate(object obj) {
        }

        #endregion 翻页执行方法

        public ObservableCollection<BarCodeItemModel> BarCodeItems {
            get => _barCodeItems;
            set => SetProperty(ref _barCodeItems, value);
        }

        public ICommand OpenDateTimeDialogCommand {
            get => new DelegateCommand<object>(OpenDateTimeDialogDelegate);
        }

        private async void OpenDateTimeDialogDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var dataTimeEditor = new DataTimeEditor();
                if (dataTimeEditor.DataContext is DataTimeEditorViewModel model) {
                    model.Identifier = "DataManagementDialog";
                    if (obj?.ToString()?.Equals("StartTime") == true) {
                        model.SelectedDataTime = StartTime;
                        model.SelectedDate = StartTime;
                        model.SelectedTime = StartTime;
                    }
                    else {
                        model.SelectedDataTime = EndTime;
                        model.SelectedDate = EndTime;
                        model.SelectedTime = EndTime;
                    }

                    await DialogHost.Show(dataTimeEditor, model.Identifier);
                    if (model.IsOk) {
                        if (obj?.ToString()?.Equals("StartTime") == true) {
                            StartTime = model.SelectedDataTime.Value;
                        }
                        else if (obj?.ToString()?.Equals("EndTime") == true) {
                            if (DateTime.Now.CompareTo(model.SelectedDataTime.Value) < 0) {
                                //DataListMessageQueue.Enqueue("截止时间不能超过当前时间!");
                                EndTime = DateTime.Now;
                                return;
                            }
                            EndTime = model.SelectedDataTime.Value;
                        }
                    }
                }
            }, DispatcherPriority.Background);
        }
        public ICommand UploadStatusCommand {
            get => new DelegateCommand<BarCodeItemModel>(UploadStatusDelegate);
        }

        private void UploadStatusDelegate(BarCodeItemModel obj) {


        }
    }
}