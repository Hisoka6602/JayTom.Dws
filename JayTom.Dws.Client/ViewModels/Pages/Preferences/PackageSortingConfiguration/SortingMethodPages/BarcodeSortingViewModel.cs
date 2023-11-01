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

    public class BarcodeSortingViewModel : BindableBase {
        private readonly IBarCodeSortingRepository _barCodeSortingRepository;
        private readonly IBarCodeRegexRepository _barCodeRegexRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly IExcel _excel;

        private ObservableCollection<BarCodeSortingItemInfoModel> _barCodeSortingItems = new();

        private SnackbarMessageQueue _barcodeSortingMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public BarcodeSortingViewModel(IBarCodeSortingRepository barCodeSortingRepository,
            IBarCodeRegexRepository barCodeRegexRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IExcel excel) {
            _barCodeSortingRepository = barCodeSortingRepository;
            _barCodeRegexRepository = barCodeRegexRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _excel = excel;
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
            if (!_isLoaded) {
                _isLoaded = true;
                RefreshData();
            }
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

        /// <summary>
        /// 导出
        /// </summary>
        public ICommand ExportCommand {
            get => new DelegateCommand<object>(ExportDelegate);
        }

        private async void ExportDelegate(object obj) {
            //导出
            if (BarCodeSortingItems?.Any() != true) {
                BarcodeSortingMessageQueue?.Enqueue(Languages.Language.ResourceManager.GetString("列表中没有数据") ?? string.Empty);
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
                    var result = BarCodeSortingItems
                        ?.SelectMany(s => s.SortingRuleGroup.Split("\n")
                            .Select(item => new BarCodeSortingItemInfoModel {
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
                        $"条码分拣列表",
                        "条码分拣列表", result ?? new List<BarCodeSortingItemInfoModel>(),
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
                            BarcodeSortingMessageQueue?.Enqueue(e.Message);
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

                    var models = await _excel.ReadExcel<BarCodeSortingItemInfoModel>(openFileDialog.FileName, async p => {
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
                        BarcodeSortingMessageQueue?.Enqueue(e.Message);
                    });
                    await Task.Delay(500);
                    if (models?.Any() == true) {
                        var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                            o => o.CreateTime);
                        var dateTime = DateTime.Now;
                        var barCodeSortingInfoModels = models
                            .Select(s => new BarCodeSortingInfoModel() {
                                CreateTime = dateTime,
                                ExitId = packageExitDefinitionInfoModels.FirstOrDefault(f => f.ExitName.Equals(s.ExitName))?.Id ?? 0,
                                ModifyTime = dateTime,
                                SortingName = s.SortingName,
                                Remarks = s.Remarks,
                                BarCodeRegexItems = new List<BarCodeRegexInfoModel>
                                {
                                    new()
                                    {
                                        CreateTime = dateTime,
                                        ModifyTime = dateTime,
                                        RegexPattern = s.SortingRuleGroup
                                    }
                                }
                            })
                            .GroupBy(s => s.ExitId)
                            .Select(group => new BarCodeSortingInfoModel {
                                CreateTime = group.First().CreateTime,
                                ExitId = group.Key,
                                SortingName = group.First().SortingName,
                                ModifyTime = group.First().ModifyTime,
                                Remarks = group.First().Remarks,
                                BarCodeRegexItems = group.SelectMany(item => item.BarCodeRegexItems).ToList()
                            })
                            .ToList();

                        //批量添加
                        var range = await _barCodeSortingRepository.InsertRange(barCodeSortingInfoModels);
                        if (range) {
                            //取出数据库对应指令列表内容
                            var infoModels = await _barCodeSortingRepository.SelectOrderByDescending(
                                s => s.CreateTime.Equals(dateTime),
                                o => o.CreateTime);

                            foreach (var barCodeSorting in infoModels) {
                                var barCodeRegexInfoModels = await _barCodeRegexRepository.Select(
                                    s => s.BarCodeSortingId.Equals(barCodeSorting.Id),
                                    o => o.Id);
                                if (barCodeRegexInfoModels?.Any() == true) {
                                    await _barCodeRegexRepository.DeleteRange(barCodeRegexInfoModels);
                                }

                                var barCodeSortingInfoModel = barCodeSortingInfoModels?.FirstOrDefault(f =>
                                    f.ExitId.Equals(barCodeSorting.ExitId) &&
                                    f.CreateTime.Equals(dateTime));
                                if (barCodeSortingInfoModel is not null) {
                                    var instructionInfoModels = barCodeSortingInfoModel?.BarCodeRegexItems.Select(s =>
                                        new BarCodeRegexInfoModel {
                                            RegexPattern = s.RegexPattern,
                                            BarCodeSortingId = barCodeSorting.Id
                                        })?.ToList();
                                    await _barCodeRegexRepository.InsertRange(instructionInfoModels ?? new List<BarCodeRegexInfoModel>());
                                }
                            }

                            BarcodeSortingMessageQueue.Enqueue("保存成功");
                            RefreshData();
                        }
                        else {
                            BarcodeSortingMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            }
        }
    }
}