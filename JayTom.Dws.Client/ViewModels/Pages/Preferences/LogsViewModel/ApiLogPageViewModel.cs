using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalLog;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Application.Logs;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Client.Models.LogsItemModels;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.LogsViewModel
{

    public class ApiLogPageViewModel : BindableBase
    {
        private readonly ILogQueryService<ApiLogInfoModel> _logQueryService;
        private string _details = string.Empty;
        private bool _isLoaded;
        private int _pageCount;
        private int _pageIndex;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private LogType? _selectLogType;
        private string? _responseContent;
        private ObservableCollection<LogType> _logTypeItems = new(Enum.GetValues(typeof(LogType)).Cast<LogType>());
        private ObservableCollection<ApiLogItemModel> _apiLogItems = new();
        private SnackbarMessageQueue _apiLogMessageQueue = new(TimeSpan.FromSeconds(2));

        public ApiLogPageViewModel(ILogQueryService<ApiLogInfoModel> logQueryService)
        {
            _logQueryService = logQueryService;
        }

        public ObservableCollection<ApiLogItemModel> ApiLogItems
        {
            get => _apiLogItems;
            set => SetProperty(ref _apiLogItems, value);
        }

        public SnackbarMessageQueue ApiLogMessageQueue
        {
            get => _apiLogMessageQueue;
            set => SetProperty(ref _apiLogMessageQueue, value);
        }

        public string Details
        {
            get => _details;
            set => SetProperty(ref _details, value);
        }

        public ICommand ClickCommand => new DelegateCommand<ApiLogItemModel>(ClickDelegate);

        private async void ClickDelegate(ApiLogItemModel obj)
        {
            //显示详细信息
            await UiThread.Dispatcher.InvokeAsync(() =>
            {
                Details = string.Join("\n", new List<string>()
                {
                    $"Url:{obj.Url}",
                    $"耗时:{obj.Duration:F2}",
                    $"上传时间:{obj.RequestTime:yyyy-MM-dd HH:mm:ss.fff}",
                    $"上传内容:{obj.RequestContent}",
                    $"响应时间:{obj.ResponseTime:yyyy-MM-dd HH:mm:ss.fff}",
                    $"响应内容:{obj.ResponseContent}",
                    $"接口参数:{obj.ApiParameters}",
                    $"异常信息:{obj.ExceptionMsg}",
                });
            });
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

        /// <summary>
        /// 类型
        /// </summary>
        public LogType? SelectLogType
        {
            get => _selectLogType;
            set => SetProperty(ref _selectLogType, value);
        }

        /// <summary>
        /// 信息
        /// </summary>
        public string? ResponseContent
        {
            get => _responseContent;
            set => SetProperty(ref _responseContent, value);
        }

        public ObservableCollection<LogType> LogTypeItems
        {
            get => _logTypeItems;
            set => SetProperty(ref _logTypeItems, value);
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
            if (PageIndex >= 0 && PageIndex <= PageCount)
            {
                LoadData(PageIndex);
            }
            else
            {
                PageIndex = 1;
            }
        }

        #endregion 翻页执行方法

        public ICommand ClearSearchCriteriaCommand => new DelegateCommand<object>(ClearSearchCriteriaDelegate);

        private async void ClearSearchCriteriaDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsync(() =>
            {
                StartTime =
                EndTime = null;
                SelectLogType = null;
                ResponseContent = null;
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

        public ICommand OpenDateTimeDialogCommand => new DelegateCommand<object>(OpenDateTimeDialogDelegate);

        private async void OpenDateTimeDialogDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var dataTimeEditor = new DataTimeEditor();
                if (dataTimeEditor.DataContext is DataTimeEditorViewModel model)
                {
                    model.Identifier = "ApiLogDialog";
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

                    await DialogHost.Show(dataTimeEditor, model.Identifier);
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
        /// 页面加载完成
        /// </summary>
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private void LoadedDelegate(object obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                FirstPageDelegate(obj);
            }
        }

        public ICommand ClearMessageCommand => new DelegateCommand<object>(ClearMessageDelegate);

        private async void ClearMessageDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var loadingDialog = new LoadingDialog();
                if (loadingDialog.DataContext is LoadingDialogViewModel model)
                {
                    model.Identifier = "ApiLogDialog";
                    DialogHost.Show(loadingDialog, model.Identifier)
                        .Forget("显示接口日志清理进度对话框");
                    await Task.Delay(500);
                    await _logQueryService.ClearAsync();
                    ApiLogItems.Clear();
                    Details = string.Empty;
                    PageIndex = PageCount = 0;
                    if (DialogHost.IsDialogOpen(model.Identifier))
                    {
                        DialogHost.Close(model.Identifier);
                    }
                }
            });
        }

        private async void LoadData(int pageIndex)
        {
            const int pageSize = 500;
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var loadingDialog = new LoadingDialog();
                if (loadingDialog.DataContext is LoadingDialogViewModel model)
                {
                    model.Identifier = "ApiLogDialog";
                    DialogHost.Show(loadingDialog, model.Identifier)
                        .Forget("显示接口日志加载进度对话框");
                    await Task.Delay(500);
                    ApiLogItems.Clear();
                    Details = string.Empty;
                    var result = await _logQueryService.SearchAsync(
                        new LogQuery(StartTime, EndTime, SelectLogType, Content: ResponseContent),
                        pageIndex - 1,
                        pageSize);
                    if (result.Total > 0)
                    {
                        PageCount = result.Total / pageSize + (result.Total % pageSize > 0 ? 1 : 0);

                        if (result.Items.Count > 0)
                        {
                            var cameraLogItemModels = result.Items.Select(s => new ApiLogItemModel()
                            {
                                ClickCommand = ClickCommand,
                                CreateTime = s.CreateTime,
                                Message = s.Message,
                                Type = s.Type,
                                ApiParameters = s.ApiParameters,
                                Duration = s.Duration * 1000,
                                ExceptionMsg = s.ExceptionMsg,
                                RequestContent = s.RequestContent,
                                RequestTime = s.RequestTime,
                                ResponseContent = s.ResponseContent,
                                ResponseTime = s.ResponseTime,
                                Url = s.Url,
                            })?.ToList();
                            await Task.Delay(100);
                            ApiLogItems.AddRange(cameraLogItemModels);
                        }
                        else
                        {
                            ApiLogMessageQueue?.Enqueue("Error loading data. Please try again.");
                        }
                    }
                    else
                    {
                        ApiLogMessageQueue?.Enqueue("No data matching the criteria found.");
                    }

                    if (DialogHost.IsDialogOpen(model.Identifier))
                    {
                        DialogHost.Close(model.Identifier);
                    }
                }
            });
        }
    }
}
