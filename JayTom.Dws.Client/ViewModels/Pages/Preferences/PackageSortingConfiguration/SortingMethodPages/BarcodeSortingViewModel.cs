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
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages {

    public class BarcodeSortingViewModel : BindableBase {
        private readonly IBarCodeSortingRepository _barCodeSortingRepository;
        private readonly IBarCodeRegexRepository _barCodeRegexRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;

        private ObservableCollection<BarCodeSortingItemInfoModel> _barCodeSortingItems = new();

        private SnackbarMessageQueue _barcodeSortingMessageQueue = new(TimeSpan.FromSeconds(2));

        public BarcodeSortingViewModel(IBarCodeSortingRepository barCodeSortingRepository,
            IBarCodeRegexRepository barCodeRegexRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository) {
            _barCodeSortingRepository = barCodeSortingRepository;
            _barCodeRegexRepository = barCodeRegexRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
        }

        public ObservableCollection<BarCodeSortingItemInfoModel> BarCodeSortingItems {
            get => _barCodeSortingItems;
            set => SetProperty(ref _barCodeSortingItems, value);
        }

        public SnackbarMessageQueue BarcodeSortingMessageQueue {
            get => _barcodeSortingMessageQueue;
            set => SetProperty(ref _barcodeSortingMessageQueue, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var barcodeSortingRuleEditor = new BarcodeSortingRuleEditor();
                if (barcodeSortingRuleEditor.DataContext is BarcodeSortingRuleEditorViewModel model) {
                    model.Identifier = "BarcodeSortingDialog";
                    await DialogHost.Show(barcodeSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        BarcodeSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        //添加到数据库
                        var infoModel = new BarCodeSortingInfoModel() {
                            CreateTime = model.BarCodeSortingItemInfo.CreateTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            ModifyTime = model.BarCodeSortingItemInfo.ModifyTime,
                            Remarks = model.BarCodeSortingItemInfo.Remarks,
                            SortingName = model.BarCodeSortingItemInfo.SortingName
                        };
                        var insertOrUpdate = await _barCodeSortingRepository.Insert(infoModel);
                        if (insertOrUpdate) {
                            EventAggregator.Instance.Publish(infoModel);
                            var barCodeSortingInfoModel = await _barCodeSortingRepository.FirstOrDefault(f =>
                                f.ModifyTime.Equals(model.BarCodeSortingItemInfo.ModifyTime) &&
                                f.ExitId.Equals(model.SelectPackageExitDefinitionInfo.Id));

                            var codeRegexInfoModels = model.BarCodeRegexItems.Select(s => new BarCodeRegexInfoModel {
                                BarCodeSortingId = barCodeSortingInfoModel.Id,
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                                RegexPattern = s.RegexPattern,
                            })?.ToList() ?? new List<BarCodeRegexInfoModel>();
                            var barCodeRegexInfoModels = await _barCodeRegexRepository.Select(s =>
                                s.BarCodeSortingId.Equals(barCodeSortingInfoModel.Id), o => o.Id);
                            if (barCodeRegexInfoModels?.Any() == true) {
                                await _barCodeRegexRepository.DeleteRange(barCodeRegexInfoModels);
                            }

                            var insertRange = await _barCodeRegexRepository.InsertRange(codeRegexInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(codeRegexInfoModels);
                                BarcodeSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                BarcodeSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            BarcodeSortingMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            });
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj) {
            RefreshData();
        }

        public ICommand ModifyCommand {
            get => new DelegateCommand<BarCodeSortingItemInfoModel>(ModifyDelegate);
        }

        private async void ModifyDelegate(BarCodeSortingItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var barcodeSortingRuleEditor = new BarcodeSortingRuleEditor();
                if (barcodeSortingRuleEditor.DataContext is BarcodeSortingRuleEditorViewModel model) {
                    model.Identifier = "BarcodeSortingDialog";
                    model.BarCodeSortingItemInfo = obj;
                    model.BarCodeRegexItems = obj.BarCodeRegexItems;
                    await DialogHost.Show(barcodeSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        BarcodeSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        //添加到数据库
                        var infoModel = new BarCodeSortingInfoModel() {
                            CreateTime = model.BarCodeSortingItemInfo.CreateTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            ModifyTime = model.BarCodeSortingItemInfo.ModifyTime,
                            Remarks = model.BarCodeSortingItemInfo.Remarks,
                            SortingName = model.BarCodeSortingItemInfo.SortingName,
                            Id = model.BarCodeSortingItemInfo.Id
                        };
                        var insertOrUpdate = await _barCodeSortingRepository.Update(infoModel);
                        if (insertOrUpdate) {
                            EventAggregator.Instance.Publish(infoModel);
                            var barCodeSortingInfoModel = await _barCodeSortingRepository.FirstOrDefault(f =>
                                f.ModifyTime.Equals(model.BarCodeSortingItemInfo.ModifyTime) &&
                                f.ExitId.Equals(model.SelectPackageExitDefinitionInfo.Id));

                            var codeRegexInfoModels = model.BarCodeRegexItems.Select(s => new BarCodeRegexInfoModel {
                                BarCodeSortingId = barCodeSortingInfoModel.Id,
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                                RegexPattern = s.RegexPattern,
                            })?.ToList() ?? new List<BarCodeRegexInfoModel>();
                            var barCodeRegexInfoModels = await _barCodeRegexRepository.Select(s =>
                                s.BarCodeSortingId.Equals(barCodeSortingInfoModel.Id), o => o.Id);
                            if (barCodeRegexInfoModels?.Any() == true) {
                                await _barCodeRegexRepository.DeleteRange(barCodeRegexInfoModels);
                            }

                            var insertRange = await _barCodeRegexRepository.InsertRange(codeRegexInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(codeRegexInfoModels);
                                BarcodeSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                BarcodeSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            BarcodeSortingMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            });
        }

        public ICommand DeleteCommand {
            get => new DelegateCommand<BarCodeSortingItemInfoModel>(DeleteDelegate);
        }

        private async void DeleteDelegate(BarCodeSortingItemInfoModel obj) {
            var logisticsCodeRecognitionInfoModel = await _barCodeSortingRepository.
                FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (logisticsCodeRecognitionInfoModel is not null) {
                var delete = await _barCodeSortingRepository.Delete(logisticsCodeRecognitionInfoModel);
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
                model.Identifier = "BarcodeSortingDialog";
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _barCodeSortingRepository
                .BarCodeSortingItems(s => s.Id > 0);

            await Application.Current.Dispatcher.InvokeAsync(() => {
                BarCodeSortingItems.Clear();
                var infoModels = models?.Select((s, i) => new BarCodeSortingItemInfoModel {
                    CreateTime = s.CreateTime,
                    Id = s.Id,
                    ModifyTime = s.ModifyTime,
                    Num = i + 1,
                    Remarks = s.Remarks,
                    SortingName = s.SortingName,
                    ExitId = s.ExitId,
                    ExitName = packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ?? string.Empty,
                    BarCodeRegexItems = new ObservableCollection<BarCodeRegexItemInfoModel>(s.BarCodeRegexItems?.Select((s1, i1) => new BarCodeRegexItemInfoModel {
                        CreateTime = s1.CreateTime,
                        Id = s1.Id,
                        BarCodeSortingId = s1.BarCodeSortingId,
                        ModifyTime = s1.ModifyTime,
                        Num = i1 + 1,
                        Remarks = s1.Remarks,
                        RegexPattern = s1.RegexPattern
                    }).ToList() ?? new List<BarCodeRegexItemInfoModel>()),
                    SortingRuleGroup = string.Join("\n", s.BarCodeRegexItems?.Select(s2 => s2.RegexPattern) ?? Array.Empty<string>())
                })?.ToList();
                BarCodeSortingItems.AddRange(infoModels);
                if (DialogHost.IsDialogOpen(model.Identifier)) {
                    DialogHost.Close(model.Identifier);
                }
            });
        }
    }
}