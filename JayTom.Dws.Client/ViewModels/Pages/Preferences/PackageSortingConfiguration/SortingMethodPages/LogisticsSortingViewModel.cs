using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages {
    public class LogisticsSortingViewModel : BindableBase {
        private readonly ILogisticsSortingRepository _logisticsSortingRepository;
        private readonly ILogisticsRuleRepository _logisticsRuleRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly ILogisticsCodeRecognitionRepository _logisticsCodeRecognitionRepository;
        private bool _isLoaded;
        private ObservableCollection<LogisticsSortingItemInfoModel> _logisticsSortingItems = new();
        private SnackbarMessageQueue _logisticsSortingMessageQueue = new(TimeSpan.FromSeconds(2));

        public LogisticsSortingViewModel(ILogisticsSortingRepository logisticsSortingRepository,
            ILogisticsRuleRepository logisticsRuleRepository, IPackageExitDefinitionRepository packageExitDefinitionRepository,
            ILogisticsCodeRecognitionRepository logisticsCodeRecognitionRepository) {
            _logisticsSortingRepository = logisticsSortingRepository;
            _logisticsRuleRepository = logisticsRuleRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _logisticsCodeRecognitionRepository = logisticsCodeRecognitionRepository;
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
    }
}