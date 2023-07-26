using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Threading;
using JayTom.Dws.Plugin;
using System.Diagnostics;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class DataManagementViewModel : BindableBase {
        private readonly IDialogService _dialogService;
        private readonly IExcel _excel;
        private readonly IBarCodeRepository _barCodeRepository;
        private DateTime _startTime = DateTime.Today;
        private DateTime _endTime = DateTime.Now;
        private int _pageCount;
        private int _pageIndex;
        private SnackbarMessageQueue _dataManagementMessageQueue = new(TimeSpan.FromSeconds(2));
        private long _timestampedGuid;
        private string _barCode = string.Empty;
        private float _minWeight;
        private float _maxWeight;
        private bool _isLoaded;

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
                IsBarcodeImageExists = @"C:\Users\77051\Desktop\15.jpg".IsFileExists(),
                IsPanoramaImageExists = @"C:\Users\77051\Desktop\16.jpg".IsFileExists()
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
                IsBarcodeImageExists = @"C:\Users\77051\Desktop\15.jpg".IsFileExists(),
                IsPanoramaImageExists = @"C:\Users\77051\Desktop\166.jpg".IsFileExists()
            },
        };

        private UploadStatus? _selectedUploadStatus;
        private ObservableCollection<UploadStatus> _uploadStatusList = new(Enum.GetValues(typeof(UploadStatus)).Cast<UploadStatus>());
        private double _pageMaxHeight;

        public DataManagementViewModel(IDialogService dialogService, IExcel excel, IBarCodeRepository barCodeRepository) {
            _dialogService = dialogService;
            _excel = excel;
            _barCodeRepository = barCodeRepository;
        }

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

        public ObservableCollection<BarCodeItemModel> BarCodeItems {
            get => _barCodeItems;
            set => SetProperty(ref _barCodeItems, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<Page>(LoadedDelegate);
        }

        private void LoadedDelegate(Page obj) {
            if (!_isLoaded) {
                var parentContainer = Utils.GetParentContainer<Grid>(obj, f => f.Visibility == Visibility.Visible);
                if (parentContainer is not null) {
                    PageMaxHeight = parentContainer.ActualHeight;
                    parentContainer.SizeChanged += delegate (object sender, SizeChangedEventArgs args) {
                        PageMaxHeight = parentContainer.ActualHeight;
                    };
                }

                _isLoaded = true;
            }
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

        /// <summary>
        /// 上传状态点击
        /// </summary>
        public ICommand UploadStatusCommand {
            get => new DelegateCommand<BarCodeItemModel>(UploadStatusDelegate);
        }

        private void UploadStatusDelegate(BarCodeItemModel obj) {
            //判断状态是否已上传再获进行弹窗
            if (obj.RequestStatus != UploadStatus.NotUploaded) {
                _dialogService.ShowDialog("ApiAccessDialog", new DialogParameters { { "BarCodeItem", obj } }, null);
            }
        }

        /// <summary>
        /// 打开图片
        /// </summary>
        public ICommand OpenPackagedImageCommand {
            get => new DelegateCommand<BarCodeItemModel>(OpenPackagedImageDelegate);
        }

        private void OpenPackagedImageDelegate(BarCodeItemModel obj) {
            if (File.Exists(obj?.BarcodeImagePath)) {
                try {
                    Process.Start(new ProcessStartInfo(obj.BarcodeImagePath) { UseShellExecute = true });
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to open the image: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 定位图片位置
        /// </summary>
        public ICommand OpenPackagedImageFolderCommand {
            get => new DelegateCommand<BarCodeItemModel>(OpenPackagedImageFolderDelegate);
        }

        private void OpenPackagedImageFolderDelegate(BarCodeItemModel obj) {
            if (File.Exists(obj?.BarcodeImagePath)) {
                try {
                    Process.Start("explorer.exe", $"/select,\"{obj.BarcodeImagePath}\"");
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to open the image: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 打开全景图片
        /// </summary>

        public ICommand OpenPanoramicImageCommand {
            get => new DelegateCommand<BarCodeItemModel>(OpenPanoramicImageDelegate);
        }

        private void OpenPanoramicImageDelegate(BarCodeItemModel obj) {
            if (File.Exists(obj?.PanoramaImagePath)) {
                try {
                    Process.Start(new ProcessStartInfo(obj.PanoramaImagePath) { UseShellExecute = true });
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to open the image: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 定位全景图片位置
        /// </summary>
        public ICommand OpenPanoramicImageFolderCommand {
            get => new DelegateCommand<BarCodeItemModel>(OpenPanoramicImageFolderDelegate);
        }

        private void OpenPanoramicImageFolderDelegate(BarCodeItemModel obj) {
            if (File.Exists(obj?.PanoramaImagePath)) {
                try {
                    Process.Start("explorer.exe", $"/select,\"{obj.PanoramaImagePath}\"");
                }
                catch (Exception ex) {
                    Console.WriteLine($"Failed to open the image: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 清空查询条件
        /// </summary>
        public ICommand ClearSearchCriteriaCommand {
            get => new DelegateCommand<object>(ClearSearchCriteriaDelegate);
        }

        private async void ClearSearchCriteriaDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                StartTime = DateTime.Today;
                EndTime = DateTime.Now;
                TimestampedGuid = 0;
                MinWeight = MaxWeight = 0;
                BarCode = string.Empty;
                SelectedUploadStatus = null;
            });
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        public ICommand SearchDataCommand {
            get => new DelegateCommand<object>(SearchDataDelegate);
        }

        private void SearchDataDelegate(object obj) {
            PageIndex = 1;
            LoadData(PageIndex);
        }

        /// <summary>
        /// 导出数据
        /// </summary>
        public ICommand ExportDataCommand {
            get => new DelegateCommand<object>(ExportDataDelegate);
        }

        private async void ExportDataDelegate(object obj) {
            if (BarCodeItems?.Any() != true) {
                DataManagementMessageQueue?.Enqueue("列表中没有数据");
                return;
            }

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
                    //如果页数超过1页则从数据库获取数据
                    var export = await _excel.Export(saveFileDialog.FileName,
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
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var loadingDialog = new LoadingDialog();
                if (loadingDialog.DataContext is LoadingDialogViewModel model) {
                    model.Identifier = "DataManagementDialog";
                    DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
                    await Task.Delay(500);
                    BarCodeItems.Clear();
                    //获取条数
                    var total = await _barCodeRepository.Total(s =>
                            (s.ScanTime.CompareTo(StartTime) >= 0) &&
                            s.ScanTime.CompareTo(EndTime) <= 0 &&
                            (string.IsNullOrWhiteSpace(BarCode) || s.Barcode.Contains(BarCode)) &&
                            (TimestampedGuid <= 0 || s.TimestampedGuid.Equals(TimestampedGuid)) &&
                            (MinWeight <= 0 || s.Weight >= MinWeight) &&
                            (MaxWeight <= 0 || s.Weight <= MaxWeight) &&
                            (SelectedUploadStatus == null || s.RequestStatus.Equals(SelectedUploadStatus)),
                        new CancellationToken(false));
                    if (total > 0) {
                        PageCount = total / pageSize + (total % pageSize > 0 ? 1 : 0);
                        var infoModels = await _barCodeRepository.SelectOrderByDescending(s =>
                                (s.ScanTime.CompareTo(StartTime) >= 0) &&
                                s.ScanTime.CompareTo(EndTime) <= 0 &&
                                (string.IsNullOrWhiteSpace(BarCode) || s.Barcode.Contains(BarCode)) &&
                                (TimestampedGuid <= 0 || s.TimestampedGuid.Equals(TimestampedGuid)) &&
                                (MinWeight <= 0 || s.Weight >= MinWeight) &&
                                (MaxWeight <= 0 || s.Weight <= MaxWeight) &&
                                (SelectedUploadStatus == null || s.RequestStatus.Equals(SelectedUploadStatus)),
                            o => o.ScanTime, pageIndex - 1, pageSize, new CancellationToken(false));
                        if (infoModels?.Any() == true) {
                            var itemModels = infoModels?.Select((s, i) => new BarCodeItemModel {
                                Num = i + 1,
                                TimestampedGuid = s.TimestampedGuid,
                                Barcode = s.Barcode,
                                Weight = s.Weight,
                                Length = s.Length,
                                Width = s.Width,
                                Height = s.Height,
                                Volume = s.Volume,
                                ScanTime = s.ScanTime,
                                RequestStatus = s.RequestStatus,
                                RequestTime = s.RequestTime,
                                RequestContent = s.RequestContent,
                                ResponseTime = s.ResponseTime,
                                ResponseContent = s.ResponseContent,
                                BarcodeImagePath = s.BarcodeImagePath ?? string.Empty,
                                PanoramaImagePath = s.PanoramaImagePath ?? string.Empty,
                                IsBarcodeImageExists = s.BarcodeImagePath?.IsFileExists() ?? false,
                                IsPanoramaImageExists = s.PanoramaImagePath?.IsFileExists() ?? false,
                                InstructionContent = s.InstructionContent ?? string.Empty,
                                InstructionSentTime = s.InstructionSentTime,
                                DestinationAddress = s.DestinationAddress ?? string.Empty,
                                Other = s.Other ?? string.Empty,
                            })?.ToList();
                            await Task.Delay(100);
                            BarCodeItems.AddRange(itemModels);
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
    }
}