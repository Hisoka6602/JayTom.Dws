using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using JayTom.Dws.Plugin;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using JayTom.Dws.Ocr.ExpressBill;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Excel;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public abstract class BulkOperationsTemplateViewModel<T> : BindableBase where T : class, new() {
        private SnackbarMessageQueue _messageQueue = new(TimeSpan.FromSeconds(2));
        protected readonly IExcel _excel;

        protected BulkOperationsTemplateViewModel(IExcel excel) {
            _excel = excel;
        }

        public SnackbarMessageQueue MessageQueue {
            get => _messageQueue;
            set => SetProperty(ref _messageQueue, value);
        }

        public abstract string Identifier { get; }
        public abstract string ExcelTitle { get; }
        public abstract string SheetName { get; }
        public abstract string SettingsName { get; }
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        public virtual void LoadedDelegate(object obj) {
        }

        /// <summary>
        /// 添加
        /// </summary>
        public ICommand AddCommand => new DelegateCommand<object>(AddDelegate);

        protected virtual void AddDelegate(object obj) {
        }

        public ICommand DeleteCommand => new DelegateCommand<object>(DeleteDelegate);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="obj"></param>
        private async void DeleteDelegate(object obj) {
            var deleteProcess = await DeleteProcess(obj);
            if (deleteProcess) {
                RefreshData();
                EventAggregator.Instance.Publish(new SettingsChangedEvent {
                    SettingsName = SettingsName,
                    IsLocallySaved = true
                });
            }
        }

        protected abstract Task<bool> DeleteProcess(object obj);

        public ICommand ModifyCommand => new DelegateCommand<object>(ModifyDelegate);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="obj"></param>
        protected virtual void ModifyDelegate(object obj) {
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        public ICommand BulkAddCommand => new DelegateCommand<object>(BulkAddDelegate);

        protected virtual void BulkAddDelegate(object obj) {
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        public ICommand BulkDeleteCommand => new DelegateCommand<object>(BulkDeleteDelegate);

        private async void BulkDeleteDelegate(object obj) {
            if (IsSelectAnyItem()) {
                var bulkDeleteAccessDialog = new BulkDeleteAccessDialog();
                if (bulkDeleteAccessDialog.DataContext is BulkDeleteAccessViewModel model) {
                    model.Identifier = Identifier;
                    model.TipContent = "是否删除[选中]内容?";
                    await DialogHost.Show(bulkDeleteAccessDialog, Identifier);
                    if (model.IsOk) {
                        await BulkDeleteProcess();
                        RefreshData();
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = SettingsName,
                            IsLocallySaved = true
                        });
                    }
                }
            }
        }

        protected abstract Task BulkDeleteProcess();

        /// <summary>
        /// 导出
        /// </summary>
        public ICommand ExportCommand => new DelegateCommand<object>(ExportDelegate);

        private async void ExportDelegate(object obj) {
            var items = ExportProcess();
            if (items?.Any() != true) {
                MessageQueue?.Enqueue(Languages.Language.ResourceManager.GetString("列表中没有数据") ?? string.Empty);
                return;
            }
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
                    try {
                        var export = await _excel.Export(saveFileDialog.FileName,
                            ExcelTitle,
                            SheetName, items,
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
                                MessageQueue?.Enqueue(e.Message);
                            });
                        if (!export) {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                                if (DialogHost.IsDialogOpen(model.Identifier)) {
                                    DialogHost.Close(model.Identifier);
                                }
                            });
                        }
                    }
                    catch (Exception e) {
                        MessageQueue?.Enqueue(e.Message);
                    }
                }
            }
        }

        protected abstract List<T> ExportProcess();

        /// <summary>
        /// 导入
        /// </summary>
        public ICommand ImportCommand => new DelegateCommand<object>(ImportDelegate);

        private async void ImportDelegate(object obj) {
            //导入
            var openFileDialog = new Microsoft.Win32.OpenFileDialog() {
                Title = "Please select the file to import.",
                Filter = $"{Languages.Language.ResourceManager.GetString("Excel文件") ?? string.Empty}(xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            };
            if (openFileDialog.ShowDialog() == true) {
                var exportDialog = new ExportDialog();
                if (exportDialog.DataContext is ExportDialogViewModel model) {
                    model.FilePath = openFileDialog.FileName;
                    model.Identifier = "MainDialog";
                    model.Message = "Retrieving data...";
                    DialogHost.Show(exportDialog, model.Identifier);

                    var readExcel = await _excel.ReadExcel<T>(openFileDialog.FileName, async p => {
                        model.Progress = p;
                        model.ProgressText = $"{p}%";
                        if (p == 100) {
                            await Application.Current.Dispatcher.InvokeAsync(() => {
                                if (DialogHost.IsDialogOpen(model.Identifier)) {
                                    DialogHost.Close(model.Identifier);
                                }
                            });
                        }
                    }, async e => {
                        await Application.Current.Dispatcher.InvokeAsync(() => {
                            if (DialogHost.IsDialogOpen(model.Identifier)) {
                                DialogHost.Close(model.Identifier);
                            }
                        });
                        MessageQueue?.Enqueue(e.Message);
                    });
                    await Task.Delay(500);
                    if (readExcel?.Any() == true) {
                        var importProcess = await ImportProcess(readExcel);
                        if (importProcess) {
                            MessageQueue.Enqueue("保存成功");
                            RefreshData();
                            EventAggregator.Instance.Publish(new SettingsChangedEvent {
                                SettingsName = SettingsName,
                                IsLocallySaved = true
                            });
                        }
                        else {
                            MessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            }
        }

        protected abstract Task<bool> ImportProcess(List<T> items);

        /// <summary>
        /// 清空
        /// </summary>
        public ICommand ClearCommand => new DelegateCommand<object>(ClearDelegate);

        private async void ClearDelegate(object obj) {
            var bulkDeleteAccessDialog = new BulkDeleteAccessDialog();
            if (bulkDeleteAccessDialog.DataContext is BulkDeleteAccessViewModel model) {
                model.Identifier = Identifier;
                model.TipContent = "是否清空[全部]内容?";
                await DialogHost.Show(bulkDeleteAccessDialog, Identifier);
                if (model.IsOk) {
                    await ClearProcess();
                    RefreshData();
                    EventAggregator.Instance.Publish(new SettingsChangedEvent {
                        SettingsName = SettingsName,
                        IsLocallySaved = true
                    });
                }
            }
        }

        protected abstract Task ClearProcess();

        /// <summary>
        /// 刷新数据
        /// </summary>
        public async void RefreshData() {
            var loadingDialog = new LoadingDialog();
            if (loadingDialog.DataContext is not LoadingDialogViewModel model) return;
            await Application.Current.Dispatcher.InvokeAsync(() => {
                model.Identifier = Identifier;
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            await RefreshDataProcess();
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                if (DialogHost.IsDialogOpen(model.Identifier)) {
                    DialogHost.Close(model.Identifier);
                }
            });
        }

        protected abstract Task RefreshDataProcess();

        protected abstract bool IsSelectAnyItem();
    }
}