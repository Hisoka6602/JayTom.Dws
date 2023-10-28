using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using JayTom.Dws.Plugin;
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
        private readonly IExcel _excel;
        private ObservableCollection<OcrSortingItemInfoModel> _ocrSortingItems = new();
        private SnackbarMessageQueue _ocrSortingMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public OcrSortingViewModel(IOcrSortingRepository ocrSortingRepository,
            IOcrRuleRepository ocrRuleRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IExcel excel) {
            _ocrSortingRepository = ocrSortingRepository;
            _ocrRuleRepository = ocrRuleRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _excel = excel;
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


        /// <summary>
        /// 导出
        /// </summary>
        public ICommand ExportCommand {
            get => new DelegateCommand<object>(ExportDelegate);
        }

        private async void ExportDelegate(object obj) {


            //导出
            if (OcrSortingItems?.Any() != true) {
                OcrSortingMessageQueue?.Enqueue(Languages.Language.ResourceManager.GetString("列表中没有数据") ?? string.Empty);
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
                    var result = OcrSortingItems
                        ?.SelectMany(s => s.SortingRuleGroup.Split("\n")
                            .Select(item => new OcrSortingItemInfoModel() {
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
                        $"Ocr分拣列表",
                        "Ocr分拣列表", result ?? new List<OcrSortingItemInfoModel>(),
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
                            OcrSortingMessageQueue?.Enqueue(e.Message);
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

                    var models = await _excel.ReadExcel<OcrSortingItemInfoModel>(openFileDialog.FileName, async p => {
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
                        OcrSortingMessageQueue?.Enqueue(e.Message);
                    });
                    await Task.Delay(500);
                    if (models?.Any() == true) {
                        var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                            o => o.CreateTime);
                        var dateTime = DateTime.Now;
                        var ocrSortingInfoModels = models
                            .Select(s => new OcrSortingInfoModel() {
                                CreateTime = dateTime,
                                ExitId = packageExitDefinitionInfoModels.FirstOrDefault(f => f.ExitName.Equals(s.ExitName))?.Id ?? 0,
                                ModifyTime = dateTime,
                                SortingName = s.SortingName,
                                Remarks = s.Remarks,
                                OcrRuleItems = new List<OcrRuleInfoModel>
                                {
                                    new()
                                    {
                                        CreateTime = dateTime,
                                        ModifyTime = dateTime,
                                        RegexPattern = s.SortingRuleGroup,
                                    }
                                }
                            })
                            .GroupBy(s => s.ExitId)
                            .Select(group => new OcrSortingInfoModel {
                                CreateTime = group.First().CreateTime,
                                ExitId = group.Key,
                                SortingName = group.First().SortingName,
                                ModifyTime = group.First().ModifyTime,
                                Remarks = group.First().Remarks,
                                OcrRuleItems = group.SelectMany(item => item.OcrRuleItems).ToList()
                            })
                            .ToList();

                        //批量添加
                        var range = await _ocrSortingRepository.InsertRange(ocrSortingInfoModels);
                        if (range) {
                            //取出数据库对应指令列表内容
                            var infoModels = await _ocrSortingRepository.SelectOrderByDescending(
                                s => s.CreateTime.Equals(dateTime),
                                o => o.CreateTime);
                            foreach (var ocrSorting in infoModels) {
                                var ocrRuleInfoModels = await _ocrRuleRepository.Select(
                                    s => s.OcrSortingInfo.Equals(ocrSorting.Id),
                                    o => o.Id);
                                if (ocrRuleInfoModels?.Any() == true) {
                                    await _ocrRuleRepository.DeleteRange(ocrRuleInfoModels);
                                }

                                var ocrSortingInfoModel = ocrSortingInfoModels?.FirstOrDefault(f =>
                                    f.ExitId.Equals(ocrSorting.ExitId) &&
                                    f.CreateTime.Equals(dateTime));
                                if (ocrSortingInfoModel is not null) {
                                    var ruleInfoModels = ocrSortingInfoModel?.OcrRuleItems.Select(s =>
                                        new OcrRuleInfoModel() {
                                            RegexPattern = s.RegexPattern,
                                            OcrSortingId = ocrSorting.Id,
                                        })?.ToList();
                                    await _ocrRuleRepository.InsertRange(ruleInfoModels ?? new List<OcrRuleInfoModel>());
                                }
                            }

                            OcrSortingMessageQueue.Enqueue("保存成功");
                            RefreshData();
                        }
                        else {
                            OcrSortingMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            }
        }
    }
}