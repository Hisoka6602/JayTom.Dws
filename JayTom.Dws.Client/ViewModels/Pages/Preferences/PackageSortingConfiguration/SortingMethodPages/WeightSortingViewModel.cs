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
using JayTom.Dws.Client.Models.ImageSettingModels;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages {

    public class WeightSortingViewModel : BindableBase {
        private readonly IWeightSortingRepository _weightSortingRepository;
        private readonly IWeightRuleRepository _weightRuleRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private SnackbarMessageQueue _weightSortingMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;
        private ObservableCollection<WeightSortingItemInfoModel> _weightSortingItems = new();

        public WeightSortingViewModel(IWeightSortingRepository weightSortingRepository,
            IWeightRuleRepository weightRuleRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository) {
            _weightSortingRepository = weightSortingRepository;
            _weightRuleRepository = weightRuleRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
        }

        public SnackbarMessageQueue WeightSortingMessageQueue {
            get => _weightSortingMessageQueue;
            set => SetProperty(ref _weightSortingMessageQueue, value);
        }

        public ObservableCollection<WeightSortingItemInfoModel> WeightSortingItems {
            get => _weightSortingItems;
            set => SetProperty(ref _weightSortingItems, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var weightSortingRuleEditor = new WeightSortingRuleEditor();
                if (weightSortingRuleEditor.DataContext is WeightSortingRuleEditorViewModel model) {
                    model.Identifier = "WeightSortingDialog";
                    await DialogHost.Show(weightSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        WeightSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        //添加到数据库
                        var weightSortingInfoModel = new WeightSortingInfoModel() {
                            CreateTime = model.WeightSortingItemInfo.CreateTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            ModifyTime = model.WeightSortingItemInfo.ModifyTime,
                            Remarks = model.WeightSortingItemInfo.Remarks,
                            SortingName = model.WeightSortingItemInfo.SortingName,
                        };
                        var insert = await _weightSortingRepository.Insert(weightSortingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(insert);

                            var sortingInfoModel = await _weightSortingRepository.FirstOrDefault(f =>
                                f.ModifyTime.Equals(model.WeightSortingItemInfo.ModifyTime) &&
                                f.ExitId.Equals(model.SelectPackageExitDefinitionInfo.Id));

                            var weightRuleInfoModels = model.WeightRuleItems.Select(s => new WeightRuleInfoModel {
                                CreateTime = s.CreateTime,
                                Formula = s.Formula,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                                WeightSortingId = sortingInfoModel.Id,
                            })?.ToList() ?? new List<WeightRuleInfoModel>();

                            var ruleInfoModels = await _weightRuleRepository.Select(s =>
                                s.WeightSortingId.Equals(sortingInfoModel.Id), o => o.Id);
                            if (ruleInfoModels?.Any() == true) {
                                await _weightRuleRepository.DeleteRange(ruleInfoModels);
                            }

                            var insertRange = await _weightRuleRepository.InsertRange(weightRuleInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(ruleInfoModels);
                                WeightSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                WeightSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            WeightSortingMessageQueue.Enqueue("保存失败");
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
            get => new DelegateCommand<WeightSortingItemInfoModel>(ModifyDelegate);
        }

        private async void ModifyDelegate(WeightSortingItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var weightSortingRuleEditor = new WeightSortingRuleEditor();
                if (weightSortingRuleEditor.DataContext is WeightSortingRuleEditorViewModel model) {
                    model.Identifier = "WeightSortingDialog";
                    model.WeightSortingItemInfo = obj;
                    model.WeightRuleItems = obj.WeightRuleItems;
                    await DialogHost.Show(weightSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        WeightSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        //添加到数据库
                        var weightSortingInfoModel = new WeightSortingInfoModel() {
                            CreateTime = model.WeightSortingItemInfo.CreateTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            ModifyTime = model.WeightSortingItemInfo.ModifyTime,
                            Remarks = model.WeightSortingItemInfo.Remarks,
                            SortingName = model.WeightSortingItemInfo.SortingName,
                            Id = model.WeightSortingItemInfo.Id
                        };
                        var insert = await _weightSortingRepository.Update(weightSortingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(insert);

                            ;

                            var weightRuleInfoModels = model.WeightRuleItems.Select(s => new WeightRuleInfoModel {
                                CreateTime = s.CreateTime,
                                Formula = s.Formula,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                                WeightSortingId = model.WeightSortingItemInfo.Id,
                            })?.ToList() ?? new List<WeightRuleInfoModel>();

                            var ruleInfoModels = await _weightRuleRepository.Select(s =>
                                s.WeightSortingId.Equals(model.WeightSortingItemInfo.Id), o => o.Id);
                            if (ruleInfoModels?.Any() == true) {
                                await _weightRuleRepository.DeleteRange(ruleInfoModels);
                            }

                            var insertRange = await _weightRuleRepository.InsertRange(weightRuleInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(ruleInfoModels);
                                WeightSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                WeightSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                    }
                }
            });
        }

        public ICommand DeleteCommand {
            get => new DelegateCommand<WeightSortingItemInfoModel>(DeleteDelegate);
        }

        private async void DeleteDelegate(WeightSortingItemInfoModel obj) {
            var weightSortingInfoModel = await _weightSortingRepository.
                FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (weightSortingInfoModel is not null) {
                var delete = await _weightSortingRepository.Delete(weightSortingInfoModel);
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
                model.Identifier = "WeightSortingDialog";
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _weightSortingRepository
                .WeightSortingItems(s => s.Id > 0);

            await Application.Current.Dispatcher.InvokeAsync(() => {
                WeightSortingItems.Clear();
                var infoModels = models?.Select((s, i) => new WeightSortingItemInfoModel() {
                    CreateTime = s.CreateTime,
                    Id = s.Id,
                    ModifyTime = s.ModifyTime,
                    Num = i + 1,
                    Remarks = s.Remarks,
                    ExitId = s.ExitId,
                    SortingName = s.SortingName,
                    ExitName = packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ?? string.Empty,
                    WeightRuleItems = new ObservableCollection<WeightRuleItemInfoModel>(s.WeightRuleItems?.Select((s1, i1) => new WeightRuleItemInfoModel() {
                        CreateTime = s1.CreateTime,
                        Id = s1.Id,
                        WeightSortingId = s1.WeightSortingId,
                        ModifyTime = s1.ModifyTime,
                        Num = i1 + 1,
                        Remarks = s1.Remarks,
                        Formula = s1.Formula
                    }).ToList() ?? new List<WeightRuleItemInfoModel>()),
                    SortingRuleGroup = string.Join("\n", s.WeightRuleItems?.Select(s2 => s2.Formula) ?? Array.Empty<string>())
                })?.ToList();
                WeightSortingItems.AddRange(infoModels);
                if (DialogHost.IsDialogOpen(model.Identifier)) {
                    DialogHost.Close(model.Identifier);
                }
            });
        }
    }
}