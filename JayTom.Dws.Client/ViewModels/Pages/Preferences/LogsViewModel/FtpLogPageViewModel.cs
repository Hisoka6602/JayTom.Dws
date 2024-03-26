using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Domain.Repository.LocalLog;
using JayTom.Dws.Client.Models.LogsItemModels;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.LogsViewModel {
    public class FtpLogPageViewModel : BindableBase {
        private readonly IFtpLogRepository _ftpLogRepository;
        private string _details = string.Empty;
        private bool _isLoaded;
        private int _pageCount;
        private int _pageIndex;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private LogType? _selectLogType;
        private string? _message;
        private ObservableCollection<LogType> _logTypeItems = new(Enum.GetValues(typeof(LogType)).Cast<LogType>());
        private SnackbarMessageQueue _ftpLogMessageQueue = new();
        private ObservableCollection<FtpLogItemModel> _ftpLogItems = new();

        public FtpLogPageViewModel(IFtpLogRepository ftpLogRepository) {
            _ftpLogRepository = ftpLogRepository;
        }

        public SnackbarMessageQueue FtpLogMessageQueue {
            get => _ftpLogMessageQueue;
            set => SetProperty(ref _ftpLogMessageQueue, value);
        }

        public ObservableCollection<FtpLogItemModel> FtpLogItems {
            get => _ftpLogItems;
            set => SetProperty(ref _ftpLogItems, value);
        }

        public string Details {
            get => _details;
            set => SetProperty(ref _details, value);
        }

        public ICommand ClickCommand {
            get => new DelegateCommand<FtpLogItemModel>(ClickDelegate);
        }

        private async void ClickDelegate(FtpLogItemModel obj) {
            //显示详细信息
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                Details = string.Join("\n", new List<string>()
                {
                    $"时间:{obj.CreateTime:yyyy-MM-dd HH:mm:ss.fff}",
                    $"类型:{obj.FtpCommunicationType}",
                    $"内容:{obj.Message}",
                });
            });
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

        /// <summary>
        /// 类型
        /// </summary>
        public LogType? SelectLogType {
            get => _selectLogType;
            set => SetProperty(ref _selectLogType, value);
        }

        /// <summary>
        /// 信息
        /// </summary>
        public string? Message {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public ObservableCollection<LogType> LogTypeItems {
            get => _logTypeItems;
            set => SetProperty(ref _logTypeItems, value);
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

        public ICommand ClearSearchCriteriaCommand {
            get => new DelegateCommand<object>(ClearSearchCriteriaDelegate);
        }

        private async void ClearSearchCriteriaDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                StartTime =
                EndTime = null;
                SelectLogType = null;
                Message = null;
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

        public ICommand OpenDateTimeDialogCommand {
            get => new DelegateCommand<object>(OpenDateTimeDialogDelegate);
        }

        private async void OpenDateTimeDialogDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var dataTimeEditor = new DataTimeEditor();
                if (dataTimeEditor.DataContext is DataTimeEditorViewModel model) {
                    model.Identifier = "FtpLogDialog";
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
        /// 页面加载完成
        /// </summary>
        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                FirstPageDelegate(obj);
            }
        }

        public ICommand ClearMessageCommand {
            get => new DelegateCommand<object>(ClearMessageDelegate);
        }

        private async void ClearMessageDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var loadingDialog = new LoadingDialog();
                if (loadingDialog.DataContext is LoadingDialogViewModel model) {
                    model.Identifier = "FtpLogDialog";
                    DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
                    await Task.Delay(500);
                    var total = await _ftpLogRepository.Total(s => s.Id > 0);
                    await _ftpLogRepository.DeleteCount(total);
                    FtpLogItems.Clear();
                    Details = string.Empty;
                    PageIndex = PageCount = 0;
                    if (DialogHost.IsDialogOpen(model.Identifier)) {
                        DialogHost.Close(model.Identifier);
                    }
                }
            });
        }

        private async void LoadData(int pageIndex) {
            const int pageSize = 500;
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var loadingDialog = new LoadingDialog();
                if (loadingDialog.DataContext is LoadingDialogViewModel model) {
                    model.Identifier = "FtpLogDialog";
                    DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
                    await Task.Delay(500);
                    FtpLogItems.Clear();
                    Details = string.Empty;
                    var total = await _ftpLogRepository.Total(s =>
                        (StartTime == null || s.CreateTime.CompareTo(StartTime) >= 0) &&
                        (EndTime == null || s.CreateTime.CompareTo(EndTime) <= 0) &&
                        (SelectLogType == null || s.Type == SelectLogType) &&
                        (string.IsNullOrEmpty(Message) || s.Message.Contains(Message)));
                    if (total > 0) {
                        PageCount = total / pageSize + (total % pageSize > 0 ? 1 : 0);
                        var selectOrderByDescending = await _ftpLogRepository.SelectOrderByDescending(s =>
                                (StartTime == null || s.CreateTime.CompareTo(StartTime) >= 0) &&
                                (EndTime == null || s.CreateTime.CompareTo(EndTime) <= 0) &&
                                (SelectLogType == null || s.Type == SelectLogType) &&
                                (string.IsNullOrEmpty(Message) || s.Message.Contains(Message)), o => o.CreateTime,
                            pageIndex - 1, pageSize);

                        if (selectOrderByDescending?.Any() == true) {
                            var cameraLogItemModels = selectOrderByDescending.Select(s => new FtpLogItemModel() {
                                ClickCommand = ClickCommand,
                                CreateTime = s.CreateTime,
                                Message = s.Message,
                                Type = s.Type,
                                FtpCommunicationType = s.FtpCommunicationType,
                            })?.ToList();
                            await Task.Delay(100);
                            FtpLogItems.AddRange(cameraLogItemModels);
                        }
                        else {
                            FtpLogMessageQueue?.Enqueue("Error loading data. Please try again.");
                        }
                    }
                    else {
                        FtpLogMessageQueue?.Enqueue("No data matching the criteria found.");
                    }

                    if (DialogHost.IsDialogOpen(model.Identifier)) {
                        DialogHost.Close(model.Identifier);
                    }
                }
            });
        }
    }
}