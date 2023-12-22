using System;
using Prism.Mvvm;
using System.Net;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using System.Windows.Input;
using System.Globalization;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Security.Policy;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Threading.Channels;
using System.Collections.Generic;
using JayTom.Dws.VideoApiClient.Api;
using System.Collections.ObjectModel;
using JayTom.Dws.VideoApiClient.Models;
using JayTom.Dws.VideoApiClient.Views.Editors;
using JayTom.Dws.VideoApiClient.ViewModels.Dialog;
using JayTom.Dws.VideoApiClient.ViewModels.Editors;

namespace JayTom.Dws.VideoApiClient.ViewModels {

    public class MainWindowViewModel : BindableBase {
        private readonly IDialogService _dialogService;
        private readonly IVideoApi _videoApi;
        private SnackbarMessageQueue _mainMessageQueue = new(TimeSpan.FromSeconds(2));
        private static SemaphoreSlim _semaphoreSlim = new(1);
        private double _uniformCornerRadius = 10;
        private string _maxBtnIcon = "\xe600";
        private string _maxBtnToolTip = "最大化";
        private int _yesterdayBarcodeCount;
        private int _todayBarcodeCount;
        private DateTime? _nodeStartTime;
        private DateTime? _nodeEndTime;
        private ObservableCollection<string> _nodeList = new();
        private string? _selectedNode;
        private string? _barcode;
        private string? _cameraName;
        private int _pageCount;
        private int _pageIndex = 1;
        private int _pageSize = 100;

        private ObservableCollection<BarCodeItemModel> _barCodeItems = new();

