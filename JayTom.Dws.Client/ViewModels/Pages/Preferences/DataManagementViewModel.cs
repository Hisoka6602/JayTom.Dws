using System;
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
using JayTom.Dws.Domain.Dto;
using Prism.Services.Dialogs;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Client.Models.DataModels;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Excel;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class DataManagementViewModel : BindableBase {
        private readonly IDialogService _dialogService;
        private readonly IExcel _excel;
        private readonly IConfigRepository _configRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly IPackageRepository _packageRepository;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private int _pageCount;
        private int _pageIndex;
        private SnackbarMessageQueue _dataManagementMessageQueue = new(TimeSpan.FromSeconds(2));
        private string _barCode = string.Empty;
        private float _minWeight;
        private float _maxWeight;
        private bool _isLoaded;

        private ObservableCollection<PackageItemModel> _packageItems = new() {
        };

        private UploadStatus? _selectedUploadStatus;
        private ObservableCollection<UploadStatus> _uploadStatusList = new(Enum.GetValues(typeof(UploadStatus)).Cast<UploadStatus>());
        private double _pageMaxHeight;
        private VolumeUnit _volumeUnit;
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();
        private PackageExitDefinitionItemInfoModel? _selectExitDefinitionInfo;

        public DataManagementViewModel(IDialogService dialogService,
            IExcel excel,
            IConfigRepository configRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IPackageRepository packageRepository) {
            _dialogService = dialogService;
            _excel = excel;
            _configRepository = configRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _packageRepository = packageRepository;
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async info => {
                if (info is SettingsChangedEvent model) {
                    if (model.SettingsName.Equals("VolumeSettings")) {
                        await Application.Current.Dispatcher.InvokeAsync(async () => {
                            //临时写在这里加载配置，后续修改通过事件通知
                            var configInfoModel = await _configRepository.FirstOrDefault(s => s.ConfigName.Equals("VolumeSettings"));
                            if (configInfoModel is not null) {
                                var volumeSettingsDto = JsonConvert.DeserializeObject<VolumeSettingsDto>(configInfoModel.Value);
                                if (volumeSettingsDto is not null) {
                                    VolumeUnit = volumeSettingsDto.Unit;
                                }
                            }
                        });
                    }
                }
            });
        }

        /// <summary>
        /// 提示内容
        /// </summary>
        public SnackbarMessageQueue DataManagementMessageQueue {
            get => _dataManagementMessageQueue;
            set => SetProperty(ref _dataManagementMessageQueue, value);
        }

        /// <summary>
        /// 体积单位
        /// </summary>
        public VolumeUnit VolumeUnit {
            get => _volumeUnit;
            set => SetProperty(ref _volumeUnit, value);
        }

        #region 搜索工具栏条件

        public DateTime? StartTime {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime? EndTime {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public ObservableCollection<PackageExitDefinitionItemInfoModel> PackageExitDefinitionItems {
            get => _packageExitDefinitionItems;
            set => SetProperty(ref _packageExitDefinitionItems, value);
        }

        public PackageExitDefinitionItemInfoModel? SelectExitDefinitionInfo {
            get => _selectExitDefinitionInfo;
            set => SetProperty(ref _selectExitDefinitionInfo, value);
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

        public ObservableCollection<UploadStatus> UploadStatusList {
            get => _uploadStatusList;
            set => SetProperty(ref _uploadStatusList, value);
        }

        /// <summary>
        /// 上传状态
        /// </summary>
        public UploadStatus? SelectedUploadStatus {
            get => _selectedUploadStatus;
            set => SetProperty(ref _selectedUploadStatus, value);
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
            if (PageIndex <= 1) return;
            PageIndex--;
            LoadData(PageIndex);
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
            LoadData(PageIndex);
        }

        /// <summary>
        /// 首页
        /// </summary>
        public ICommand FirstPageCommand {
            get => new DelegateCommand<object>(FirstPageDelegate);
        }

        private void FirstPageDelegate(object obj) {
            PageIndex = 1;
            LoadData(PageIndex);
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

        #endregion 翻页执行方法

        /// <summary>
        /// 最大高度
        /// </summary>
        public double PageMaxHeight {
            get => _pageMaxHeight;
            set => SetProperty(ref _pageMaxHeight, value);
        }

        public ObservableCollection<PackageItemModel> PackageItems {
            get => _packageItems;
            set => SetProperty(ref _packageItems, value);
        }

        public ICommand LoadedCommand => new DelegateCommand<Page>(LoadedDelegate);

        private async void LoadedDelegate(Page obj) {
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                o => o.CreateTime);
            PackageExitDefinitionItems.Clear();
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                PackageExitDefinitionItems.Clear();
                var packageExitDefinitionItemInfoModels = packageExitDefinitionInfoModels?.Select((s, i) => new PackageExitDefinitionItemInfoModel {
                    CreateTime = s.CreateTime,
                    ExitName = s.ExitName,
                    Id = s.Id,
                    IsActive = s.IsActive,
                    ModifyTime = s.ModifyTime,
                    Num = i + 1,
                    Remarks = s.Remarks,
                    Type = s.Type
                })?.ToList();

                if (packageExitDefinitionItemInfoModels?.Any() == true) {
                    PackageExitDefinitionItems.AddRange(packageExitDefinitionItemInfoModels);
                }
            });
            if (!_isLoaded) {
                var parentContainer = PluginInterface.Utils.Utils.GetParentContainer<Grid>(obj, f => f.Visibility == Visibility.Visible);
                if (parentContainer is not null) {
                    PageMaxHeight = parentContainer.ActualHeight;
                    parentContainer.SizeChanged += delegate (object sender, SizeChangedEventArgs args) {
                        PageMaxHeight = parentContainer.ActualHeight;
                    };
                }
                _isLoaded = true;
                //加载数据
                FirstPageDelegate(obj);
            }
        }

        public ICommand OpenDateTimeDialogCommand => new DelegateCommand<object>(OpenDateTimeDialogDelegate);

        private async void OpenDateTimeDialogDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var dataTimeEditor = new DataTimeEditor();
                if (dataTimeEditor.DataContext is DataTimeEditorViewModel model) {
                    model.Identifier = "DataManagementDialog";
                    if (obj?.ToString()?.Equals("StartTime") == true) {
                        model.SelectedDataTime = StartTime ?? DateTime.Today;
                        model.SelectedDate = StartTime ?? DateTime.Today;
                        model.SelectedTime = StartTime ?? DateTime.Today;
                    }
                    else {
                        model.SelectedDataTime = EndTime ?? DateTime.Now;
                        model.SelectedDate = EndTime ?? DateTime.Now;
                        model.SelectedTime = EndTime ?? DateTime.Now;
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
        /// 上传状态点击
        /// </summary>
        public ICommand UploadStatusCommand => new DelegateCommand<PackageItemModel>(UploadStatusDelegate);

        private void UploadStatusDelegate(PackageItemModel obj) {
            //判断状态是否已上传再获进行弹窗
            if (obj.RequestStatus != UploadStatus.NotUploaded) {
                _dialogService.Show("ApiAccessDialog", new DialogParameters { { "PackageItem", obj } }, null);
            }
        }

        /// <summary>
        /// 打开图片
        /// </summary>
        public ICommand OpenPackagedImageCommand => new DelegateCommand<PackageItemModel>(OpenPackagedImageDelegate);

        private void OpenPackagedImageDelegate(PackageItemModel obj) {
            if (File.Exists(obj?.BarcodeImagePath)) {
                try {
                    Process.Start(new ProcessStartInfo(obj.BarcodeImagePath) { UseShellExecute = true });
                }
                catch (Exception ex) {
                    Console.WriteLine($@"Failed to open the image: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 定位图片位置
        /// </summary>
        public ICommand OpenPackagedImageFolderCommand => new DelegateCommand<PackageItemModel>(OpenPackagedImageFolderDelegate);

        private void OpenPackagedImageFolderDelegate(PackageItemModel obj) {
            if (File.Exists(obj?.BarcodeImagePath)) {
                try {
                    Process.Start("explorer.exe", $"/select,\"{obj.BarcodeImagePath}\"");
                }
                catch (Exception ex) {
                    Console.WriteLine($@"Failed to open the image: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 打开全景图片
        /// </summary>

        public ICommand OpenPanoramaImageCommand => new DelegateCommand<PanoramaImageItemModel>(OpenPanoramaImageDelegate);

        private void OpenPanoramaImageDelegate(PanoramaImageItemModel obj) {
            if (File.Exists(obj?.PanoramaImagePath)) {
                try {
                    Process.Start(new ProcessStartInfo(obj.PanoramaImagePath) { UseShellExecute = true });
                }
                catch (Exception ex) {
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

        private async void ClearSearchCriteriaDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
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

        private void SearchDataDelegate(object obj) {
            PageIndex = 1;
            LoadData(PageIndex);
        }

        /// <summary>
        /// 导出数据
        /// </summary>
        public ICommand ExportDataCommand => new DelegateCommand<object>(ExportDataDelegate);

        private async void ExportDataDelegate(object obj) {
            if (PackageItems?.Any() != true) {
                DataManagementMessageQueue?.Enqueue(Languages.Language.ResourceManager.GetString("列表中没有数据") ?? string.Empty);
                return;
            }

            //导出
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog() {
                Title = "Please select the location to save the file.",
                Filter = $"{Languages.Language.ResourceManager.GetString("Excel文件") ?? string.Empty}(xlsx)|*.xlsx",
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
                    //如果页数超过1页则从数据库获取数据(未完成)  ExcelPackageItemModel
                    // var packageItemModels = new List<PackageItemModel>();
                    var packageItemModels = new List<ExcelPackageItemModel>();
                    if (PageCount > 0) {
                        for (var i = 0; i < PageCount; i++) {
                            var (key, infoModels) = await _packageRepository.SelectPackageOrderByDescending(s =>
                               s.BarCodeInfo != null && s.WeightInfo != null &&
                               (StartTime == null || s.BarCodeInfo.ScanTime >= StartTime) &&
                               (EndTime == null || s.BarCodeInfo.ScanTime <= EndTime) &&
                               (string.IsNullOrWhiteSpace(BarCode) || EF.Functions.Like(s.BarCodeInfo.Barcode, "%" + BarCode + "%")) &&
                               (SelectExitDefinitionInfo == null || (s.ExitInfo != null && s.ExitInfo.PhysicalExit.Equals(SelectExitDefinitionInfo.ExitName))) &&
                               (MinWeight <= 0 || s.WeightInfo.FormattedWeight >= MinWeight) &&
                               (MaxWeight <= 0 || s.WeightInfo.FormattedWeight <= MaxWeight) &&
                               (SelectedUploadStatus == null || (s.UploadInfo != null && s.UploadInfo.RequestStatus.Equals(SelectedUploadStatus))),
                           o => o.PackageCreateTime, i, 500, new CancellationToken(false));

                            if (infoModels?.Any() == true) {
                                var itemModels = infoModels?.Select((s, num) => new ExcelPackageItemModel {
                                    Num = num + 1 + (i * 500),
                                    TimestampedGuid = s.PackageTimestamped,
                                    Barcode = s.BarCodeInfo?.Barcode ?? string.Empty,
                                    Weight = (float)(s.WeightInfo?.FormattedWeight ?? 0),
                                    Length = (float)(s.VolumeInfo?.FormattedLength ?? 0),
                                    Width = (float)(s.VolumeInfo?.FormattedWidth ?? 0),
                                    Height = (float)(s.VolumeInfo?.FormattedHeight ?? 0),
                                    Volume = (float)(s.VolumeInfo?.FormattedVolume ?? 0),
                                    ScanTime = s.BarCodeInfo?.ScanTime ?? s.PackageCreateTime,
                                    RequestStatus = s.UploadInfo?.RequestStatus ?? UploadStatus.NotUploaded,
                                    BarcodeImagePath = s.ImageInfos?.LastOrDefault(l => l.Type == 0)?.LocalPath ?? string.Empty,
                                    TheoreticalExit = s.ExitInfo?.TheoreticalExit ?? string.Empty,
                                    PhysicalExit = s.ExitInfo?.PhysicalExit ?? string.Empty,
                                    SortingCode = s.SortingInfo?.SortingCode ?? string.Empty,
                                    SignalCallbackInstructionGeneratedTime = s?.SortingInfo?.InstructionInfos?.LastOrDefault(l => l.InstructionType
                                        is InstructionType.SignalCallback
                                        or InstructionType.PackageExceptionEx)?.InstructionGeneratedTime
                                })?.ToList();
                                packageItemModels.AddRange(itemModels ?? new List<ExcelPackageItemModel>());
                            }
                        }
                    }

                    var export = await _excel.Export(saveFileDialog.FileName,
                        $"PackageItems",
                        "PackageItems", packageItemModels,
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
                            DataManagementMessageQueue?.Enqueue(e.Message);
                        });
                    if (!export) {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            if (DialogHost.IsDialogOpen(model.Identifier)) {
                                DialogHost.Close(model.Identifier);
                            }
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <param name="pageIndex"></param>
        private void LoadData(int pageIndex) {
            const int pageSize = 500;
            //这里的查询要分开锁，不然显示有卡顿
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var loadingDialog = new LoadingDialog();
                if (loadingDialog.DataContext is LoadingDialogViewModel model) {
                    model.Identifier = "DataManagementDialog";
                    DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
                    await Task.Delay(500);
                    PackageItems.Clear();
                    //获取格口集合
                    var exitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
                    //获取条数
                    var total = await _packageRepository.Total(s =>
                            s.BarCodeInfo != null && s.WeightInfo != null &&

                            (StartTime == null || s.BarCodeInfo.ScanTime >= StartTime) &&
                            (EndTime == null || s.BarCodeInfo.ScanTime <= EndTime) &&
                            (string.IsNullOrWhiteSpace(BarCode) || EF.Functions.Like(s.BarCodeInfo.Barcode, "%" + BarCode + "%")) &&
                            (SelectExitDefinitionInfo == null || (s.ExitInfo != null && s.ExitInfo.PhysicalExit.Equals(SelectExitDefinitionInfo.ExitName))) &&
                            (MinWeight <= 0 || s.WeightInfo.FormattedWeight >= MinWeight) &&
                            (MaxWeight <= 0 || s.WeightInfo.FormattedWeight <= MaxWeight) &&
                            (SelectedUploadStatus == null || (s.UploadInfo != null && s.UploadInfo.RequestStatus.Equals(SelectedUploadStatus))),
                        new CancellationToken(false));
                    if (total > 0) {
                        PageCount = total / pageSize + (total % pageSize > 0 ? 1 : 0);
                        var (key, infoModels) = await _packageRepository.SelectPackageOrderByDescending(s =>
                                s.BarCodeInfo != null && s.WeightInfo != null &&
                                (StartTime == null || s.BarCodeInfo.ScanTime >= StartTime) &&
                                (EndTime == null || s.BarCodeInfo.ScanTime <= EndTime) &&
                                (string.IsNullOrWhiteSpace(BarCode) || EF.Functions.Like(s.BarCodeInfo.Barcode, "%" + BarCode + "%")) &&
                                (SelectExitDefinitionInfo == null || (s.ExitInfo != null && s.ExitInfo.PhysicalExit.Equals(SelectExitDefinitionInfo.ExitName))) &&
                                (MinWeight <= 0 || s.WeightInfo.FormattedWeight >= MinWeight) &&
                                (MaxWeight <= 0 || s.WeightInfo.FormattedWeight <= MaxWeight) &&
                                (SelectedUploadStatus == null || (s.UploadInfo != null && s.UploadInfo.RequestStatus.Equals(SelectedUploadStatus))),
                            o => o.PackageCreateTime, pageIndex - 1, pageSize, new CancellationToken(false));

                        if (infoModels?.Any() == true) {
                            var itemModels = infoModels?.Select((s, i) => new PackageItemModel {
                                Num = i + 1,
                                TimestampedGuid = s.PackageTimestamped,
                                Barcode = s.BarCodeInfo?.Barcode ?? string.Empty,
                                Weight = (float)(s.WeightInfo?.FormattedWeight ?? 0),
                                Length = (float)(s.VolumeInfo?.FormattedLength ?? 0),
                                Width = (float)(s.VolumeInfo?.FormattedWidth ?? 0),
                                Height = (float)(s.VolumeInfo?.FormattedHeight ?? 0),
                                Volume = (float)(s.VolumeInfo?.FormattedVolume ?? 0),
                                ScanTime = s.BarCodeInfo?.ScanTime ?? s.PackageCreateTime,
                                RequestStatus = s.UploadInfo?.RequestStatus ?? UploadStatus.NotUploaded,
                                BarcodeImagePath = s.ImageInfos?.LastOrDefault(l => l.Type == 0)?.LocalPath ?? string.Empty,
                                IsBarcodeImageExists = s.ImageInfos?.LastOrDefault(l => l.Type == 0)?.LocalPath?.IsFileExists() ?? false,
                                Other = s.Other ?? string.Empty,
                                ExitName = s.ExitInfo?.PhysicalExit ?? string.Empty,
                                PanoramaImageItems = s.ImageInfos?.Where(w => w.Type == 1)?.Select(ps =>
                                new PanoramaImageItemModel() {
                                    IsPanoramaImageExists = ps.LocalPath?.IsFileExists() ?? false,
                                    PanoramaImagePath = ps.LocalPath
                                })?.ToList() ?? new List<PanoramaImageItemModel>(),
                                UploadInfo = new UploadItemModel {
                                    DurationInSeconds = s.UploadInfo?.DurationInSeconds ?? 0,
                                    ExceptionMessage = s.UploadInfo?.ExceptionMessage ?? string.Empty,
                                    InterfaceParameters = s.UploadInfo?.InterfaceParameters ?? string.Empty,
                                    IsSuccess = s.UploadInfo?.RequestStatus == UploadStatus.Succeeded,
                                    RequestContent = s.UploadInfo?.RequestContent ?? string.Empty,
                                    RequestTime = s.UploadInfo?.RequestTime,
                                    RequestUrl = s.UploadInfo?.RequestUrl ?? string.Empty,
                                    ResponseContent = s.UploadInfo?.ResponseContent ?? string.Empty,
                                    ResponseTime = s.UploadInfo?.ResponseTime
                                },
                                WeightInfo = new WeightItemModel() {
                                    CreateTime = s.WeightInfo?.CreateTime,
                                    FormattedWeight = s.WeightInfo?.FormattedWeight ?? 0,
                                    OriginalText = s.WeightInfo?.OriginalText ?? string.Empty,
                                    SourceType = s.WeightInfo?.SourceType ?? SourceType.SerialPort
                                },
                                SortingInfo = new SortingItemModel {
                                    IsSortingUsed = s.SortingInfo?.IsSortingUsed ?? false,
                                    SortingCode = s.SortingInfo?.SortingCode ?? string.Empty,
                                    SortingMode = s.SortingInfo?.SortingMode ?? SortMode.None,
                                    IsCreatedByLowerMachine = s.SortingInfo?.IsCreatedByLowerMachine ?? false,
                                    CommunicationMethod = s.SortingInfo?.CommunicationMethod ?? CommunicationsType.None,
                                    ChecksumProtocolName = s.SortingInfo?.ChecksumProtocolName ?? string.Empty,
                                    ConnectionName = s.SortingInfo?.ConnectionName ?? string.Empty,
                                    IsAbnormalSorting = s.SortingInfo?.IsAbnormalSorting ?? false,
                                    AbnormalSortingType = s.SortingInfo?.AbnormalSortingType ?? AbnormalSortingType.None,
                                    InstructionInfoItems = new ObservableCollection<InstructionInfoItemModel>(s.SortingInfo?.InstructionInfos?
                                        .Select(s1 => new InstructionInfoItemModel() {
                                            InstructionGeneratedTime = s1.InstructionGeneratedTime,
                                            InstructionType = s1.InstructionType,
                                            InstructionContent = s1.InstructionContent
                                        })?.ToList() ?? new List<InstructionInfoItemModel>())
                                },
                                VolumeInfo = new VolumeItemModel() {
                                    CreateTime = s.VolumeInfo?.CreateTime,
                                    FormattedHeight = s.VolumeInfo?.FormattedHeight ?? 0,
                                    FormattedLength = s.VolumeInfo?.FormattedLength ?? 0,
                                    FormattedVolume = s.VolumeInfo?.FormattedVolume ?? 0,
                                    FormattedWidth = s.VolumeInfo?.FormattedWidth ?? 0,
                                    OriginalText = s.VolumeInfo?.OriginalText ?? string.Empty,
                                    SourceType = s.VolumeInfo?.SourceType ?? SourceType.Camera
                                },
                                OcrInfo = new OcrItemInfo() {
                                    CreateTime = s.OcrInfo?.RecognizeTime,
                                    /*OcrInterfaceName = s.OcrInfo?.OcrInterfaceName ?? string.Empty,
                                    OriginalContent = s.OcrInfo?.OriginalContent ?? string.Empty,
                                    ParsedContent = s.OcrInfo?.ParsedContent ?? string.Empty*/
                                },
                                ExitInfo = new ExitInfoItemModel() {
                                    PhysicalExitId = s.ExitInfo?.PhysicalExitId ?? 0,
                                    PhysicalExit = s.ExitInfo?.PhysicalExit ?? string.Empty,
                                    TheoreticalExit = s.ExitInfo?.TheoreticalExit ?? string.Empty,
                                },
                                IsUploadedToCloudVideo = s.CloudVideoUploadInfo is { UploadTime: not null }
                            })?.ToList();
                            await Task.Delay(100);
                            PackageItems.AddRange(itemModels);
                            DataManagementMessageQueue?.Enqueue($"共查询到:{total}条数据,显示{PackageItems?.Count}条");
                        }
                        else {
                            DataManagementMessageQueue?.Enqueue("Error loading data. Please try again.");
                        }
                    }
                    else {
                        DataManagementMessageQueue?.Enqueue("No data matching the criteria found.");
                    }

                    if (DialogHost.IsDialogOpen(model.Identifier)) {
                        DialogHost.Close(model.Identifier);
                    }
                }
            }, DispatcherPriority.Background);
        }

        public ICommand ShowDetailsCommand => new DelegateCommand<object>(ShowDetailsDelegate);

        private void ShowDetailsDelegate(object obj) {
            _dialogService.Show("PackageDetailsDialog", new DialogParameters { { "PackageItem", obj } }, null);
        }
    }
}