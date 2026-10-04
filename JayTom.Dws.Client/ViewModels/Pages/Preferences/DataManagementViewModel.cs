using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.PackageExits;
using JayTom.Dws.Application.PackageHistory;
using JayTom.Dws.Application.UseCases;
using System;
using System.Runtime.CompilerServices;
using JayTom.Dws.Abstractions.Threading;
using JayTom.Dws.Client.Extensions;
using DryIoc;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Plugin;
using System.Diagnostics;
using System.Windows.Input;
using JayTom.Dws.Legacy.Contracts.Dto;
using Prism.Services.Dialogs;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Models.Package;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;
using LibreHardwareMonitor.Hardware;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Client.Models.DataModels;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Excel;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Views.Dialog.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences
{

    public class DataManagementViewModel : BindableBase
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private readonly IDialogService _dialogService;
        private readonly IExcel _excel;
        private readonly ISettingsStore _settingsStore;
        private readonly IPackageExitCatalog _packageExitCatalog;
        private readonly IApplicationQueryHandler<SearchPackageHistoryQuery, PackageHistoryPage> _packageHistoryQuery;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private int _pageCount;
        private int _pageIndex;
        private SnackbarMessageQueue _dataManagementMessageQueue = new(TimeSpan.FromSeconds(2));
        private string _barCode = string.Empty;
        private decimal _minWeight;
        private decimal _maxWeight;
        private bool _isLoaded;
        /// <summary>查询页面每次只构建一百条记录，限制渲染和内存峰值。</summary>
        private const int HistoryPageSize = 100;
        /// <summary>只保留用户最近一次查询的结果。</summary>
        private CancellationTokenSource? _queryCancellation;
        /// <summary>只保留最近打开的单包裹详情读取。</summary>
        private CancellationTokenSource? _detailsCancellation;
        /// <summary>在页面卸载时取消尚未结束的导出。</summary>
        private CancellationTokenSource? _exportCancellation;
        /// <summary>标记导出正在执行，避免重复点击堆积工作簿。</summary>
        private bool _isExporting;
        /// <summary>判断异步读取返回时页面是否仍然有效。</summary>
        private bool _isPageActive;
        /// <summary>保存本页拥有的查询进度会话。</summary>
        private LogPageDialog? _queryDialog;
        /// <summary>为导出单独保留一个低优先级通道，避免导出等待读取时阻塞查询通道。</summary>
        private static readonly Lazy<LowPriorityWorkScheduler> ExportScheduler = new(() => new("DwsHistoryExport", 1));

        private ObservableCollection<PackageItemModel> _packageItems = new()
        {
        };

        private UploadStatus? _selectedUploadStatus;
        private ObservableCollection<UploadStatus> _uploadStatusList = new(Enum.GetValues(typeof(UploadStatus)).Cast<UploadStatus>());
        private decimal _pageMaxHeight;
        private VolumeUnit _volumeUnit;
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();
        private PackageExitDefinitionItemInfoModel? _selectExitDefinitionInfo;

        public DataManagementViewModel(IDialogService dialogService,
            IExcel excel,
            ISettingsStore settingsStore,
            IPackageExitCatalog packageExitCatalog,
            IApplicationQueryHandler<SearchPackageHistoryQuery, PackageHistoryPage> packageHistoryQuery,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _dialogService = dialogService;
            _excel = excel;
            _settingsStore = settingsStore;
            _packageExitCatalog = packageExitCatalog;
            _packageHistoryQuery = packageHistoryQuery;
            _eventBus.SubscribeAsync<SettingsChangedEvent>(async info =>
            {
                if (info is SettingsChangedEvent model)
                {
                    if (model.SettingsName.Equals("VolumeSettings"))
                    {
                        await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
                        {
                            //临时写在这里加载配置，后续修改通过事件通知
                            var volumeSettingsDto = await _settingsStore
                                .GetAsync<VolumeSettingsDto>("VolumeSettings");
                            if (volumeSettingsDto is not null)
                            {
                                VolumeUnit = volumeSettingsDto.Unit;
                            }
                        });
                    }
                }
            });
        }

        /// <summary>
        /// 提示内容
        /// </summary>
        public SnackbarMessageQueue DataManagementMessageQueue
        {
            get => _dataManagementMessageQueue;
            set => SetProperty(ref _dataManagementMessageQueue, value);
        }

        /// <summary>
        /// 体积单位
        /// </summary>
        public VolumeUnit VolumeUnit
        {
            get => _volumeUnit;
            set => SetProperty(ref _volumeUnit, value);
        }

        #region 搜索工具栏条件

        public DateTime? StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public ObservableCollection<PackageExitDefinitionItemInfoModel> PackageExitDefinitionItems
        {
            get => _packageExitDefinitionItems;
            set => SetProperty(ref _packageExitDefinitionItems, value);
        }

        public PackageExitDefinitionItemInfoModel? SelectExitDefinitionInfo
        {
            get => _selectExitDefinitionInfo;
            set => SetProperty(ref _selectExitDefinitionInfo, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode
        {
            get => _barCode;
            set => SetProperty(ref _barCode, value);
        }

        /// <summary>
        /// 最小重量
        /// </summary>
        public decimal MinWeight
        {
            get => _minWeight;
            set => SetProperty(ref _minWeight, value);
        }

        /// <summary>
        /// 最大重量
        /// </summary>
        public decimal MaxWeight
        {
            get => _maxWeight;
            set => SetProperty(ref _maxWeight, value);
        }

        /// <summary>
        /// 验证浮点数
        /// </summary>
        public ICommand ValidateInputCommand
        {
            get => new DelegateCommand<object>(ValidateInputDelegate);
        }

        private void ValidateInputDelegate(object args)
        {
            var type = args.GetType();
            Console.WriteLine(type);
            /*if (!Regex.IsMatch(args.Text, @"^[0-9]*(?:\.[0-9]*)?$")) {
                args.Handled = true; // 阻止字符输入
            }*/
        }

        public ObservableCollection<UploadStatus> UploadStatusList
        {
            get => _uploadStatusList;
            set => SetProperty(ref _uploadStatusList, value);
        }

        /// <summary>
        /// 上传状态
        /// </summary>
        public UploadStatus? SelectedUploadStatus
        {
            get => _selectedUploadStatus;
            set => SetProperty(ref _selectedUploadStatus, value);
        }

        #endregion 搜索工具栏条件

        #region 翻页变量

        /// <summary>
        /// 页数
        /// </summary>
        public int PageCount
        {
            get => _pageCount;
            set => SetProperty(ref _pageCount, value);
        }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageIndex
        {
            get => _pageIndex;
            set => SetProperty(ref _pageIndex, value);
        }

        #endregion 翻页变量

        #region 翻页执行方法

        /// <summary>
        /// 上一页
        /// </summary>
        public ICommand PreviousPageCommand
        {
            get => new DelegateCommand<object>(PreviousPageDelegate);
        }

        private void PreviousPageDelegate(object obj)
        {
            if (PageIndex <= 1) return;
            PageIndex--;
            LoadData(PageIndex);
        }

        /// <summary>
        /// 下一页
        /// </summary>
        public ICommand NextPageCommand
        {
            get => new DelegateCommand<object>(NextPageDelegate);
        }

        private void NextPageDelegate(object obj)
        {
            if (PageIndex >= PageCount) return;
            PageIndex++;
            LoadData(PageIndex);
        }

        /// <summary>
        /// 首页
        /// </summary>
        public ICommand FirstPageCommand
        {
            get => new DelegateCommand<object>(FirstPageDelegate);
        }

        private void FirstPageDelegate(object obj)
        {
            PageIndex = 1;
            LoadData(PageIndex);
        }

        /// <summary>
        /// 尾页
        /// </summary>
        public ICommand LastPageCommand
        {
            get => new DelegateCommand<object>(LastPageDelegate);
        }

        private void LastPageDelegate(object obj)
        {
            if (PageCount > 0)
            {
                PageIndex = PageCount;
                LoadData(PageIndex);
            }
        }

        //跳转
        public ICommand JumpPageCommand
        {
            get => new DelegateCommand<object>(JumpPageDelegate);
        }

        private void JumpPageDelegate(object obj)
        {
            if (PageIndex >= 1 && PageIndex <= PageCount)
            {
                LoadData(PageIndex);
            }
            else
            {
                PageIndex = 1;
            }
        }

        #endregion 翻页执行方法

        /// <summary>
        /// 最大高度
        /// </summary>
        public decimal PageMaxHeight
        {
            get => _pageMaxHeight;
            set => SetProperty(ref _pageMaxHeight, value);
        }

        public ObservableCollection<PackageItemModel> PackageItems
        {
            get => _packageItems;
            set => SetProperty(ref _packageItems, value);
        }

        public ICommand LoadedCommand => new DelegateCommand<Page>(LoadedDelegate);

        /// <summary>页面入口只启动已观察异常的异步操作。</summary>
        private void LoadedDelegate(Page obj)
        {
            _isPageActive = true;
            LoadedAsync(obj).Forget("加载数据查询页面");
        }

        /// <summary>低优先级读取页面筛选配置，避免打开页面就同步访问数据库。</summary>
        private async Task LoadedAsync(Page obj)
        {
            try
            {
                var exits = await LowPriorityWorkScheduler.Shared.RunAsync(() => _packageExitCatalog.ListAsync());
                if (!_isPageActive) return;
                PackageExitDefinitionItems = new ObservableCollection<PackageExitDefinitionItemInfoModel>(exits.Select((s, i) => new PackageExitDefinitionItemInfoModel
                {
                    CreateTime = s.CreateTime, ExitName = s.ExitName, Id = s.Id, IsActive = s.IsActive,
                    ModifyTime = s.ModifyTime, Num = i + 1, Remarks = s.Remarks, Type = s.Type
                }));
                if (!_isLoaded)
                {
                    var parent = PluginInterface.Utils.Utils.GetParentContainer<Grid>(obj, f => f.Visibility == Visibility.Visible);
                    if (parent is not null)
                    {
                        PageMaxHeight = Convert.ToDecimal(parent.ActualHeight);
                        parent.SizeChanged += (_, _) => PageMaxHeight = Convert.ToDecimal(parent.ActualHeight);
                    }
                    _isLoaded = true;
                }
                if (PageIndex < 1) PageIndex = 1;
                LoadData(PageIndex);
            }
            catch (Exception error)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(error, "加载数据查询页面失败");
                DataManagementMessageQueue.Enqueue("数据查询暂不可用，请稍后重试。");
            }
        }

        /// <summary>离开页面立即取消历史读取和导出，不保留无用的后台任务。</summary>
        public ICommand UnloadedCommand => new DelegateCommand<object>(_ =>
        {
            _isPageActive = false;
            CancelBackgroundOperation(_queryCancellation, "取消已离开页面的数据查询");
            CancelBackgroundOperation(_detailsCancellation, "取消已离开页面的详情读取");
            CancelBackgroundOperation(_exportCancellation, "取消已离开页面的数据导出");
            _queryDialog?.Dispose();
            _queryDialog = null;
        });

        /// <summary>立即标记取消并在线程池执行取消回调，避免慢提供商回调同步阻断 UI 输入。</summary>
        private static void CancelBackgroundOperation(CancellationTokenSource? cancellation, string operation)
        {
            if (cancellation is null) return;
            try { cancellation.CancelAsync().Forget(operation); }
            catch (ObjectDisposedException) { } // 已完成的旧操作可能刚释放自己的取消源。
        }

        public ICommand OpenDateTimeDialogCommand => new DelegateCommand<object>(OpenDateTimeDialogDelegate);

        /// <summary>打开日期筛选入口统一观察异步异常，切页和宿主关闭不会导致程序崩溃。</summary>
        private void OpenDateTimeDialogDelegate(object obj) => OpenDateTimeDialogAsync(obj).Forget("打开日期筛选");

        /// <summary>异步处理打开日期筛选操作。</summary>
        private async Task OpenDateTimeDialogAsync(object obj)
        {
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var dataTimeEditor = new DataTimeEditor();
                if (dataTimeEditor.DataContext is DataTimeEditorViewModel model)
                {
                    model.Identifier = "DataManagementDialog";
                    if (obj?.ToString()?.Equals("StartTime") == true)
                    {
                        model.SelectedDataTime = StartTime ?? DateTime.Today;
                        model.SelectedDate = StartTime ?? DateTime.Today;
                        model.SelectedTime = StartTime ?? DateTime.Today;
                    }
                    else
                    {
                        model.SelectedDataTime = EndTime ?? DateTime.Now;
                        model.SelectedDate = EndTime ?? DateTime.Now;
                        model.SelectedTime = EndTime ?? DateTime.Now;
                    }

                    using var dialog = LogPageDialog.Open(dataTimeEditor, model.Identifier);
                    await dialog.Completion;
                    if (model.IsOk)
                    {
                        if (obj?.ToString()?.Equals("StartTime") == true)
                        {
                            StartTime = model.SelectedDataTime.Value;
                        }
                        else if (obj?.ToString()?.Equals("EndTime") == true)
                        {
                            if (DateTime.Now.CompareTo(model.SelectedDataTime.Value) < 0)
                            {
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
        /// 上传状态点击
        /// </summary>
        public ICommand UploadStatusCommand => new DelegateCommand<PackageItemModel>(UploadStatusDelegate);

        private void UploadStatusDelegate(PackageItemModel obj)
        {
            if (obj.RequestStatus != UploadStatus.NotUploaded)
            {
                LoadDetailsAsync(obj, "ApiAccessDialog").Forget("加载单包裹接口详情");
            }
        }

        /// <summary>
        /// 打开图片
        /// </summary>
        public ICommand OpenPackagedImageCommand => new DelegateCommand<PackageItemModel>(OpenPackagedImageDelegate);

        private void OpenPackagedImageDelegate(PackageItemModel obj)
        {
            if (File.Exists(obj?.BarcodeImagePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(obj.BarcodeImagePath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"Failed to open the image: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 定位图片位置
        /// </summary>
        public ICommand OpenPackagedImageFolderCommand => new DelegateCommand<PackageItemModel>(OpenPackagedImageFolderDelegate);

        private void OpenPackagedImageFolderDelegate(PackageItemModel obj)
        {
            if (File.Exists(obj?.BarcodeImagePath))
            {
                try
                {
                    var startInfo = new ProcessStartInfo("explorer.exe")
                    {
                        UseShellExecute = false
                    };
                    startInfo.ArgumentList.Add("/select,");
                    startInfo.ArgumentList.Add(Path.GetFullPath(obj.BarcodeImagePath));
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"Failed to open the image: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 打开全景图片
        /// </summary>

        public ICommand OpenPanoramaImageCommand => new DelegateCommand<PanoramaImageItemModel>(OpenPanoramaImageDelegate);

        private void OpenPanoramaImageDelegate(PanoramaImageItemModel obj)
        {
            if (File.Exists(obj?.PanoramaImagePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(obj.PanoramaImagePath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"Failed to open the image: {ex.Message}");
                }
            }
        }

        /*/// <summary>
        /// 定位全景图片位置
        /// </summary>
        public ICommand OpenPanoramaImageFolderCommand {
            get => new DelegateCommand<PanoramaImageItemModel>(OpenPanoramaImageFolderDelegate);
        }

        private void OpenPanoramaImageFolderDelegate(PanoramaImageItemModel obj) {
            if (File.Exists(obj?.PanoramaImagePath)) {
                try {
                    Process.Start("explorer.exe", $"/select,\"{obj.PanoramaImagePath}\"");
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to open the image: {ex.Message}");
                }
            }
        }*/

        /// <summary>
        /// 清空查询条件
        /// </summary>
        public ICommand ClearSearchCriteriaCommand => new DelegateCommand<object>(ClearSearchCriteriaDelegate);

        /// <summary>清空查询条件入口统一观察异步异常，切页和宿主关闭不会导致程序崩溃。</summary>
        private void ClearSearchCriteriaDelegate(object obj) => ClearSearchCriteriaAsync(obj).Forget("清空查询条件");

        /// <summary>异步处理清空查询条件操作。</summary>
        private async Task ClearSearchCriteriaAsync(object obj)
        {
            await UiThread.Dispatcher.InvokeAsync(() =>
            {
                StartTime = null;
                EndTime = null;
                SelectExitDefinitionInfo = null;
                MinWeight = MaxWeight = 0;
                BarCode = string.Empty;
                SelectedUploadStatus = null;
            });
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        public ICommand SearchDataCommand => new DelegateCommand<object>(SearchDataDelegate);

        private void SearchDataDelegate(object obj)
        {
            PageIndex = 1;
            LoadData(PageIndex);
        }

        /// <summary>
        /// 导出数据
        /// </summary>
        public ICommand ExportDataCommand => new DelegateCommand<object>(ExportDataDelegate);

        /// <summary>导出入口观察所有后台异常，防止 async void 异常终止程序。</summary>
        private void ExportDataDelegate(object obj) => ExportDataAsync().Forget("导出历史包裹");

        /// <summary>以单独低优先级通道分页导出，不累积全历史列表及完整工作簿。</summary>
        private async Task ExportDataAsync()
        {
            if (_isExporting) return;
            if (PackageItems.Count == 0)
            {
                DataManagementMessageQueue.Enqueue("列表中没有数据。");
                return;
            }
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Please select the location to save the file.",
                Filter = "Excel (*.xlsx)|*.xlsx", DefaultExt = "xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (saveDialog.ShowDialog() != true) return;
            var filter = CreateHistoryFilter() with { EndTime = EndTime ?? TimeProvider.System.GetLocalNow().DateTime };
            var dispatcher = UiThread.Dispatcher;
            using var cancellation = new CancellationTokenSource();
            _exportCancellation = cancellation;
            _isExporting = true;
            LogPageDialog? dialog = null;
            try
            {
                var view = new ExportDialog();
                var model = view.DataContext as ExportDialogViewModel;
                if (model is not null)
                {
                    model.FilePath = saveDialog.FileName;
                    model.Identifier = "DataManagementDialog";
                    model.Message = "正在分页导出数据...";
                    dialog = LogPageDialog.Open(view, model.Identifier);
                    dialog.Completion.Forget("显示数据导出进度");
                }
                string? exportError = null;
                var previousProgress = -1;
                void Progress(int value)
                {
                    if (value == previousProgress || cancellation.IsCancellationRequested) return;
                    previousProgress = value;
                    dispatcher.InvokeAsync(() =>
                    {
                        if (model is not null) { model.Progress = value; model.ProgressText = value + "%"; }
                    }, DispatcherPriority.Background, cancellation.Token).Task.Forget("更新数据导出进度");
                }
                var result = await ExportScheduler.Value.RunAsync(() => _excel.ExportStreamingAsync(
                    saveDialog.FileName, "PackageItems", "PackageItems", ReadExportItemsAsync(filter, Progress, cancellation.Token),
                    Array.Empty<string>(), Progress, error => exportError = error.Message, cancellation.Token), cancellation.Token);
                DataManagementMessageQueue.Enqueue(result ? "数据导出完成。" : "数据导出失败：" + (exportError ?? "没有可导出的数据。"));
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception error)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(error, "导出历史包裹失败");
                DataManagementMessageQueue.Enqueue("数据导出失败，请稍后重试。");
            }
            finally
            {
                dialog?.Dispose();
                _exportCancellation = null;
                _isExporting = false;
            }
        }

        /// <summary>每次只保留一页导出对象，筛选条件在导出开始时固定。</summary>
        private async IAsyncEnumerable<ExcelPackageItemModel> ReadExportItemsAsync(PackageHistoryQuery filter,
            Action<int> progress, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var pageCount = 1;
            for (var page = 0; page < pageCount; page++)
            {
                var result = await _packageHistoryQuery.HandleAsync(new SearchPackageHistoryQuery(
                    filter with { IncludeTotal = page == 0 }, page, HistoryPageSize), cancellationToken);
                if (page == 0) pageCount = (int)Math.Ceiling((decimal)result.Total / HistoryPageSize);
                var rowNumber = page * HistoryPageSize;
                foreach (var s in result.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return new ExcelPackageItemModel
                    {
                        Num = ++rowNumber,
                        TimestampMilliseconds = s.PackageTimestamped, Barcode = s.BarCodeInfo?.Barcode ?? string.Empty,
                        Weight = s.WeightInfo?.FormattedWeight ?? 0, Length = s.VolumeInfo?.FormattedLength ?? 0,
                        Width = s.VolumeInfo?.FormattedWidth ?? 0, Height = s.VolumeInfo?.FormattedHeight ?? 0,
                        Volume = s.VolumeInfo?.FormattedVolume ?? 0, ScanTime = s.BarCodeInfo?.ScanTime ?? s.PackageCreateTime,
                        RequestStatus = s.UploadInfo?.RequestStatus ?? UploadStatus.NotUploaded,
                        BarcodeImagePath = s.ImageInfos.LastOrDefault(l => l.Type == 0)?.LocalPath ?? string.Empty,
                        TheoreticalExit = s.ExitInfo?.TheoreticalExit ?? string.Empty, PhysicalExit = s.ExitInfo?.PhysicalExit ?? string.Empty,
                        SortingCode = s.SortingInfo?.SortingCode ?? string.Empty,
                        SignalCallbackInstructionGeneratedTime = s.SortingInfo?.InstructionInfos.LastOrDefault(l => l.InstructionType
                            is InstructionType.SignalCallback or InstructionType.PackageExceptionEx)?.InstructionGeneratedTime
                    };
                }
                progress(Math.Min(99, (page + 1) * 100 / Math.Max(1, pageCount)));
            }
        }

        /// <summary>在 UI 线程取得不可变的筛选条件，后台任务不会读取正在编辑的控件属性。</summary>
        private PackageHistoryQuery CreateHistoryFilter() => new(StartTime, EndTime, BarCode,
            SelectExitDefinitionInfo?.ExitName, MinWeight, MaxWeight, SelectedUploadStatus);

        /// <summary>启动可取消的单页读取并观察其异常。</summary>
        private void LoadData(int pageIndex) => LoadDataAsync(pageIndex).Forget("加载数据管理列表");

        /// <summary>取消旧请求，后台读取和映射，只在 UI 线程一次性替换最新的有界页面。</summary>
        private async Task LoadDataAsync(int pageIndex)
        {
            var filter = CreateHistoryFilter();
            var dispatcher = UiThread.Dispatcher;
            CancelBackgroundOperation(_queryCancellation, "取消已替代的数据查询");
            _queryDialog?.Dispose();
            using var cancellation = new CancellationTokenSource();
            _queryCancellation = cancellation;
            LogPageDialog? dialog = null;
            try
            {
                if (System.Windows.Application.Current is not null)
                {
                    var loading = new LoadingDialog();
                    dialog = LogPageDialog.Open(loading, "DataManagementDialog");
                    _queryDialog = dialog;
                    dialog.Completion.Forget("显示数据加载进度");
                }
                var result = await _packageHistoryQuery.HandleAsync(new SearchPackageHistoryQuery(
                    filter, Math.Max(0, pageIndex - 1), HistoryPageSize), cancellation.Token);
                var items = await LowPriorityWorkScheduler.Shared.RunAsync(() =>
                    new ObservableCollection<PackageItemModel>(result.Items.Select(DataManagementItemMapper.Map)), cancellation.Token);
                await dispatcher.InvokeAsync(() =>
                {
                    if (!ReferenceEquals(_queryCancellation, cancellation)) return;
                    PageCount = (int)Math.Ceiling((decimal)result.Total / HistoryPageSize);
                    PackageItems = items;
                    DataManagementMessageQueue.Enqueue(result.Total == 0 ? "没有符合条件的数据。" : $"共查询到:{result.Total}条数据,显示{items.Count}条");
                }, DispatcherPriority.Background, cancellation.Token);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception error)
            {
                if (ReferenceEquals(_queryCancellation, cancellation))
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(error, "加载数据管理列表失败");
                    DataManagementMessageQueue.Enqueue("数据查询失败，请稍后重试。" + error.Message);
                }
            }
            finally
            {
                dialog?.Dispose();
                if (ReferenceEquals(_queryDialog, dialog)) _queryDialog = null;
                if (ReferenceEquals(_queryCancellation, cancellation)) _queryCancellation = null;
            }
        }

        public ICommand ShowDetailsCommand => new DelegateCommand<object>(ShowDetailsDelegate);

        private void ShowDetailsDelegate(object obj)
        {
            if (obj is PackageItemModel item) LoadDetailsAsync(item, "PackageDetailsDialog").Forget("加载单包裹详情");
        }

        /// <summary>打开详情时才读取单个包裹的完整接口报文，列表查询不占用这些大字段的内存。</summary>
        private async Task LoadDetailsAsync(PackageItemModel item, string dialogName)
        {
            CancelBackgroundOperation(_detailsCancellation, "取消已替代的详情读取");
            using var cancellation = new CancellationTokenSource();
            _detailsCancellation = cancellation;
            try
            {
                var page = await _packageHistoryQuery.HandleAsync(new SearchPackageHistoryQuery(
                    new PackageHistoryQuery(PackageTimestamp: item.TimestampMilliseconds), 0, 1), cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (page.Items.Count == 0)
                {
                    DataManagementMessageQueue.Enqueue("该包裹记录已不存在。");
                    return;
                }
                var detail = await LowPriorityWorkScheduler.Shared.RunAsync(
                    () => DataManagementItemMapper.Map(page.Items[0], item.Num - 1), cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                _dialogService.Show(dialogName, new DialogParameters { { "PackageItem", detail } }, null);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception error)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(error, "加载单包裹详情失败");
                DataManagementMessageQueue.Enqueue("包裹详情读取失败，请稍后重试。");
            }
            finally
            {
                if (ReferenceEquals(_detailsCancellation, cancellation)) _detailsCancellation = null;
            }
        }

        /// <summary>
        /// 获取或设置查看NVR录像命令。
        /// </summary>
        public ICommand ViewNvrRecordingCommand => new DelegateCommand<PackageItemModel>(ViewNvrRecordingDelegate);

        /// <summary>查看包裹录像入口统一观察异步异常，切页和宿主关闭不会导致程序崩溃。</summary>
        private void ViewNvrRecordingDelegate(PackageItemModel obj) => ViewNvrRecordingAsync(obj).Forget("查看包裹录像");

        /// <summary>异步处理查看包裹录像操作。</summary>
        private async Task ViewNvrRecordingAsync(PackageItemModel obj)
        {
            var nvrRecordingDialog = new NvrRecordingDialog();
            if (nvrRecordingDialog.DataContext is NvrRecordingViewModel model)
            {
                model.Identifier = "SettingDialog";
                model.PackageItemModel = obj;
                await DialogHost.Show(nvrRecordingDialog, model.Identifier);
            }
        }

    }
}
