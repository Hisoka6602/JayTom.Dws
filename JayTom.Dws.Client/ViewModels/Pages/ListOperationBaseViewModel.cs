using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using JayTom.Dws.Plugin;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Threading;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Application.Workflows;
using JayTom.Dws.Application.Presentation;

namespace JayTom.Dws.Client.ViewModels.Pages
{

    public abstract class ListOperationBaseViewModel<T> : BindableBase
    {
        private readonly IExcel _excel;
        private int _currentPage;
        private int _totalPages;
        private int _pageSize = 500;
        private SnackbarMessageQueue _itemMessageQueue = new(TimeSpan.FromSeconds(2));
        private ObservableCollection<T> _itemsSource = new();
        /// <summary>统一维护耗时命令的忙碌与取消状态。</summary>
        private readonly AsyncOperationController _operationController = new();

        protected ListOperationBaseViewModel(IExcel excel)
        {
            _excel = excel;
            CancelOperationCommand = new DelegateCommand(
                _operationController.Cancel,
                () => IsBusy);
            _operationController.StateChanged += (_, _) =>
            {
                RaisePropertyChanged(nameof(IsBusy));
                CancelOperationCommand.RaiseCanExecuteChanged();
            };
        }

        /// <summary>获取当前是否正在执行耗时命令。</summary>
        public bool IsBusy => _operationController.IsBusy;

        /// <summary>获取取消当前耗时命令的统一命令。</summary>
        public DelegateCommand CancelOperationCommand { get; }

        public abstract string Identifier { get; }
        public abstract string ExcelTitle { get; }
        public abstract string SheetName { get; }

        /// <summary>
        /// 清空搜索条件
        /// </summary>
        public abstract Task ClearConditions();

        /// <summary>
        /// 清空全部内容
        /// </summary>
        /// <returns></returns>
        public abstract Task<bool> ClearData();

        /// <summary>
        /// 执行导出数据操作
        /// </summary>
        protected abstract List<T> ExportProcess();

        /// <summary>
        /// 执行删除数据操作
        /// </summary>
        public abstract Task<bool> Delete(T obj);

        /// <summary>
        /// 执行编辑数据操作
        /// </summary>
        public abstract Task<bool> Edit(T obj);

        /// <summary>
        ///  加载数据
        /// </summary>
        public abstract Task<KeyValuePair<int, ObservableCollection<T>>> LoadData(int currentPage);

        /// <summary>
        /// 数据
        /// </summary>
        public ObservableCollection<T> ItemsSource
        {
            get => _itemsSource;
            set => SetProperty(ref _itemsSource, value);
        }

        /// <summary>
        /// 当前页
        /// </summary>

        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>
        /// 总页数
        /// </summary>

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        public SnackbarMessageQueue ItemMessageQueue
        {
            get => _itemMessageQueue;
            set => SetProperty(ref _itemMessageQueue, value);
        }

        /// <summary>
        /// 清空条件方法
        /// </summary>
        public ICommand ClearConditionsCommand => new DelegateCommand<object>(ClearConditionsCommandDelegate);

        private async void ClearConditionsCommandDelegate(object obj)
        {
            await ClearConditions();
        }

        /// <summary>
        /// 搜索方法
        /// </summary>
        public ICommand SearchCommand => new DelegateCommand<object>(SearchCommandDelegate);

        private void SearchCommandDelegate(object obj)
        {
            CurrentPage = 1;
            LoadDataToView(CurrentPage);
        }

        /// <summary>
        /// 导出方法
        /// </summary>
        public ICommand ExportCommand => new DelegateCommand<object>(ExportDelegate);

