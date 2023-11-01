using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using JayTom.Dws.Plugin;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages {

    public class LogisticsSortingViewModel : BindableBase {
        private readonly ILogisticsSortingRepository _logisticsSortingRepository;
        private readonly ILogisticsRuleRepository _logisticsRuleRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly ILogisticsCodeRecognitionRepository _logisticsCodeRecognitionRepository;
        private readonly IExcel _excel;
        private bool _isLoaded;
        private ObservableCollection<LogisticsSortingItemInfoModel> _logisticsSortingItems = new();
        private SnackbarMessageQueue _logisticsSortingMessageQueue = new(TimeSpan.FromSeconds(2));

        public LogisticsSortingViewModel(ILogisticsSortingRepository logisticsSortingRepository,
            ILogisticsRuleRepository logisticsRuleRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            ILogisticsCodeRecognitionRepository logisticsCodeRecognitionRepository,
            IExcel excel) {
            _logisticsSortingRepository = logisticsSortingRepository;
            _logisticsRuleRepository = logisticsRuleRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _logisticsCodeRecognitionRepository = logisticsCodeRecognitionRepository;
            _excel = excel;
        }

        public ObservableCollection<LogisticsSortingItemInfoModel> LogisticsSortingItems {
            get => _logisticsSortingItems;
            set => SetProperty(ref _logisticsSortingItems, value);
        }

        public SnackbarMessageQueue LogisticsSortingMessageQueue {
            get => _logisticsSortingMessageQueue;
            set => SetProperty(ref _logisticsSortingMessageQueue, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var logisticsSortingRuleEditor = new LogisticsSortingRuleEditor();
                if (logisticsSortingRuleEditor.DataContext is LogisticsSortingRuleEditorViewModel model) {
                    model.Identifier = "LogisticsSortingDialog";
                    await DialogHost.Show(logisticsSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        LogisticsSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        //添加到数据库
                        var logisticsSortingInfoModel = new LogisticsSortingInfoModel() {
                            CreateTime = model.LogisticsSortingItemInfo.CreateTime,
                            ModifyTime = model.LogisticsSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.LogisticsSortingItemInfo.Remarks,
                            SortingName = model.LogisticsSortingItemInfo.SortingName,
                        };
                        var insert = await _logisticsSortingRepository.Insert(logisticsSortingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(logisticsSortingInfoModel);

                            var sortingInfoModel = await _logisticsSortingRepository.FirstOrDefault(f =>
                                f.ModifyTime.Equals(model.LogisticsSortingItemInfo.ModifyTime) &&
                                f.ExitId.Equals(model.SelectPackageExitDefinitionInfo.Id));

                            var logisticsRuleInfoModels = model.LogisticsRuleItems.Select(s => new LogisticsRuleInfoModel() {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                LogisticsId = s.LogisticsId,
                                Remarks = s.Remarks,
                                LogisticsSortingId = sortingInfoModel.Id
                            })?.ToList() ?? new List<LogisticsRuleInfoModel>();

                            var ruleInfoModels = await _logisticsRuleRepository.Select(s => s.LogisticsSortingId.Equals(sortingInfoModel.Id),
                            o => o.Id);

                            if (ruleInfoModels?.Any() == true) {
                                await _logisticsRuleRepository.DeleteRange(ruleInfoModels);
                            }

                            var insertRange = await _logisticsRuleRepository.InsertRange(logisticsRuleInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(ruleInfoModels);
                                LogisticsSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                LogisticsSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            LogisticsSortingMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            });
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                RefreshData();
            }
        }

        public ICommand ModifyCommand {
            get => new DelegateCommand<LogisticsSortingItemInfoModel>(ModifyDelegate);
        }

        private async void ModifyDelegate(LogisticsSortingItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var logisticsSortingRuleEditor = new LogisticsSortingRuleEditor();
                if (logisticsSortingRuleEditor.DataContext is LogisticsSortingRuleEditorViewModel model) {
                    model.Identifier = "LogisticsSortingDialog";
                    model.LogisticsSortingItemInfo = obj;
                    model.LogisticsRuleItems = obj.LogisticsRuleItems;
                    await DialogHost.Show(logisticsSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        LogisticsSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        //添加到数据库
                        var logisticsSortingInfoModel = new LogisticsSortingInfoModel() {
                            CreateTime = model.LogisticsSortingItemInfo.CreateTime,
                            ModifyTime = model.LogisticsSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.LogisticsSortingItemInfo.Remarks,
                            SortingName = model.LogisticsSortingItemInfo.SortingName,
                            Id = model.LogisticsSortingItemInfo.Id,
                        };
                        var update = await _logisticsSortingRepository.Update(logisticsSortingInfoModel);
                        if (update) {
                            EventAggregator.Instance.Publish(logisticsSortingInfoModel);

                            var logisticsRuleInfoModels = model.LogisticsRuleItems.Select(s => new LogisticsRuleInfoModel() {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                LogisticsId = s.LogisticsId,
                                Remarks = s.Remarks,
                                LogisticsSortingId = model.LogisticsSortingItemInfo.Id
                            })?.ToList() ?? new List<LogisticsRuleInfoModel>();

                            var ruleInfoModels = await _logisticsRuleRepository.Select(s => s.LogisticsSortingId.Equals(model.LogisticsSortingItemInfo.Id),
                            o => o.Id);

                            if (ruleInfoModels?.Any() == true) {
                                await _logisticsRuleRepository.DeleteRange(ruleInfoModels);
                            }

                            var insertRange = await _logisticsRuleRepository.InsertRange(logisticsRuleInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(ruleInfoModels);
                                LogisticsSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                LogisticsSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            LogisticsSortingMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            });
        }

        public ICommand DeleteCommand {
            get => new DelegateCommand<LogisticsSortingItemInfoModel>(DeleteDelegate);
        }

        private async void DeleteDelegate(LogisticsSortingItemInfoModel obj) {
            var logisticsSortingInfoModel = await _logisticsSortingRepository.
                FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (logisticsSortingInfoModel is not null) {
                var delete = await _logisticsSortingRepository.Delete(logisticsSortingInfoModel);
                if (delete) {
                    //刷新列表
                    RefreshData();
                }
            }
        }

        private async void RefreshData() {
            var loadingDialog = new LoadingDialog();
            if (loadingDialog.DataContext is not LoadingDialogViewModel model) return;
            await Application.Current.Dispatcher.InvokeAsync(() => {
                model.Identifier = "LogisticsSortingDialog";
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            var logisticsCodeRecognitionInfoModels = await _logisticsCodeRecognitionRepository.Select(s => s.Id > 0,
                o => o.ModifyTime);

            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _logisticsSortingRepository
                .LogisticsSortingItems(s => s.Id > 0);

            await Application.Current.Dispatcher.InvokeAsync(() => {
                LogisticsSortingItems.Clear();
                var infoModels = models?.Select((s, i) => new LogisticsSortingItemInfoModel() {
                    CreateTime = s.CreateTime,
                    Id = s.Id,
                    ModifyTime = s.ModifyTime,
                    Num = i + 1,
                    Remarks = s.Remarks,
                    ExitId = s.ExitId,
                    SortingName = s.SortingName,
                    ExitName = packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ?? string.Empty,
                    LogisticsRuleItems = new ObservableCollection<LogisticsRuleItemInfoModel>(s.LogisticsRuleItems?.Select((s1, i1) => new LogisticsRuleItemInfoModel() {
                        CreateTime = s1.CreateTime,
                        Id = s1.Id,
                        LogisticsSortingId = s1.LogisticsSortingId,
                        ModifyTime = s1.ModifyTime,
                        Num = i1 + 1,
                        Remarks = s1.Remarks,
                        LogisticsId = s1.LogisticsId,
                        LogisticsName = logisticsCodeRecognitionInfoModels.FirstOrDefault(f => f.Id.Equals(s1.LogisticsId))?.LogisticsName ?? string.Empty
                    }).ToList() ?? new List<LogisticsRuleItemInfoModel>()),
                    SortingRuleGroup = string.Join(",", s.LogisticsRuleItems?.Select(s2 =>
                        logisticsCodeRecognitionInfoModels.FirstOrDefault(f => f.Id.Equals(s2.LogisticsId))?.LogisticsName ?? string.Empty) ?? Array.Empty<string>())
                })?.ToList();
                LogisticsSortingItems.AddRange(infoModels);
                if (DialogHost.IsDialogOpen(model.Identifier)) {
                    DialogHost.Close(model.Identifier);
                }
            });
        }

        /// <summary>
        /// 导出
        /// </summary>
        public ICommand ExportCommand {
            get => new DelegateCommand<object>(ExportDelegate);
        }

        private async void ExportDelegate(object obj) {
            //导出
            if (LogisticsSortingItems?.Any() != true) {
                LogisticsSortingMessageQueue?.Enqueue(Languages.Language.ResourceManager.GetString("列表中没有数据") ?? string.Empty);
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
                    var result = LogisticsSortingItems
                        ?.SelectMany(s => s.SortingRuleGroup.Split(",")
                            .Select(item => new LogisticsSortingItemInfoModel() {
                                CreateTime = s.CreateTime,
                                ExitId = s.ExitId,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                                ExitName = s.ExitName,
                                SortingName = s.SortingName,
                                Num = s.Num,
                                Id = s.Id,
                                SortingRuleGroup = item,
                            }))
                        ?.ToList();
                    var export = await _excel.Export(saveFileDialog.FileName,
                        $"物流分拣列表",
                        "物流分拣列表", result ?? new List<LogisticsSortingItemInfoModel>(),
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
                            LogisticsSortingMessageQueue?.Enqueue(e.Message);
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

        public ICommand ImportCommand {
            get => new DelegateCommand<object>(ImportDelegate);
        }

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

                    var models = await _excel.ReadExcel<LogisticsSortingItemInfoModel>(openFileDialog.FileName, async p => {
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
                        LogisticsSortingMessageQueue?.Enqueue(e.Message);
                    });
                    await Task.Delay(500);
                    if (models?.Any() == true) {
                        var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                            o => o.CreateTime);
                        var logisticsCodeRecognitionInfoModels = await _logisticsCodeRecognitionRepository.Select(s => s.Id > 0,
                            o => o.CreateTime);
                        var dateTime = DateTime.Now;
                        var logisticsSortingInfoModels = models
                            .Select(s => new LogisticsSortingInfoModel() {
                                CreateTime = dateTime,
                                ExitId = packageExitDefinitionInfoModels.FirstOrDefault(f => f.ExitName.Equals(s.ExitName))?.Id ?? 0,
                                ModifyTime = dateTime,
                                SortingName = s.SortingName,
                                Remarks = s.Remarks,
                                LogisticsRuleItems = new List<LogisticsRuleInfoModel>
                                {
                                    new()
                                    {
                                        CreateTime = dateTime,
                                        ModifyTime = dateTime,
                                        LogisticsId = logisticsCodeRecognitionInfoModels?.FirstOrDefault(f=>f.LogisticsName.Equals(s.SortingRuleGroup))?.Id??0,
                                    }
                                }
                            })
                            .GroupBy(s => s.ExitId)
                            .Select(group => new LogisticsSortingInfoModel {
                                CreateTime = group.First().CreateTime,
                                ExitId = group.Key,
                                SortingName = group.First().SortingName,
                                ModifyTime = group.First().ModifyTime,
                                Remarks = group.First().Remarks,
                                LogisticsRuleItems = group.SelectMany(item => item.LogisticsRuleItems).ToList()
                            })
                            .ToList();

                        //批量添加
                        var range = await _logisticsSortingRepository.InsertRange(logisticsSortingInfoModels);
                        if (range) {
                            //取出数据库对应指令列表内容
                            var infoModels = await _logisticsSortingRepository.SelectOrderByDescending(
                                s => s.CreateTime.Equals(dateTime),
                                o => o.CreateTime);
                            foreach (var logisticsSorting in infoModels) {
                                var logisticsRuleInfoModels = await _logisticsRuleRepository.Select(
                                    s => s.LogisticsSortingId.Equals(logisticsSorting.Id),
                                    o => o.Id);
                                if (logisticsRuleInfoModels?.Any() == true) {
                                    await _logisticsRuleRepository.DeleteRange(logisticsRuleInfoModels);
                                }

                                var logisticsSortingInfoModel = logisticsSortingInfoModels?.FirstOrDefault(f =>
                                    f.ExitId.Equals(logisticsSorting.ExitId) &&
                                    f.CreateTime.Equals(dateTime));
                                if (logisticsSortingInfoModel is not null) {
                                    var ruleInfoModels = logisticsSortingInfoModel?.LogisticsRuleItems?.Select(s =>
                                        new LogisticsRuleInfoModel() {
                                            LogisticsSortingId = logisticsSorting.Id,
                                            LogisticsId = s.LogisticsId
                                        })?.ToList();
                                    await _logisticsRuleRepository.InsertRange(ruleInfoModels ?? new List<LogisticsRuleInfoModel>());
                                }
                            }

                            LogisticsSortingMessageQueue.Enqueue("保存成功");
                            RefreshData();
                        }
                        else {
                            LogisticsSortingMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            }
        }
    }
}