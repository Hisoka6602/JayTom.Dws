using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Regions;
using System.Windows;
using Prism.Commands;
using System.Threading;
using JayTom.Dws.Plugin;
using System.Windows.Input;
using System.Globalization;
using Prism.Services.Dialogs;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using JayTom.Dws.Plugin.Speech;
using JayTom.Dws.Device.Camera;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.TemporaryClient.Models;
using JayTom.Dws.TemporaryClient.Service;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.TemporaryClient.Views.Dialog;
using JayTom.Dws.TemporaryClient.Views.Editors;
using JayTom.Dws.TemporaryClient.ViewModels.Dialog;
using JayTom.Dws.TemporaryClient.ViewModels.Editors;
using JayTom.Dws.Infrastructure.Repository.LocalData;

namespace JayTom.Dws.TemporaryClient.ViewModels {

    public class MainWindowViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        private readonly IExcel _excel;
        private readonly IBarCodeRepository _barCodeRepository;
        private readonly ISpeech _speech;
        private readonly I3DCamera _camera;
        private readonly IDialogService _dialogService;
        private double _uniformCornerRadius = 10;
        private string _maxBtnIcon = "\xe600";
        private string _maxBtnToolTip = "Maximize";
        private DateTime _startTime = DateTime.Today;
        private DateTime _endTime = DateTime.Now;
        private string _barCode = string.Empty;
        private int _pageCount;
        private int _pageIndex;
        private int _pageSize = 1000;
        private SnackbarMessageQueue _mainMessageQueue = new(TimeSpan.FromSeconds(2));
        private string _length = "0";
        private string _width = "0";
        private string _height = "0";
        private string _requestStatus = string.Empty;
        private string _displayBarcode = string.Empty;