        public MainWindowViewModel(IDialogService dialogService,
            IVideoApi videoApi) {
            _dialogService = dialogService;
            _videoApi = videoApi;
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
        public ObservableCollection<string> NodeList {
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
            //查询节点
            Task.Factory.StartNew(async () => {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    NodeList.Clear();
                    var (key, value) = await _videoApi.GroupedNodeNames();
                    if (key && value is ApiResult { Data: List<string> nodeNames }) {
                        NodeList.AddRange(nodeNames);
                    }

                    var (b, o) = await _videoApi.BarcodeTotalForDate(DateTime.Today);
                    if (b && o is ApiResult { Data: long total }) {
                        TodayBarcodeCount = (int)total;
                    }

                    var (key1, value1) = await _videoApi.BarcodeTotalForDate(DateTime.Today.AddDays(-1));
                    if (key1 && value1 is ApiResult { Data: long count }) {
                        YesterdayBarcodeCount = (int)count;
                    }
                });
            });
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
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                NodeStartTime = null;
                NodeEndTime = null;
                SelectedNode = null;
                Barcode = null;
                CameraName = null;
                NodeList.Clear();
                var (key, value) = await _videoApi.GroupedNodeNames();
                if (key && value is ApiResult { Data: List<string> nodeNames }) {
                    NodeList.AddRange(nodeNames);
                }
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
                try {
                    Process.Start(new ProcessStartInfo {
                        FileName = "cmd.exe",
                        Arguments = $"/c start {obj.ScanImageUrl}",
                        CreateNoWindow = true
                    });
                }
                catch (Exception e) {
                    MainMessageQueue.Enqueue(e.Message);
                }
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
                try {
                    Process.Start(new ProcessStartInfo {
                        FileName = "cmd.exe",
                        Arguments = $"/c start {obj.ImageUrl}",
                        CreateNoWindow = true
                    });
                }
                catch (Exception e) {
                    MainMessageQueue.Enqueue(e.Message);
                }
            }
        }

        /// <summary>
        /// 点击视频方法
        /// </summary>
        public ICommand VideoCommand {
            get => new DelegateCommand<BarCodeItemModel>(VideoDelegate);
        }

        private void VideoDelegate(BarCodeItemModel obj) {
            //调用视频Demo并传参
            Console.WriteLine(obj);
            try {
                var process = new Process();
                process.StartInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}x64Demo\\PlayBackAndDownloadDemo.exe";
                process.StartInfo.Arguments = JsonConvert.SerializeObject(new {
                    Channel = obj.NvrCameraBindingItemInfo?.Channel,
                    IpAddress = obj.NvrCameraBindingItemInfo?.IpAddress?.ToString(),
                    Port = obj.NvrCameraBindingItemInfo?.Port,
                    Password = obj.NvrCameraBindingItemInfo?.Password?.ToString(),
                    Username = obj.NvrCameraBindingItemInfo?.Username?.ToString(),
                    StartTime = obj.ScanTime
                }).Replace("\"", "\\\"");
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
            }
            catch (Exception e) {
                MainMessageQueue.Enqueue($"{e.Message}");
            }
        }

        #region 翻页执行方法

        /// <summary>
        /// 上一页
        /// </summary>
        public ICommand PreviousPageCommand {
            get => new DelegateCommand<object>(PreviousPageDelegate);
        }

        private void PreviousPageDelegate(object obj) {
            if (PageIndex <= 1) return;
            PageIndex--;
            LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
        }

        /// <summary>
        /// 下一页
        /// </summary>
        public ICommand NextPageCommand {
            get => new DelegateCommand<object>(NextPageDelegate);
        }

        private void NextPageDelegate(object obj) {
            if (PageIndex >= PageCount) return;
            PageIndex++;
            LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
        }

        /// <summary>
        /// 首页
        /// </summary>
        public ICommand FirstPageCommand {
            get => new DelegateCommand<object>(FirstPageDelegate);
        }

        private void FirstPageDelegate(object obj) {
            PageIndex = 1;
            LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
        }

        /// <summary>
        /// 尾页
        /// </summary>
        public ICommand LastPageCommand {
            get => new DelegateCommand<object>(LastPageDelegate);
        }

        private void LastPageDelegate(object obj) {
            if (PageCount > 0) {
                PageIndex = PageCount;
                LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
            }
        }

        //跳转
        public ICommand JumpPageCommand {
            get => new DelegateCommand<object>(JumpPageDelegate);
        }

        private void JumpPageDelegate(object obj) {
            if (PageIndex >= 0 && PageIndex <= PageCount) {
                LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
            }
            else {
                PageIndex = 1;
            }
        }

        #endregion 翻页执行方法

        private async void LoadData(int pageIndex, DateTime? startTime, DateTime? endTime,
            string? nodeName, string? barCode, string? cameraName) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                await _semaphoreSlim.WaitAsync();
                var loadingDialog = new LoadingDialog();
                if (loadingDialog.DataContext is LoadingDialogViewModel model) {
                    model.Identifier = "MainDialog";
                    DialogHost.Show(loadingDialog, model.Identifier);
                    BarCodeItems.Clear();

                    var (key, value) = await _videoApi.BarcodeInfos(barCode,
                        startTime, endTime,
                        nodeName, string.Empty, cameraName,
                        pageIndex - 1, _pageSize);
                    try {
                        if (value is ApiResult result) {
                            PageCount = result.Total / _pageSize + (result.Total % _pageSize > 0 ? 1 : 0);
                            if (result.Total > 0) {
                                //转换
                                if (result.Data is List<ApiBarCodesInfo> infos) {
                                    var barCodeItemModels = infos.SelectMany(d => d.ScanNodeInfos, (d, s) => new { d.Barcode, s })
                                        .Select((s, i) => new BarCodeItemModel {
                                            Num = i + 1,
                                            BarCode = s.Barcode,
                                            CameraCustomName = s.s.BarcodeImageInfos
                                                ?.FirstOrDefault(f => f.ImageType == 0)?.CameraName,
                                            CameraSerialNumber = s.s.BarcodeImageInfos
                                                ?.FirstOrDefault(f => f.ImageType == 0)?.CameraSerialNumber,
                                            NodeName = s.s.Name,
                                            ScanImageUrl = s.s.BarcodeImageInfos?.FirstOrDefault(f => f.ImageType == 0)
                                                ?.Path,
                                            ScanTime = s.s.ScanTime,
                                            ScanImageVisible = !string.IsNullOrEmpty(s.s.BarcodeImageInfos
                                                ?.FirstOrDefault(f => f.ImageType == 0)?.Path),
                                            PanoramaImageItems = new ObservableCollection<PanoramaImageItemModel>(s.s
                                                .BarcodeImageInfos?.Where(w => w.ImageType == 1)
                                                ?.Select(s1 => new PanoramaImageItemModel {
                                                    ImageVisible = !string.IsNullOrEmpty(s1.Path),
                                                    ImageUrl = s1.Path
                                                })?.ToList() ?? new List<PanoramaImageItemModel>()),
                                            NvrCameraBindingItemInfo = new NvrCameraBindingItemInfo() {
                                                BarcodeScannerSerialNumber = s.s.NvrCameraBindingInfo.BarcodeScannerSerialNumber,
                                                Channel = s.s.NvrCameraBindingInfo.Channel,
                                                IpAddress = s.s.NvrCameraBindingInfo.IpAddress,
                                                Password = s.s.NvrCameraBindingInfo.Password,
                                                Port = s.s.NvrCameraBindingInfo.Port,
                                                Username = s.s.NvrCameraBindingInfo.Username
                                            },
                                            IsVideoLinkVisible =
                                                new Func<bool>(() => !string.IsNullOrEmpty(s.s.NvrCameraBindingInfo.BarcodeScannerSerialNumber) &&
                                                                     !string.IsNullOrEmpty(s.s.NvrCameraBindingInfo.Password) &&
                                                                     !string.IsNullOrEmpty(s.s.NvrCameraBindingInfo.Username) &&
                                                                     !string.IsNullOrEmpty(s.s.NvrCameraBindingInfo.IpAddress) &&
                                                                     s.s.NvrCameraBindingInfo is { Port: > 0, Channel: > 0 })(),
                                        })?.OrderByDescending(o => o.ScanTime)?.ToList();
                                    BarCodeItems.AddRange(barCodeItemModels);
                                }
                            }
                            else {
                                MainMessageQueue.Enqueue("未查询到相关数据");
                            }
                        }
                        else {
                            MainMessageQueue.Enqueue(value?.ToString() ?? string.Empty);
                        }
                    }
                    catch (Exception e) {
                        MainMessageQueue.Enqueue(e.Message);
                    }
                    if (DialogHost.IsDialogOpen(model.Identifier)) {
                        DialogHost.Close(model.Identifier);
                    }
                }
                _semaphoreSlim.Release();
            });
        }
    }
}