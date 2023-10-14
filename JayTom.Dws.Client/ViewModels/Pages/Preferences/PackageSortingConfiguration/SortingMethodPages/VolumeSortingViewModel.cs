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
    public class VolumeSortingViewModel : BindableBase {
        private readonly IVolumeSortingRepository _volumeSortingRepository;
        private readonly IVolumeRuleRepository _volumeRuleRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;

        private ObservableCollection<VolumeSortingItemInfoModel> _volumeSortingItems = new();

        private SnackbarMessageQueue _volumeSortingMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public VolumeSortingViewModel(IVolumeSortingRepository volumeSortingRepository,
            IVolumeRuleRepository volumeRuleRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository) {
            _volumeSortingRepository = volumeSortingRepository;
            _volumeRuleRepository = volumeRuleRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
        }

        public ObservableCollection<VolumeSortingItemInfoModel> VolumeSortingItems {
            get => _volumeSortingItems;
            set => SetProperty(ref _volumeSortingItems, value);
        }

        public SnackbarMessageQueue VolumeSortingMessageQueue {
            get => _volumeSortingMessageQueue;
            set => SetProperty(ref _volumeSortingMessageQueue, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var volumeSortingRuleEditor = new VolumeSortingRuleEditor();
                if (volumeSortingRuleEditor.DataContext is VolumeSortingRuleEditorViewModel model) {
                    model.Identifier = "VolumeSortingDialog";
                    await DialogHost.Show(volumeSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        VolumeSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        //添加到数据库
                        var volumeSortingInfoModel = new VolumeSortingInfoModel() {
                            CreateTime = model.VolumeSortingItemInfo.CreateTime,
                            ModifyTime = model.VolumeSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.VolumeSortingItemInfo.Remarks,
                            SortingName = model.VolumeSortingItemInfo.SortingName,
                        };
                        var insert = await _volumeSortingRepository.Insert(volumeSortingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(volumeSortingInfoModel);

                            var sortingInfoModel = await _volumeSortingRepository.FirstOrDefault(f =>
                                f.ModifyTime.Equals(model.VolumeSortingItemInfo.ModifyTime) &&
                                f.ExitId.Equals(model.SelectPackageExitDefinitionInfo.Id));
                            var volumeRuleInfoModels = model.VolumeRuleItems?.Select(s => new VolumeRuleInfoModel() {
                                CreateTime = s.CreateTime,
                                Formula = s.Formula,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                                VolumeSortingId = sortingInfoModel.Id,
                            })?.ToList() ?? new List<VolumeRuleInfoModel>();
                            var ruleInfoModels = await _volumeRuleRepository.Select(s =>
                                s.VolumeSortingId.Equals(sortingInfoModel.Id), o => o.Id);
                            if (ruleInfoModels?.Any() == true) {
                                await _volumeRuleRepository.DeleteRange(ruleInfoModels);
                            }

                            var insertRange = await _volumeRuleRepository.InsertRange(volumeRuleInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(ruleInfoModels);
                                VolumeSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                VolumeSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            VolumeSortingMessageQueue.Enqueue("保存失败");
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
            get => new DelegateCommand<VolumeSortingItemInfoModel>(ModifyDelegate);
        }

        private async void ModifyDelegate(VolumeSortingItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var volumeSortingRuleEditor = new VolumeSortingRuleEditor();
                if (volumeSortingRuleEditor.DataContext is VolumeSortingRuleEditorViewModel model) {
                    model.Identifier = "VolumeSortingDialog";
                    model.VolumeSortingItemInfo = obj;
                    model.VolumeRuleItems = obj.VolumeRuleItems;
                    await DialogHost.Show(volumeSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        VolumeSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        //添加到数据库
                        var volumeSortingInfoModel = new VolumeSortingInfoModel() {
                            CreateTime = model.VolumeSortingItemInfo.CreateTime,
                            ModifyTime = model.VolumeSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.VolumeSortingItemInfo.Remarks,
                            SortingName = model.VolumeSortingItemInfo.SortingName,
                            Id = model.VolumeSortingItemInfo.Id,
                        };
                        var insert = await _volumeSortingRepository.Update(volumeSortingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(insert);

                            var volumeRuleInfoModels = model.VolumeRuleItems?.Select(s => new VolumeRuleInfoModel() {
                                CreateTime = s.CreateTime,
                                Formula = s.Formula,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                                VolumeSortingId = model.VolumeSortingItemInfo.Id,
                            })?.ToList() ?? new List<VolumeRuleInfoModel>();
                            var ruleInfoModels = await _volumeRuleRepository.Select(s =>
                                s.VolumeSortingId.Equals(model.VolumeSortingItemInfo.Id), o => o.Id);
                            if (ruleInfoModels?.Any() == true) {
                                await _volumeRuleRepository.DeleteRange(ruleInfoModels);
                            }

                            var insertRange = await _volumeRuleRepository.InsertRange(volumeRuleInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(ruleInfoModels);
                                VolumeSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                VolumeSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            VolumeSortingMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            });
        }

        public ICommand DeleteCommand {
            get => new DelegateCommand<VolumeSortingItemInfoModel>(DeleteDelegate);
        }

        private async void DeleteDelegate(VolumeSortingItemInfoModel obj) {
            var volumeSortingInfoModel = await _volumeSortingRepository.
                FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (volumeSortingInfoModel is not null) {
                var delete = await _volumeSortingRepository.Delete(volumeSortingInfoModel);
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
                model.Identifier = "VolumeSortingDialog";
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _volumeSortingRepository
                .VolumeSortingItems(s => s.Id > 0);

            await Application.Current.Dispatcher.InvokeAsync(() => {
                VolumeSortingItems.Clear();
                var infoModels = models?.Select((s, i) => new VolumeSortingItemInfoModel() {
                    CreateTime = s.CreateTime,
                    Id = s.Id,
                    ModifyTime = s.ModifyTime,
                    Num = i + 1,
                    Remarks = s.Remarks,
                    ExitId = s.ExitId,
                    SortingName = s.SortingName,
                    ExitName = packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ?? string.Empty,
                    VolumeRuleItems = new ObservableCollection<VolumeRuleItemInfoModel>(s.VolumeRuleItems?.Select((s1, i1) => new VolumeRuleItemInfoModel() {
                        CreateTime = s1.CreateTime,
                        Id = s1.Id,
                        VolumeSortingId = s1.VolumeSortingId,
                        ModifyTime = s1.ModifyTime,
                        Num = i1 + 1,
                        Remarks = s1.Remarks,
                        Formula = s1.Formula
                    }).ToList() ?? new List<VolumeRuleItemInfoModel>()),
                    SortingRuleGroup = string.Join("\n", s.VolumeRuleItems?.Select(s2 => s2.Formula) ?? Array.Empty<string>())
                })?.ToList();
                VolumeSortingItems.AddRange(infoModels);
                if (DialogHost.IsDialogOpen(model.Identifier)) {
                    DialogHost.Close(model.Identifier);
                }
            });
        }
    }
}