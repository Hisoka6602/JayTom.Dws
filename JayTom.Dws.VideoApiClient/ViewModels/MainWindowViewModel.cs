using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Security.Policy;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.VideoApiClient.Models;
using JayTom.Dws.VideoApiClient.Views.Editors;
using JayTom.Dws.VideoApiClient.ViewModels.Dialog;
using JayTom.Dws.VideoApiClient.ViewModels.Editors;

namespace JayTom.Dws.VideoApiClient.ViewModels {

    public class MainWindowViewModel : BindableBase {
        private readonly IDialogService _dialogService;
        private SnackbarMessageQueue _mainMessageQueue = new(TimeSpan.FromSeconds(2));
        private double _uniformCornerRadius = 10;
        private string _maxBtnIcon = "\xe600";
        private string _maxBtnToolTip = "最大化";
        private int _yesterdayBarcodeCount;
        private int _todayBarcodeCount;
        private DateTime? _nodeStartTime;
        private DateTime? _nodeEndTime;
        private List<string> _nodeList = new();
        private string? _selectedNode;
        private string? _barcode;
        private string? _cameraName;
        private int _pageCount;
        private int _pageIndex = 1;
        private int _pageSize = 100;

        private ObservableCollection<BarCodeItemModel> _barCodeItems = new()
        {
            new BarCodeItemModel()
            {
                BarCode = "SF123456789",
                CameraCustomName = "自定义名称1",
                CameraSerialNumber = "DAO555777888",
                NodeName = "节点1",
                Num = 1,
                ScanImageUrl = "https://pica.zhimg.com/v2-c6af95fde4c49be17489191ac9129fdf_r.jpg?source=1def8aca",
                ScanImageVisible = true,
                ScanTime = DateTime.Now,
                PanoramaImageItems = new ObservableCollection<PanoramaImageItemModel>()
                {
                    new()
                    {
                        ImageUrl = "https://pica.zhimg.com/80/c7ad985268e7144b588d7bf94eedb487_720w.webp?source=1def8aca",
                        ImageVisible = true
                    },
                    new()
                    {
                        ImageUrl = "https://picx.zhimg.com/80/v2-3ff3d6a85edb2f19d343668d24ed9269_720w.webp?source=1def8aca",
                        ImageVisible = true
                    },
                }
            },
            new BarCodeItemModel()
            {
                BarCode = "SF123456789",
                CameraCustomName = "自定义名称1",
                CameraSerialNumber = "DAO555777888",
                NodeName = "节点1",
                Num = 1,
                ScanImageUrl = "https://pic1.zhimg.com/80/v2-73b8307b2db44c617f4e8515ce67dd39_720w.webp?source=1def8aca",
                ScanImageVisible = true,
                ScanTime = DateTime.Now,
                PanoramaImageItems = new ObservableCollection<PanoramaImageItemModel>()
                {
                    new()
                    {
                        ImageUrl = "https://picx.zhimg.com/80/v2-3fcdfeacc10696e3f71d66a9ba6e9cc4_720w.webp?source=1def8aca",
                        ImageVisible = true
                    },
                    new()
                    {
                        ImageUrl = "https://picx.zhimg.com/80/v2-e5427c1e9ad8aaad99d643e7bd7e927b_720w.webp?source=1def8aca",
                        ImageVisible = true
                    },
                }
            },
        };

        public MainWindowViewModel(IDialogService dialogService) {
            _dialogService = dialogService;
        }

