using System;
using System.IO;
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
using System.Configuration;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Security.Policy;
using JayTom.Dws.Data.Package;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Threading.Channels;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using JayTom.Dws.VideoApiClient.Api;
using System.Collections.ObjectModel;
using JayTom.Dws.VideoApiClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using JayTom.Dws.VideoApiClient.Views.Editors;
using Microsoft.Extensions.Configuration.Json;
using JayTom.Dws.VideoApiClient.ViewModels.Dialog;
using JayTom.Dws.VideoApiClient.ViewModels.Editors;
using JayTom.Dws.Infrastructure.SignalR.VideoApi.ClientMessageHub;
using JayTom.Dws.Infrastructure.SignalR.VideoApi.SignalRMessageHub;

namespace JayTom.Dws.VideoApiClient.ViewModels {

    public class MainWindowViewModel : BindableBase {
        private readonly IDialogService _dialogService;
        private readonly IVideoApi _videoApi;
        private readonly IClientMessageHub _clientMessageHub;
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
        private int _videoLengthInSeconds = 60;
        private int _secondsToSubtract = 0;
        private ObservableCollection<BarCodeItemModel> _barCodeItems = new();
        private int _firstDayOfMonthCount;
        private int _lastDayOfMonthCount;

        public MainWindowViewModel(IDialogService dialogService,
            IVideoApi videoApi, IClientMessageHub clientMessageHub) {
            _dialogService = dialogService;
            _videoApi = videoApi;
            _clientMessageHub = clientMessageHub;
            _clientMessageHub.Reconnected += async delegate (string s) {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    MainMessageQueue.Enqueue(s);
                });
            };
            _clientMessageHub.Reconnecting += async delegate (Exception exception) {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    MainMessageQueue.Enqueue("远程服务器正在重连");
                });
            };
            _clientMessageHub.Closed += async delegate (Exception exception) {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    MainMessageQueue.Enqueue("远程服务器已断开");
                });
            };
            _clientMessageHub.ReceiveMessage += async delegate (ReceiveMessageInfo info) {
                /*if (info.MessageType == ReceiveMessageType.MessageItem) {
                    //添加或更新一行
                    if (NodeStartTime is null &&
                        NodeEndTime is null &&
                        SelectedNode is null &&
                        Barcode is null &&
                        CameraName is null) {
                        var messageBarCodeItemInfo = JsonConvert.DeserializeObject<MessageBarCodeItemInfo>(info.MessageData?.ToString()
                            ?? string.Empty);
                        if (messageBarCodeItemInfo is not null) {
                            var nvrCameraBindingItemInfos = messageBarCodeItemInfo.NvrCameraBindingItem?
                                .Select(s => new NvrCameraBindingItemInfo() {
                                    BarcodeScannerSerialNumber = s.BarcodeScannerSerialNumber,
                                    Channel = s.Channel,
                                    IpAddress = s.IpAddress,
                                    Password = s.Password,
                                    Port = s.Port,
                                    Username = s.Username
                                })?.ToList() ?? new List<NvrCameraBindingItemInfo>();
                            var barCodeItemModel = BarCodeItems.FirstOrDefault(f => f.BarCode.Equals(messageBarCodeItemInfo.BarCode) &&
                                f.NodeName.Equals(messageBarCodeItemInfo.NodeName));
                            if (barCodeItemModel is not null) {
                                //更新
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                                    barCodeItemModel.CameraCustomName = messageBarCodeItemInfo.CameraCustomName;
                                    barCodeItemModel.CameraSerialNumber = messageBarCodeItemInfo.CameraSerialNumber;
                                    barCodeItemModel.ScanImageUrl = messageBarCodeItemInfo.ScanImageUrl;
                                    barCodeItemModel.ScanTime = messageBarCodeItemInfo.ScanTime;
                                    barCodeItemModel.ScanImageVisible =
                                        !string.IsNullOrEmpty(messageBarCodeItemInfo.ScanImageUrl);
                                    barCodeItemModel.PanoramaImageItems = new ObservableCollection<PanoramaImageItemModel>(
                                        messageBarCodeItemInfo.PanoramaImageItems?.Select(s => new PanoramaImageItemModel {
                                            ImageUrl = s,
                                            ImageVisible = !string.IsNullOrEmpty(s)
                                        })?.ToList() ?? new List<PanoramaImageItemModel>());
                                    barCodeItemModel.NvrCameraBindingItemInfos = nvrCameraBindingItemInfos?
                                        .Select(nvr => new NvrCameraBindingItemInfo {
                                            BarcodeScannerSerialNumber = nvr.BarcodeScannerSerialNumber,
                                            Channel = nvr.Channel,
                                            IpAddress = nvr.IpAddress,
                                            Password = nvr.Password,
                                            Port = nvr.Port,
                                            Username = nvr.Username,
                                            IsVideoLinkVisible =
                                                new Func<bool>(() => !string.IsNullOrEmpty(nvr.BarcodeScannerSerialNumber) &&
                                                                     !string.IsNullOrEmpty(nvr.Password) &&
                                                                     !string.IsNullOrEmpty(nvr.Username) &&
                                                                     !string.IsNullOrEmpty(nvr.IpAddress) &&
                                                                     nvr is { Port: > 0, Channel: > 0 })(),
                                            BarCode = barCodeItemModel.BarCode ?? string.Empty,
                                            ScanTime = barCodeItemModel.ScanTime,
                                        })?.ToList() ?? new List<NvrCameraBindingItemInfo>();
                                });
                            }
                            else {
                                //添加
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                                    BarCodeItems.Insert(0, new BarCodeItemModel() {
                                        BarCode = messageBarCodeItemInfo.BarCode,
                                        CameraCustomName = messageBarCodeItemInfo.CameraCustomName,
                                        CameraSerialNumber = messageBarCodeItemInfo.CameraSerialNumber,
                                        NodeName = messageBarCodeItemInfo.NodeName,
                                        Num = 0,
                                        ScanImageUrl = messageBarCodeItemInfo.ScanImageUrl,
                                        ScanTime = messageBarCodeItemInfo.ScanTime,
                                        ScanImageVisible = !string.IsNullOrEmpty(messageBarCodeItemInfo.ScanImageUrl),
                                        NvrCameraBindingItemInfos = nvrCameraBindingItemInfos?
                                            .Select(nvr => new NvrCameraBindingItemInfo {
                                                BarcodeScannerSerialNumber = nvr.BarcodeScannerSerialNumber,
                                                Channel = nvr.Channel,
                                                IpAddress = nvr.IpAddress,
                                                Password = nvr.Password,
                                                Port = nvr.Port,
                                                Username = nvr.Username,
                                                IsVideoLinkVisible =
                                                    new Func<bool>(() => !string.IsNullOrEmpty(nvr.BarcodeScannerSerialNumber) &&
                                                                         !string.IsNullOrEmpty(nvr.Password) &&
                                                                         !string.IsNullOrEmpty(nvr.Username) &&
                                                                         !string.IsNullOrEmpty(nvr.IpAddress) &&
                                                                         nvr is { Port: > 0, Channel: > 0 })(),
                                                BarCode = messageBarCodeItemInfo.BarCode ?? string.Empty,
                                                ScanTime = messageBarCodeItemInfo.ScanTime,
                                            })?.ToList() ?? new List<NvrCameraBindingItemInfo>(),
                                        PanoramaImageItems = new ObservableCollection<PanoramaImageItemModel>(
                                            messageBarCodeItemInfo.PanoramaImageItems?.Select(s => new PanoramaImageItemModel {
                                                ImageUrl = s,
                                                ImageVisible = !string.IsNullOrEmpty(s)
                                            })?.ToList() ?? new List<PanoramaImageItemModel>())
                                    });
                                    if (BarCodeItems.Count > _pageSize) {
                                        BarCodeItems.Remove(BarCodeItems.LastOrDefault()!);
                                    }
                                    //排序
                                    foreach (var codeItemModel in BarCodeItems) {
                                        codeItemModel.Num += 1;
                                    }
                                });

                                //
                            }
                        }
                    }
                }*/
                if (info.MessageType == ReceiveMessageType.DataStatistics) {
                    //更新数据汇总
                    try {
                        var statistics = JsonConvert.DeserializeObject<DataStatistics>(info.MessageData?.ToString() ?? string.Empty);
                        if (statistics is not null) {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                                FirstDayOfMonthCount = statistics.ThisMonthBarcodeTotal;
                                LastDayOfMonthCount = statistics.LastMonthBarcodeTotal;
                                TodayBarcodeCount = statistics.TodayBarcodeTotal;
                                YesterdayBarcodeCount = statistics.YesterdayBarcodeTotal;
                            });
                        }
                    }
                    catch (Exception e) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    }
                }
                else if (info.MessageType == ReceiveMessageType.UpDateNodes) {
                    //更新数据节点

                    var deserializeObject = JsonConvert.DeserializeObject<List<string>>(info.MessageData?.ToString() ?? string.Empty);
                    if (deserializeObject is not null) {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                            NodeList.Clear();
                            NodeList.AddRange(deserializeObject);
                        });
                    }
                }
            };
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

        public int FirstDayOfMonthCount {
            get => _firstDayOfMonthCount;
            set => SetProperty(ref _firstDayOfMonthCount, value);
        }

        public int LastDayOfMonthCount {
            get => _lastDayOfMonthCount;
            set => SetProperty(ref _lastDayOfMonthCount, value);
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

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private void LoadedDelegate(object obj) {
            //查询节点
            //SetConfig();
            Task.Factory.StartNew(async () => {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    //读取内容
                    NodeList.Clear();
                    var configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();
                    var webDomains = configuration.GetSection("AppSettings:WebDomain").Value ?? string.Empty;
                    //判断连接
                    if (!string.IsNullOrEmpty(webDomains)) {
                        //连接
                        await _clientMessageHub.StartAsync($"http://{webDomains}/Message");

                        _videoApi.SetWebDomain(webDomains);
                        var (key, value) = await _videoApi.GroupedNodeNames();
                        if (key && value is ApiResult { Data: List<string> nodeNames }) {
                            NodeList.AddRange(nodeNames);
                        }

                        FirstPageDelegate(obj);
                    }
                });
            });
        }

        public ICommand SizeChangedCommand => new DelegateCommand<object>(SizeChangeDelegate);

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

        public ICommand MinWinCommand => new DelegateCommand<object>(MinWinDelegate);

        private void MinWinDelegate(object obj) {
            if (obj is Window window) {
                window.WindowState = WindowState.Minimized;
            }
        }

        public ICommand MaxWinCommand => new DelegateCommand<object>(MaxWinDelegate);

        private void MaxWinDelegate(object obj) {
            if (obj is Window window) {
                if (window.WindowState == WindowState.Maximized) {
                    window.WindowState = WindowState.Normal;
                    return;
                }
                window.WindowState = WindowState.Maximized;
            }
        }

        public ICommand CloseWinCommand => new DelegateCommand<object>(CloseWinDelegate);

        private void CloseWinDelegate(object obj) {
            System.Windows.Application.Current.Shutdown();//关闭
        }

        public ICommand OpenDateTimeDialogCommand => new DelegateCommand<object>(OpenDateTimeDialogDelegate);

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
        public ICommand ClearSearchCriteriaCommand => new DelegateCommand<object>(ClearSearchCriteriaDelegate);

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
        public ICommand SearchCommand => new DelegateCommand<object>(SearchDelegate);

        private void SearchDelegate(object obj) {
            PageIndex = 1;
            LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
        }

        public ICommand SettingCommand => new DelegateCommand<object>(SettingDelegate);

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
        public ICommand PanoramaImageCommand => new DelegateCommand<PanoramaImageItemModel>(PanoramaImageDelegate);

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
        public ICommand VideoCommand => new DelegateCommand<NvrCameraBindingItemInfo>(VideoDelegate);

        private void VideoDelegate(NvrCameraBindingItemInfo obj) {
            //调用视频Demo并传参
            try {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();
                var videoLengthInSeconds = configuration.GetSection("AppSettings:VideoLengthInSeconds").Value ?? string.Empty;
                int.TryParse(videoLengthInSeconds, out _videoLengthInSeconds);
                var secondsToSubtracts = configuration.GetSection("AppSettings:SecondsToSubtract").Value ?? string.Empty;
                int.TryParse(secondsToSubtracts, out _secondsToSubtract);
                var nvrIpAddress = configuration.GetSection("AppSettings:NvrIpAddress").Value ?? string.Empty;
                var process = new Process();
                process.StartInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}x64Demo\\PlayBackAndDownloadDemo.exe";
                process.StartInfo.Arguments = JsonConvert.SerializeObject(new {
                    Channel = obj?.Channel,
                    IpAddress = string.IsNullOrEmpty(nvrIpAddress) ? obj?.IpAddress : nvrIpAddress,
                    Port = obj?.Port,
                    Password = obj?.Password?.ToString(),
                    Username = obj?.Username?.ToString(),
                    StartTime = obj?.ScanTime.AddSeconds(0 - _secondsToSubtract),
                    BarCode = obj?.BarCode,
                    VideoLengthInSeconds = _videoLengthInSeconds,
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
        public ICommand PreviousPageCommand => new DelegateCommand<object>(PreviousPageDelegate);

        private void PreviousPageDelegate(object obj) {
            if (PageIndex <= 1) return;
            PageIndex--;
            LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
        }

        /// <summary>
        /// 下一页
        /// </summary>
        public ICommand NextPageCommand => new DelegateCommand<object>(NextPageDelegate);

        private void NextPageDelegate(object obj) {
            if (PageIndex >= PageCount) return;
            PageIndex++;
            LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
        }

        /// <summary>
        /// 首页
        /// </summary>
        public ICommand FirstPageCommand => new DelegateCommand<object>(FirstPageDelegate);

        private void FirstPageDelegate(object obj) {
            PageIndex = 1;
            LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
        }

        /// <summary>
        /// 尾页
        /// </summary>
        public ICommand LastPageCommand => new DelegateCommand<object>(LastPageDelegate);

        private void LastPageDelegate(object obj) {
            if (PageCount > 0) {
                PageIndex = PageCount;
                LoadData(PageIndex, NodeStartTime, NodeEndTime, SelectedNode, Barcode, CameraName);
            }
        }

        //跳转
        public ICommand JumpPageCommand => new DelegateCommand<object>(JumpPageDelegate);

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

                    var (b, o) = await _videoApi.BarcodeTotalForDate(DateTime.Today);
                    if (b && o is ApiResult { Data: long total }) {
                        TodayBarcodeCount = (int)total;
                    }

                    var (key1, value1) = await _videoApi.BarcodeTotalForDate(DateTime.Today.AddDays(-1));
                    if (key1 && value1 is ApiResult { Data: long count }) {
                        YesterdayBarcodeCount = (int)count;
                    }
                    var (b1, o1) = await _videoApi.BarcodeTotalForDateBetween(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                        new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddSeconds(-1));
                    if (b1 && o1 is ApiResult { Data: long firstDayOfMonthCount }) {
                        FirstDayOfMonthCount = (int)firstDayOfMonthCount;
                    }
                    var (key2, value2) = await _videoApi.BarcodeTotalForDateBetween(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1),
                        new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddSeconds(-1));
                    if (key2 && value2 is ApiResult { Data: long lastDayOfMonthCount }) {
                        LastDayOfMonthCount = (int)lastDayOfMonthCount;
                    }

                    var (key, value) = await _videoApi.BarcodeInfos(barCode,
                        startTime, endTime,
                        nodeName, string.Empty, cameraName,
                        pageIndex - 1, _pageSize);
                    try {
                        if (value is ApiResult result) {
                            PageCount = result.Total / _pageSize + (result.Total % _pageSize > 0 ? 1 : 0);
                            if (result.Total > 0) {
                                //转换
                                if (result.Data is List<PackageInfoModel> infos) {
                                    var barCodeItemModels = infos.Select((s, i) => new BarCodeItemModel {
                                        Num = i + 1,
                                        BarCode = s.BarCodeInfo?.Barcode,
                                        CameraCustomName = s?.BarCodeInfo?.CameraSerialNumber,

                                        CameraSerialNumber = s?.BarCodeInfo?.CameraSerialNumber,
                                        NodeName = s?.DeviceInfo?.NodeName,
                                        ScanImageUrl = s?.ImageInfos?.FirstOrDefault(f => f.Type == 0)?.ImageUrl,
                                        ScanTime = s?.BarCodeInfo?.ScanTime ?? DateTime.MinValue,
                                        ScanImageVisible = !string.IsNullOrEmpty(s?.ImageInfos
                                            ?.FirstOrDefault(f => f.Type == 0)?.ImageUrl),
                                        PanoramaImageItems = new ObservableCollection<PanoramaImageItemModel>(s
                                            ?.ImageInfos?.Where(w => w.Type == 1)
                                            ?.Select(s1 => new PanoramaImageItemModel {
                                                ImageVisible = !string.IsNullOrEmpty(s1.ImageUrl),
                                                ImageUrl = s1.ImageUrl
                                            })?.ToList() ?? new List<PanoramaImageItemModel>()),
                                        NvrCameraBindingItemInfos = s?.NvrInfos?.Select(nvr =>
                                            new NvrCameraBindingItemInfo {
                                                Channel = nvr.Channel,
                                                IpAddress = nvr.IpAddress,
                                                Password = nvr.Password,
                                                Port = nvr.Port,
                                                Username = nvr.Username,
                                                IsVideoLinkVisible =
                                                    new Func<bool>(() =>
                                                        !string.IsNullOrEmpty(nvr.Password) &&
                                                        !string.IsNullOrEmpty(nvr.Username) &&
                                                        !string.IsNullOrEmpty(nvr.IpAddress) &&
                                                        nvr is { Port: > 0, Channel: > 0 })(),
                                                BarCode = s?.BarCodeInfo?.Barcode ?? string.Empty,
                                                ScanTime = s?.BarCodeInfo?.ScanTime ?? DateTime.MinValue,
                                            })?.ToList() ?? new List<NvrCameraBindingItemInfo>()
                                    })?.OrderByDescending(o => o.ScanTime)?.ToList(); ;
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