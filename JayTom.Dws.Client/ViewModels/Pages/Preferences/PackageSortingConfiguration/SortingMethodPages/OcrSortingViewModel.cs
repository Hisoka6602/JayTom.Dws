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
    public class OcrSortingViewModel : BindableBase {
        private readonly IOcrSortingRepository _ocrSortingRepository;
        private readonly IOcrRuleRepository _ocrRuleRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private ObservableCollection<OcrSortingItemInfoModel> _ocrSortingItems = new();
        private SnackbarMessageQueue _ocrSortingMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public OcrSortingViewModel(IOcrSortingRepository ocrSortingRepository,
            IOcrRuleRepository ocrRuleRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository) {
            _ocrSortingRepository = ocrSortingRepository;
            _ocrRuleRepository = ocrRuleRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
        }

        public ObservableCollection<OcrSortingItemInfoModel> OcrSortingItems {
            get => _ocrSortingItems;
            set => SetProperty(ref _ocrSortingItems, value);
        }

        public SnackbarMessageQueue OcrSortingMessageQueue {
            get => _ocrSortingMessageQueue;
            set => SetProperty(ref _ocrSortingMessageQueue, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var ocrSortingRuleEditor = new OcrSortingRuleEditor();
                if (ocrSortingRuleEditor.DataContext is OcrSortingRuleEditorViewModel model) {
                    model.Identifier = "OcrSortingDialog";
                    await DialogHost.Show(ocrSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        OcrSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk) {
                        var ocrSortingInfoModel = new OcrSortingInfoModel() {
                            CreateTime = model.OcrSortingItemInfo.CreateTime,
                            ModifyTime = model.OcrSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.OcrSortingItemInfo.Remarks,
                            SortingName = model.OcrSortingItemInfo.SortingName,
                        };
                        var insert = await _ocrSortingRepository.Insert(ocrSortingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(ocrSortingInfoModel);

                            var sortingInfoModel = await _ocrSortingRepository.FirstOrDefault(f =>
                                f.ModifyTime.Equals(model.OcrSortingItemInfo.ModifyTime) &&
                                f.ExitId.Equals(model.SelectPackageExitDefinitionInfo.Id));
                            var ocrRuleInfoModels = model.OcrRuleItems.Select(s => new OcrRuleInfoModel() {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                OcrSortingId = sortingInfoModel.Id,
                                Remarks = s.Remarks,
                                RegexPattern = s.RegexPattern
                            })?.ToList() ?? new List<OcrRuleInfoModel>();

                            var ruleInfoModels = await _ocrRuleRepository.Select(s =>
                                    s.OcrSortingId.Equals(sortingInfoModel.Id),
                                o => o.Id);
                            if (ruleInfoModels?.Any() == true) {
                                await _ocrRuleRepository.DeleteRange(ruleInfoModels);
                            }

                            var insertRange = await _ocrRuleRepository.InsertRange(ocrRuleInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(ruleInfoModels);
                                OcrSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                OcrSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            OcrSortingMessageQueue.Enqueue("保存失败");
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
            get => new DelegateCommand<OcrSortingItemInfoModel>(ModifyDelegate);
        }

        private async void ModifyDelegate(OcrSortingItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var ocrSortingRuleEditor = new OcrSortingRuleEditor();
                if (ocrSortingRuleEditor.DataContext is OcrSortingRuleEditorViewModel model) {
                    model.Identifier = "OcrSortingDialog";
                    model.OcrSortingItemInfo = obj;
                    model.OcrRuleItems = obj.OcrRuleItems ?? new ObservableCollection<OcrRuleItemInfoModel>();
                    await DialogHost.Show(ocrSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        OcrSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk) {
                        var ocrSortingInfoModel = new OcrSortingInfoModel() {
                            CreateTime = model.OcrSortingItemInfo.CreateTime,
                            ModifyTime = model.OcrSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.OcrSortingItemInfo.Remarks,
                            SortingName = model.OcrSortingItemInfo.SortingName,
                            Id = model.OcrSortingItemInfo.Id
                        };
                        var insert = await _ocrSortingRepository.Update(ocrSortingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(ocrSortingInfoModel);


                            var ocrRuleInfoModels = model.OcrRuleItems.Select(s => new OcrRuleInfoModel() {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                OcrSortingId = model.OcrSortingItemInfo.Id,
                                Remarks = s.Remarks,
                                RegexPattern = s.RegexPattern
                            })?.ToList() ?? new List<OcrRuleInfoModel>();

                            var ruleInfoModels = await _ocrRuleRepository.Select(s =>
                                    s.OcrSortingId.Equals(model.OcrSortingItemInfo.Id),
                                o => o.Id);
                            if (ruleInfoModels?.Any() == true) {
                                await _ocrRuleRepository.DeleteRange(ruleInfoModels);
                            }

                            var insertRange = await _ocrRuleRepository.InsertRange(ocrRuleInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(ruleInfoModels);
                                OcrSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                OcrSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            OcrSortingMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            });
        }

        public ICommand DeleteCommand {
            get => new DelegateCommand<OcrSortingItemInfoModel>(DeleteDelegate);
        }

        private async void DeleteDelegate(OcrSortingItemInfoModel obj) {
            var ocrSortingInfoModel = await _ocrSortingRepository.
                FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (ocrSortingInfoModel is not null) {
                var delete = await _ocrSortingRepository.Delete(ocrSortingInfoModel);
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
                model.Identifier = "OcrSortingDialog";
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _ocrSortingRepository
                .OcrSortingItems(s => s.Id > 0);

            await Application.Current.Dispatcher.InvokeAsync(() => {
                OcrSortingItems.Clear();
                var infoModels = models?.Select((s, i) => new OcrSortingItemInfoModel() {
                    CreateTime = s.CreateTime,
                    Id = s.Id,
                    ModifyTime = s.ModifyTime,
                    Num = i + 1,
                    Remarks = s.Remarks,
                    ExitId = s.ExitId,
                    SortingName = s.SortingName,
                    ExitName = packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ?? string.Empty,
                    OcrRuleItems = new ObservableCollection<OcrRuleItemInfoModel>(s.OcrRuleItems?.Select((s1, i1) => new OcrRuleItemInfoModel() {
                        CreateTime = s1.CreateTime,
                        Id = s1.Id,
                        OcrSortingId = s1.OcrSortingId,
                        ModifyTime = s1.ModifyTime,
                        Num = i1 + 1,
                        Remarks = s1.Remarks,
                        RegexPattern = s1.RegexPattern
                    }).ToList() ?? new List<OcrRuleItemInfoModel>()),
                    SortingRuleGroup = string.Join("\n", s.OcrRuleItems?.Select(s2 => s2.RegexPattern) ?? Array.Empty<string>())
                })?.ToList();
                OcrSortingItems.AddRange(infoModels);
                if (DialogHost.IsDialogOpen(model.Identifier)) {
                    DialogHost.Close(model.Identifier);
                }
            });
        }
    }
}