        private async void ExportDelegate(object obj)
        {
            var items = ExportProcess();
            if (items?.Any() != true)
            {
                ItemMessageQueue?.Enqueue(Languages.Language.ResourceManager.GetString("列表中没有数据") ?? string.Empty);
                return;
            }
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "Please select the location to save the file.",
                Filter = $"{Languages.Language.ResourceManager.GetString("Excel文件") ?? string.Empty}(xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var exportDialog = new ExportDialog();
                if (exportDialog.DataContext is ExportDialogViewModel model)
                {
                    model.FilePath = saveFileDialog.FileName;
                    model.Identifier = "MainDialog";
                    model.Message = "Retrieving data...";
                    DialogHost.Show(exportDialog, model.Identifier)
                        .Forget("显示列表导出进度对话框");
                    try
                    {
                        var export = await _excel.Export(saveFileDialog.FileName,
                            ExcelTitle,
                            SheetName, items,
                            new List<string>(), async p =>
                            {
                                model.Progress = p;
                                model.ProgressText = $"{p}%";
                                if (p == 100)
                                {
                                    await UiThread.Dispatcher.InvokeAsync(() =>
                                    {
                                        if (DialogHost.IsDialogOpen(model.Identifier))
                                        {
                                            DialogHost.Close(model.Identifier);
                                        }
                                    });
                                }
                            }, e =>
                            {
                                ItemMessageQueue?.Enqueue(e.Message);
                            });
                        if (!export)
                        {
                            await UiThread.Dispatcher.InvokeAsync(() =>
                            {
                                if (DialogHost.IsDialogOpen(model.Identifier))
                                {
                                    DialogHost.Close(model.Identifier);
                                }
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        ItemMessageQueue?.Enqueue(e.Message);
                    }
                }
            }
        }

        /// <summary>
        /// 删除方法
        /// </summary>
        public ICommand DeleteCommand => new DelegateCommand<T>(DeleteCommandDelegate);

        private async void DeleteCommandDelegate(T obj)
        {
            await Delete(obj);
            FirstPageDelegate(null);
        }

        /// <summary>
        /// 编辑方法
        /// </summary>
        public ICommand EditCommand => new DelegateCommand<T>(EditCommandDelegate);

        private async void EditCommandDelegate(T obj)
        {
            await Edit(obj);
            FirstPageDelegate(null);
        }

        /// <summary>
        /// 页面加载方法
        /// </summary>
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        public virtual void LoadedDelegate(object obj)
        {
        }

        /// <summary>
        /// 首页
        /// </summary>
        public ICommand FirstPageCommand => new DelegateCommand<object>(FirstPageDelegate);

        private void FirstPageDelegate(object obj)
        {
            CurrentPage = 1;
            LoadDataToView(CurrentPage);
        }

        /// <summary>
        /// 上一页
        /// </summary>
        public ICommand PreviousPageCommand => new DelegateCommand<object>(PreviousPageDelegate);

        private void PreviousPageDelegate(object obj)
        {
            if (CurrentPage <= 1) return;
            CurrentPage--;
            LoadDataToView(CurrentPage);
        }

        /// <summary>
        /// 下一页
        /// </summary>
        public ICommand NextPageCommand => new DelegateCommand<object>(NextPageDelegate);

        private void NextPageDelegate(object obj)
        {
            if (CurrentPage >= TotalPages) return;
            CurrentPage++;
            LoadDataToView(CurrentPage);
        }

        /// <summary>
        /// 尾页
        /// </summary>
        public ICommand LastPageCommand => new DelegateCommand<object>(LastPageDelegate);

        private void LastPageDelegate(object obj)
        {
            if (TotalPages > 0)
            {
                CurrentPage = TotalPages;
                LoadData(CurrentPage);
            }
        }

        /// <summary>
        /// 跳转页
        /// </summary>
        public ICommand GoToPageCommand => new DelegateCommand<object>(GoToPageDelegate);

        private void GoToPageDelegate(object obj)
        {
            if (CurrentPage >= 0 && CurrentPage <= TotalPages)
            {
                LoadData(CurrentPage);
            }
            else
            {
                CurrentPage = 1;
            }
        }

        /// <summary>
        /// 清空内容
        /// </summary>
        public ICommand ClearDataCommand => new DelegateCommand<object>(ClearDataDelegate);

        private async void ClearDataDelegate(object obj)
        {
            var clearData = await ClearData();
            if (clearData)
            {
                UiThread.Dispatcher.InvokeAsync(() =>
                {
                    ItemsSource.Clear();
                }).Task.Forget("清空列表数据");
            }
        }

        public void LoadDataToView(int currentPage)
        {
            _operationController.TryRunAsync(async cancellationToken =>
            {
                await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var loadingDialog = new LoadingDialog();
                    if (loadingDialog.DataContext is LoadingDialogViewModel model)
                    {
                        model.Identifier = Identifier;
                        DialogHost.Show(loadingDialog, model.Identifier)
                            .Forget("显示列表加载进度对话框");
                        try
                        {
                            await Task.Delay(500, cancellationToken);
                            ItemsSource.Clear();
                            var (key, value) = await LoadData(currentPage);
                            cancellationToken.ThrowIfCancellationRequested();
                            if (key > 0)
                            {
                                PaginationState pagination = PaginationState.Create(key, PageSize, currentPage);
                                CurrentPage = pagination.CurrentPage;
                                TotalPages = pagination.TotalPages;

                                await Task.Delay(100, cancellationToken);
                                ItemsSource = value;
                                ItemMessageQueue?.Enqueue($"共查询到:{key}条数据,显示{ItemsSource?.Count}条");
                            }
                            else
                            {
                                ItemMessageQueue?.Enqueue("No data matching the criteria found.");
                            }
                        }
                        finally
                        {
                            if (DialogHost.IsDialogOpen(model.Identifier))
                            {
                                DialogHost.Close(model.Identifier);
                            }
                        }
                    }
                });
            }).Forget("加载列表数据");
        }
    }
}