        public SnackbarMessageQueue MainMessageQueue {
            get => _mainMessageQueue;
            set => SetProperty(ref _mainMessageQueue, value);
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
        /// 昨天条码数量
        /// </summary>
        public int YesterdayBarcodeCount {
            get => _yesterdayBarcodeCount;
            set => SetProperty(ref _yesterdayBarcodeCount, value);
        }

        /// <summary>
        /// 今天条码数量
        /// </summary>
        public int TodayBarcodeCount {
            get => _todayBarcodeCount;
            set => SetProperty(ref _todayBarcodeCount, value);
        }

        /// <summary>
        /// 节点开始时间
        /// </summary>
        public DateTime? NodeStartTime {
            get => _nodeStartTime;
            set => SetProperty(ref _nodeStartTime, value);
        }

        /// <summary>
        /// 节点结束时间
        /// </summary>
        public DateTime? NodeEndTime {
            get => _nodeEndTime;
            set => SetProperty(ref _nodeEndTime, value);
        }

        /// <summary>
        /// 节点列表
        /// </summary>
        public List<string> NodeList {
            get => _nodeList;
            set => SetProperty(ref _nodeList, value);
        }

        /// <summary>
        /// 节点选中项
        /// </summary>
        public string? SelectedNode {
            get => _selectedNode;
            set => SetProperty(ref _selectedNode, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string? Barcode {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        /// <summary>
        /// 相机名称
        /// </summary>
        public string? CameraName {
            get => _cameraName;
            set => SetProperty(ref _cameraName, value);
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

        public ObservableCollection<BarCodeItemModel> BarCodeItems {
            get => _barCodeItems;
            set => SetProperty(ref _barCodeItems, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj) {
        }

        public ICommand SizeChangedCommand {
            get => new DelegateCommand<object>(SizeChangeDelegate);
        }

        private void SizeChangeDelegate(object obj) {
            if (obj is Window window) {
                window.MaxHeight = SystemParameters.WorkArea.Width;
                window.MaxHeight = SystemParameters.WorkArea.Height;
                if (window.WindowState == WindowState.Maximized ||
                    (window.Height >= SystemParameters.WorkArea.Width &&
                     window.Width >= SystemParameters.WorkArea.Height)) {
                    //直角
                    UniformCornerRadius = 0;
                }
                else {
                    UniformCornerRadius = 10;
                    //圆角
                }
                if (window.WindowState == WindowState.Maximized) {
                    MaxBtnIcon = "\xe72c";
                    MaxBtnToolTip = "还原";
                }
                else {
                    MaxBtnIcon = "\xe600";
                    MaxBtnToolTip = "最大化";
                }
            }
        }

        public ICommand MinWinCommand {
            get => new DelegateCommand<object>(MinWinDelegate);
        }

        private void MinWinDelegate(object obj) {
            if (obj is Window window) {
                window.WindowState = WindowState.Minimized;
            }
        }

        public ICommand MaxWinCommand {
            get => new DelegateCommand<object>(MaxWinDelegate);
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

        public ICommand CloseWinCommand {
            get => new DelegateCommand<object>(CloseWinDelegate);
        }

        private void CloseWinDelegate(object obj) {
            System.Windows.Application.Current.Shutdown();//关闭
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
                        model.SelectedDataTime = NodeStartTime ?? DateTime.Today;
                        model.SelectedDate = NodeStartTime ?? DateTime.Today;
                        model.SelectedTime = NodeStartTime ?? DateTime.Today;
                    }
                    else {
                        model.SelectedDataTime = NodeEndTime ?? DateTime.Now;
                        model.SelectedDate = NodeEndTime ?? DateTime.Now;
                        model.SelectedTime = NodeEndTime ?? DateTime.Now;
                    }

                    await DialogHost.Show(dataTimeEditor, model.Identifier);
                    if (model.IsOk) {
                        if (obj?.ToString()?.Equals("StartTime") == true) {
                            NodeStartTime = model.SelectedDataTime.Value;
                        }
                        else if (obj?.ToString()?.Equals("EndTime") == true) {
                            if (DateTime.Now.CompareTo(model.SelectedDataTime.Value) < 0) {
                                //DataListMessageQueue.Enqueue("截止时间不能超过当前时间!");
                                NodeEndTime = DateTime.Now;
                                return;
                            }
                            NodeEndTime = model.SelectedDataTime.Value;
                        }
                    }
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// 清空搜素条件
        /// </summary>
        public ICommand ClearSearchCriteriaCommand {
            get => new DelegateCommand<object>(ClearSearchCriteriaDelegate);
        }

        private async void ClearSearchCriteriaDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                NodeStartTime = null;
                NodeEndTime = null;
                SelectedNode = null;
                Barcode = null;
                CameraName = null;
            });
        }

        /// <summary>
        /// 搜索
        /// </summary>
        public ICommand SearchCommand {
            get => new DelegateCommand<object>(SearchDelegate);
        }

        private void SearchDelegate(object obj) {
            PageIndex = 1;
            LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
        }

        public ICommand SettingCommand {
            get => new DelegateCommand<object>(SettingDelegate);
        }

        private async void SettingDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                // _dialogService.Show("VideoDialog", new DialogParameters { { "VideoItem", obj } }, null);

                var settingDialog = new SettingDialog();
                if (settingDialog.DataContext is SettingDialogViewModel model) {
                    model.Identifier = "MainDialog";

                    await DialogHost.Show(settingDialog, model.Identifier);
                }
            });
        }

        /// <summary>
        /// 点击扫码图方法
        /// </summary>
        public ICommand ScanImageCommand {
            get => new DelegateCommand<BarCodeItemModel>(ScanImageDelegate);
        }

        private void ScanImageDelegate(BarCodeItemModel obj) {
            if (!string.IsNullOrEmpty(obj.ScanImageUrl)) {
                Process.Start(new ProcessStartInfo {
                    FileName = "cmd.exe",
                    Arguments = $"/c start {obj.ScanImageUrl}",
                    CreateNoWindow = true
                });
            }
        }

        /// <summary>
        /// 点击全景图方法
        /// </summary>
        public ICommand PanoramaImageCommand {
            get => new DelegateCommand<PanoramaImageItemModel>(PanoramaImageDelegate);
        }

        private void PanoramaImageDelegate(PanoramaImageItemModel obj) {
            if (!string.IsNullOrEmpty(obj.ImageUrl)) {
                Process.Start(new ProcessStartInfo {
                    FileName = "cmd.exe",
                    Arguments = $"/c start {obj.ImageUrl}",
                    CreateNoWindow = true
                });
            }
        }

        /// <summary>
        /// 点击视频方法
        /// </summary>
        public ICommand VideoImageCommand {
            get => new DelegateCommand<BarCodeItemModel>(VideoImageDelegate);
        }

        private void VideoImageDelegate(BarCodeItemModel obj) {
        }

        private async void LoadData(int pageIndex, DateTime? startTime, DateTime? endTime,
            string? nodeName, string? barCode, string? cameraName) {
            return;
        }
    }
}