        public MainWindowViewModel(IRegionManager regionManager,
            IExcel excel, IBarCodeRepository barCodeRepository,
            IBarcodeScannerService barcodeScannerService, ISpeech speech
            , I3DCamera camera, IDialogService dialogService) {
            _regionManager = regionManager;
            _excel = excel;
            _barCodeRepository = barCodeRepository;
            _speech = speech;
            _camera = camera;
            _dialogService = dialogService;
            barcodeScannerService.ScanCompleted += async delegate (object? sender, ScanCompletedEventArgs args) {
                switch (args.RequestStatus) {
                    //播报声音
                    case 1:
                        _speech.PlaySuccess();
                        break;

                    case 2:
                        _speech.PlayFail();
                        break;
                }
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    //显示长、宽、高
                    Length = args.Length.ToString(CultureInfo.InvariantCulture);
                    Width = args.Width.ToString(CultureInfo.InvariantCulture);
                    Height = args.Height.ToString(CultureInfo.InvariantCulture);
                    //存到文件
                    var insert = await _barCodeRepository.Insert(new BarCodeInfoModel() {
                        TimestampedGuid = args.TimestampedGuid,
                        Barcode = args.Barcode,
                        Weight = args.Weight,
                        Length = args.Length,
                        Width = args.Width,
                        Height = args.Height,
                        ScanTime = args.ScanTime,
                        RequestStatus = args.RequestStatus,
                        RequestTime = args.RequestTime,
                        RequestContent = args.RequestContent,
                        ResponseTime = args.ResponseTime,
                        ResponseContent = args.ResponseContent,
                    }, new CancellationToken(false));
                    if (!insert) {
                        MainMessageQueue.Enqueue("Failed to save data.");
                    }

                    DisplayBarcode = args.Barcode;
                    RequestStatus = args.RequestStatus switch {
                        0 => "NotUploaded",
                        1 => "Success",
                        2 => "Failure",
                        _ => string.Empty
                    };
                });
            };
            _camera.Excepted += delegate (object? sender, Exception exception) {
                MainMessageQueue.Enqueue($"Camera loading error[{exception.Message}]");
            };
        }

        public double UniformCornerRadius {
            get => _uniformCornerRadius;
            set => SetProperty(ref _uniformCornerRadius, value);
        }

        /// <summary>
        /// 最大化按钮图标
        /// </summary>
        public string MaxBtnIcon {
            get => _maxBtnIcon;
            set => SetProperty(ref _maxBtnIcon, value);
        }

        /// <summary>
        /// 最大化按钮提示内容
        /// </summary>
        public string MaxBtnToolTip {
            get => _maxBtnToolTip;
            set => SetProperty(ref _maxBtnToolTip, value);
        }

        /// <summary>
        /// 长
        /// </summary>
        public string Length {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        /// <summary>
        /// 宽
        /// </summary>
        public string Width {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// 高
        /// </summary>
        public string Height {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        /// <summary>
        /// 状态
        /// </summary>
        public string RequestStatus {
            get => _requestStatus;
            set => SetProperty(ref _requestStatus, value);
        }

        /// <summary>
        /// 显示的条码
        /// </summary>

        public string DisplayBarcode {
            get => _displayBarcode;
            set => SetProperty(ref _displayBarcode, value);
        }

        /// <summary>
        /// 数据列表
        /// </summary>
        public ObservableCollection<BarCodeItemModel> BarCodeItems { get; set; } = new();

        public DateTime StartTime {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime EndTime {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

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

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode {
            get => _barCode;
            set => SetProperty(ref _barCode, value);
        }

        /// <summary>
        /// 提示内容
        /// </summary>
        public SnackbarMessageQueue MainMessageQueue {
            get => _mainMessageQueue;
            set => SetProperty(ref _mainMessageQueue, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        public ICommand MinWinCommand {
            get => new DelegateCommand<object>(MinWinDelegate);
        }

        public ICommand MaxWinCommand {
            get => new DelegateCommand<object>(MaxWinDelegate);
        }

        public ICommand CloseWinCommand {
            get => new DelegateCommand<object>(CloseWinDelegate);
        }

        public ICommand SizeChangedCommand {
            get => new DelegateCommand<object>(SizeChangeDelegate);
        }

        public ICommand PageSwitchingCommand {
            get => new DelegateCommand<object>(PageSwitchingDelegate);
        }

        /// <summary>
        /// 搜索
        /// </summary>
        public ICommand SearchCommand {
            get => new DelegateCommand<object>(SearchDelegate);
        }

        private void SearchDelegate(object obj) {
            PageIndex = 1;
            LoadData(PageIndex);
        }

        public ICommand ExportCommand {
            get => new DelegateCommand<object>(ExportDelegate);
        }

        private async void ExportDelegate(object obj) {
            //导出
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog() {
                Title = "Please select the location to save the file.",
                Filter = "Excel文件(xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            };
            if (saveFileDialog.ShowDialog() == true) {
                var exportDialog = new ExportDialog();
                if (exportDialog.DataContext is ExportDialogViewModel model) {
                    model.FilePath = saveFileDialog.FileName;
                    model.Identifier = "MainDialog";
                    model.Message = "Retrieving data...";
                    DialogHost.Show(exportDialog, model.Identifier);

                    await _excel.Export(saveFileDialog.FileName,
                        $"BarCodeItems",
                        "BarCodeItems", BarCodeItems?.ToList() ?? new List<BarCodeItemModel>(),
                        new List<string>(), async p => {
                            model.Progress = p;
                            model.ProgressText = $"{p}%";
                            if (p == 100) {
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                                    if (DialogHost.IsDialogOpen(model.Identifier)) {
                                        DialogHost.Close(model.Identifier);
                                    }
                                });
                            }
                        }, e => {
                            MainMessageQueue?.Enqueue(e.Message);
                        });
                    //查询当前条件所有内容
                    //弹导出框
                    /*var (key, value) = await _appDataService.GetBarCodeItems(StartTime, EndTime, BarCode);
                    if (key && value is BarCodeDto dto) {
                        if (dto is { Total: > 0, BarCodeItems: List<BarCodeInfoModel> models }) {
                            var itemModels = models.Select((s, i) => new BarCodeItemModel() {
                                Num = i + 1,
                                BarCode = s.BarCode,
                                WeighTime = s.WeighTime,
                                Weight = s.Weight
                            })?.ToList();
                            //导出

                            await _excel.Export(saveFileDialog.FileName,
                                 $"条码记录",
                                 "条码记录", itemModels ?? new List<BarCodeItemModel>(),
                                 new List<string>(), async p => {
                                     model.Progress = p;
                                     model.ProgressText = $"{p}%";
                                     if (p == 100) {
                                         await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                                             if (DialogHost.IsDialogOpen(model.Identifier)) {
                                                 DialogHost.Close(model.Identifier);
                                             }
                                         });
                                     }
                                 }, e => {
                                     MainMessageQueue?.Enqueue(e.Message);
                                 });
                        }
                    }
                    else {
                        MainMessageQueue?.Enqueue(value);
                    }*/
                }
            }
        }

        public ICommand OpenDateTimeDialogCommand {
            get => new DelegateCommand<object>(OpenDateTimeDialogDelegate);
        }

        private async void OpenDateTimeDialogDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var dataTimeEditor = new DataTimeEditor();
                if (dataTimeEditor.DataContext is DataTimeEditorViewModel model) {
                    model.Identifier = "MainDialog";
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

        /// <summary>
        /// 上一页
        /// </summary>
        public ICommand PreviousPageCommand {
            get => new DelegateCommand<object>(PreviousPageDelegate);
        }

        private void PreviousPageDelegate(object obj) {
            if (PageIndex <= 1) return;
            PageIndex--;
            LoadData(PageIndex);
        }

        public ICommand NextPageCommand {
            get => new DelegateCommand<object>(NextPageDelegate);
        }

        private void NextPageDelegate(object obj) {
            if (PageIndex >= PageCount) return;
            PageIndex++;
            LoadData(PageIndex);
        }

        public ICommand FirstPageCommand {
            get => new DelegateCommand<object>(FirstPageDelegate);
        }

        private void FirstPageDelegate(object obj) {
            PageIndex = 1;
            LoadData(PageIndex);
        }

        public ICommand LastPageCommand {
            get => new DelegateCommand<object>(LastPageDelegate);
        }

        private void LastPageDelegate(object obj) {
            if (PageCount > 0) {
                PageIndex = PageCount;
                LoadData(PageIndex);
            }
        }

        //跳转
        public ICommand JumpPageCommand {
            get => new DelegateCommand<object>(JumpPageDelegate);
        }

        private void JumpPageDelegate(object obj) {
            if (PageIndex >= 0 && PageIndex <= PageCount) {
                LoadData(PageIndex);
            }
            else {
                PageIndex = 1;
            }
        }

        public ICommand CameraViewCommand {
            get => new DelegateCommand<object>(CameraViewDelegate);
        }

        private void CameraViewDelegate(object obj) {
            _dialogService.ShowDialog("CameraView");
        }

        /// <summary>
        /// 页面切换
        /// </summary>
        /// <param name="obj"></param>
        private async void PageSwitchingDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                _regionManager.Regions["ContentRegion"].RequestNavigate(obj.ToString());
            });
        }

        private void SizeChangeDelegate(object obj) {
            if (obj is Window window) {
                window.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                window.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                if (window.WindowState == WindowState.Maximized ||
                    (window.Height >= SystemParameters.MaximizedPrimaryScreenHeight &&
                     window.Width >= SystemParameters.MaximizedPrimaryScreenWidth)) {
                    //直角
                    UniformCornerRadius = 0;
                }
                else {
                    UniformCornerRadius = 10;
                    //圆角
                }
                if (window.WindowState == WindowState.Maximized) {
                    MaxBtnIcon = "\xe72c";
                    MaxBtnToolTip = "Restore";
                }
                else {
                    MaxBtnIcon = "\xe600";
                    MaxBtnToolTip = "Maximize";
                }
            }
        }

        private void CloseWinDelegate(object obj) {
            System.Windows.Application.Current.Shutdown();//关闭
        }

        private void MaxWinDelegate(object obj) {
            if (obj is Window window) {
                if (window.WindowState == WindowState.Maximized) {
                    window.WindowState = WindowState.Normal;
                    return;
                }
                window.WindowState = WindowState.Maximized;
            }
        }

        private async void LoadedDelegate(object obj) {
            /*await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                _regionManager.Regions["ContentRegion"].RequestNavigate("HomeView");
            });*/
        }

        private void MinWinDelegate(object obj) {
            if (obj is Window window) {
                window.WindowState = WindowState.Minimized;
            }
        }

        private async void LoadData(int pageIndex) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                //等待框
                var loadingDialog = new LoadingDialog();
                if (loadingDialog.DataContext is LoadingDialogViewModel model) {
                    model.Identifier = "MainDialog";
                    DialogHost.Show(loadingDialog, model.Identifier);
                    BarCodeItems.Clear();
                    //获取条数
                    var total = await _barCodeRepository.Total(s =>
                            (s.ScanTime.CompareTo(StartTime) >= 0) &&
                            s.ScanTime.CompareTo(EndTime) <= 0 &&
                            (string.IsNullOrWhiteSpace(BarCode) || s.Barcode.Contains(BarCode)),
                        new CancellationToken(false));
                    if (total > 0) {
                        PageCount = total / _pageSize + (total % _pageSize > 0 ? 1 : 0);
                        var infoModels = await _barCodeRepository.SelectOrderByDescending(s =>
                                (s.ScanTime.CompareTo(StartTime) >= 0) &&
                                s.ScanTime.CompareTo(EndTime) <= 0 &&
                                (string.IsNullOrWhiteSpace(BarCode) || s.Barcode.Contains(BarCode)),
                            o => o.ScanTime, pageIndex - 1, _pageSize, new CancellationToken(false));
                        if (infoModels?.Any() == true) {
                            var itemModels = infoModels?.Select((s, i) => new BarCodeItemModel {
                                Num = i + 1,
                                TimestampedGuid = s.TimestampedGuid,
                                Barcode = s.Barcode,
                                Weight = s.Weight,
                                Length = s.Length,
                                Width = s.Width,
                                Height = s.Height,
                                ScanTime = s.ScanTime,
                                RequestStatus = s.RequestStatus switch {
                                    0 => "NotUploaded",
                                    1 => "Success",
                                    2 => "Failure",
                                    _ => string.Empty
                                },
                                RequestTime = s.RequestTime,
                                RequestContent = s.RequestContent,
                                ResponseTime = s.ResponseTime,
                                ResponseContent = s.ResponseContent,
                            })?.ToList();
                            BarCodeItems.AddRange(itemModels);
                        }
                        else {
                            MainMessageQueue?.Enqueue("Error loading data. Please try again.");
                        }
                    }
                    else {
                        MainMessageQueue?.Enqueue("No data matching the criteria found.");
                    }

                    if (DialogHost.IsDialogOpen(model.Identifier)) {
                        DialogHost.Close(model.Identifier);
                    }
                }
            });
        }
    }
}