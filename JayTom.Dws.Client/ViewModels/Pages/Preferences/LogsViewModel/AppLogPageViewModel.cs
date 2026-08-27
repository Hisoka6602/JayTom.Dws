using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Threading;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Application.Logs;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Client.Models.DataModels;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalLog;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Client.Models.LogsItemModels;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.LogsViewModel
{

    public class AppLogPageViewModel : BindableBase
    {
        private readonly ILogQueryService<AppLogInfoModel> _logQueryService;
        private bool _isLoaded;
        private ObservableCollection<AppLogItemModel> _appLogItems = new();
        private string _details = string.Empty;
        private int _pageCount;
        private int _pageIndex;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private LogType? _selectLogType;
        private string? _message;
        private ObservableCollection<LogType> _logTypeItems = new(Enum.GetValues(typeof(LogType)).Cast<LogType>());
        private SnackbarMessageQueue _appLogMessageQueue = new(TimeSpan.FromSeconds(2));

        public AppLogPageViewModel(ILogQueryService<AppLogInfoModel> logQueryService)
        {
            _logQueryService = logQueryService;
        }

        public SnackbarMessageQueue AppLogMessageQueue
        {
            get => _appLogMessageQueue;
            set => SetProperty(ref _appLogMessageQueue, value);
        }

        public ObservableCollection<AppLogItemModel> AppLogItems
        {
            get => _appLogItems;
            set => SetProperty(ref _appLogItems, value);
        }

        public string Details
        {
            get => _details;
            set => SetProperty(ref _details, value);
        }

        public ICommand ClickCommand
        {
            get => new DelegateCommand<AppLogItemModel>(ClickDelegate);
        }

        private async void ClickDelegate(AppLogItemModel obj)
        {
            //显示详细信息
            await UiThread.Dispatcher.InvokeAsync(() =>
            {
                Details = obj.Message;
            });
        }

        /// <summary>
        /// 页面加载完成
        /// </summary>
        public ICommand LoadedCommand
        {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                FirstPageDelegate(obj);
            }
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
        public string? Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
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

        //加载数据方法

        private async void LoadData(int pageIndex)
        {
            const int pageSize = 500;
            var loadingDialog = new LoadingDialog();
            if (loadingDialog.DataContext is not LoadingDialogViewModel model)
            {
                return;
            }
            model.Identifier = "AppLogDialog";
            _ = DialogHost.Show(loadingDialog, model.Identifier);
            try
            {
                var result = await _logQueryService.SearchAsync(
                        new LogQuery(StartTime, EndTime, SelectLogType, Message),
                        pageIndex - 1,
                        pageSize)
                    .ConfigureAwait(false);
                var pageCount = result.Total / pageSize + (result.Total % pageSize > 0 ? 1 : 0);
                var items = result.Items.Select(entity => new AppLogItemModel
                {
                    ClickCommand = ClickCommand,
                    CreateTime = entity.CreateTime,
                    Message = entity.Message,
                    Type = entity.Type
                }).ToList();
                await UiThread.Dispatcher.InvokeAsync(() =>
                {
                    Details = string.Empty;
                    PageCount = pageCount;
                    AppLogItems = new ObservableCollection<AppLogItemModel>(items);
                    if (result.Total == 0)
                    {
                        AppLogMessageQueue?.Enqueue("No data matching the criteria found.");
                    }
                    else if (items.Count == 0)
                    {
                        AppLogMessageQueue?.Enqueue("Error loading data. Please try again.");
                    }
                }, DispatcherPriority.Background);
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, "加载程序日志失败");
                AppLogMessageQueue?.Enqueue("Error loading data. Please try again.");
            }
            finally
            {
                await UiThread.Dispatcher.InvokeAsync(() =>
                {
                    if (DialogHost.IsDialogOpen(model.Identifier))
                    {
                        DialogHost.Close(model.Identifier);
                    }
                }, DispatcherPriority.Background);
            }
        }

        public ICommand ClearSearchCriteriaCommand
        {
            get => new DelegateCommand<object>(ClearSearchCriteriaDelegate);
        }

        private async void ClearSearchCriteriaDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsync(() =>
            {
                StartTime =
                EndTime = null;
                SelectLogType = null;
                Message = null;
            });
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        public ICommand SearchDataCommand
        {
            get => new DelegateCommand<object>(SearchDataDelegate);
        }

        private void SearchDataDelegate(object obj)
        {
            PageIndex = 1;
            LoadData(PageIndex);
        }

        public ICommand ClearMessageCommand
        {
            get => new DelegateCommand<object>(ClearMessageDelegate);
        }

        private async void ClearMessageDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var loadingDialog = new LoadingDialog();
                if (loadingDialog.DataContext is LoadingDialogViewModel model)
                {
                    model.Identifier = "AppLogDialog";
                    DialogHost.Show(loadingDialog, model.Identifier)
                        .Forget("显示应用日志清理进度对话框");
                    await Task.Delay(500);
                    await _logQueryService.ClearAsync();
                    AppLogItems.Clear();
                    Details = string.Empty;
                    PageIndex = PageCount = 0;
                    if (DialogHost.IsDialogOpen(model.Identifier))
                    {
                        DialogHost.Close(model.Identifier);
                    }
                }
            });
        }

        public ICommand OpenDateTimeDialogCommand
        {
            get => new DelegateCommand<object>(OpenDateTimeDialogDelegate);
        }

        private async void OpenDateTimeDialogDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var dataTimeEditor = new DataTimeEditor();
                if (dataTimeEditor.DataContext is DataTimeEditorViewModel model)
                {
                    model.Identifier = "AppLogDialog";
                    if (obj?.ToString()?.Equals("StartTime") == true)
                    {
                        model.SelectedDataTime = StartTime;
                        model.SelectedDate = StartTime;
                        model.SelectedTime = StartTime;
                    }
                    else
                    {
                        model.SelectedDataTime = EndTime;
                        model.SelectedDate = EndTime;
                        model.SelectedTime = EndTime;
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
    }
}
