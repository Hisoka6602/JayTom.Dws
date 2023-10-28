using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using JayTom.Dws.Plugin;
using System.Windows.Input;
using System.Net.Http.Json;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Client.Models.PackageSorting.Excel;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages {
    public class ApiResponseSortingViewModel : BindableBase {
        private readonly IApiSortingRepository _apiSortingRepository;
        private readonly IApiRuleRepository _apiRuleRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly IExcel _excel;
        private ObservableCollection<ApiSortingItemInfoModel> _apiSortingItems = new();
        private SnackbarMessageQueue _apiSortingMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public ApiResponseSortingViewModel(IApiSortingRepository apiSortingRepository,
            IApiRuleRepository apiRuleRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IExcel excel) {
            _apiSortingRepository = apiSortingRepository;
            _apiRuleRepository = apiRuleRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _excel = excel;
        }

        public ObservableCollection<ApiSortingItemInfoModel> ApiSortingItems {
            get => _apiSortingItems;
            set => SetProperty(ref _apiSortingItems, value);
        }

        public SnackbarMessageQueue ApiSortingMessageQueue {
            get => _apiSortingMessageQueue;
            set => SetProperty(ref _apiSortingMessageQueue, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var apiSortingRuleEditor = new ApiSortingRuleEditor();
                if (apiSortingRuleEditor.DataContext is ApiSortingRuleEditorViewModel model) {
                    model.Identifier = "ApiSortingDialog";
                    await DialogHost.Show(apiSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        ApiSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk) {
                        var apiSortingInfoModel = new ApiSortingInfoModel() {
                            CreateTime = model.ApiSortingItemInfo.CreateTime,
                            ModifyTime = model.ApiSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.ApiSortingItemInfo.Remarks,
                            SortingName = model.ApiSortingItemInfo.SortingName,
                        };
                        var insert = await _apiSortingRepository.Insert(apiSortingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(apiSortingInfoModel);
                            var sortingInfoModel = await _apiSortingRepository.FirstOrDefault(f =>
                                f.ModifyTime.Equals(model.ApiSortingItemInfo.ModifyTime) &&
                                f.ExitId.Equals(model.SelectPackageExitDefinitionInfo.Id));
                            var apiRuleInfoModels = model.ApiRuleItems.Select(s => new ApiRuleInfoModel() {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                ApiSortingId = sortingInfoModel.Id,
                                Remarks = s.Remarks,
                                JsonContent = s.JsonContent
                            })?.ToList() ?? new List<ApiRuleInfoModel>();
                            var ruleInfoModels = await _apiRuleRepository.Select(s =>
                                    s.ApiSortingId.Equals(sortingInfoModel.Id),
                                o => o.Id);
                            if (ruleInfoModels?.Any() == true) {
                                await _apiRuleRepository.DeleteRange(ruleInfoModels);
                            }
                            var insertRange = await _apiRuleRepository.InsertRange(apiRuleInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(ruleInfoModels);
                                ApiSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                ApiSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            ApiSortingMessageQueue.Enqueue("保存失败");
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
            get => new DelegateCommand<ApiSortingItemInfoModel>(ModifyDelegate);
        }

        private async void ModifyDelegate(ApiSortingItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var apiSortingRuleEditor = new ApiSortingRuleEditor();
                if (apiSortingRuleEditor.DataContext is ApiSortingRuleEditorViewModel model) {
                    model.Identifier = "ApiSortingDialog";
                    model.ApiSortingItemInfo = obj;
                    model.ApiRuleItems = obj.ApiRuleItems ?? new ObservableCollection<ApiRuleItemInfoModel>();
                    await DialogHost.Show(apiSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        ApiSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk) {
                        var apiSortingInfoModel = new ApiSortingInfoModel() {
                            CreateTime = model.ApiSortingItemInfo.CreateTime,
                            ModifyTime = model.ApiSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.ApiSortingItemInfo.Remarks,
                            SortingName = model.ApiSortingItemInfo.SortingName,
                            Id = model.ApiSortingItemInfo.Id,
                        };
                        var insert = await _apiSortingRepository.Update(apiSortingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(apiSortingInfoModel);

                            var apiRuleInfoModels = model.ApiRuleItems.Select(s => new ApiRuleInfoModel() {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                ApiSortingId = model.ApiSortingItemInfo.Id,
                                Remarks = s.Remarks,
                                JsonContent = s.JsonContent,
                            })?.ToList() ?? new List<ApiRuleInfoModel>();
                            var ruleInfoModels = await _apiRuleRepository.Select(s =>
                                    s.ApiSortingId.Equals(model.ApiSortingItemInfo.Id),
                                o => o.Id);
                            if (ruleInfoModels?.Any() == true) {
                                await _apiRuleRepository.DeleteRange(ruleInfoModels);
                            }
                            var insertRange = await _apiRuleRepository.InsertRange(apiRuleInfoModels);
                            if (insertRange) {
                                EventAggregator.Instance.Publish(ruleInfoModels);
                                ApiSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                ApiSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            ApiSortingMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            });
        }

        public ICommand DeleteCommand {
            get => new DelegateCommand<ApiSortingItemInfoModel>(DeleteDelegate);
        }

        private async void DeleteDelegate(ApiSortingItemInfoModel obj) {
            var apiSortingInfoModel = await _apiSortingRepository.
                FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (apiSortingInfoModel is not null) {
                var delete = await _apiSortingRepository.Delete(apiSortingInfoModel);
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
                model.Identifier = "ApiSortingDialog";
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _apiSortingRepository
                .ApiSortingItems(s => s.Id > 0);

            await Application.Current.Dispatcher.InvokeAsync(() => {
                try {
                    ApiSortingItems.Clear();
                    var infoModels = models?.Select((s, i) => new ApiSortingItemInfoModel() {
                        CreateTime = s.CreateTime,
                        Id = s.Id,
                        ModifyTime = s.ModifyTime,
                        Num = i + 1,
                        Remarks = s.Remarks,
                        ExitId = s.ExitId,
                        SortingName = s.SortingName,
                        ExitName =
                            packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ??
                            string.Empty,
                        ApiRuleItems = new ObservableCollection<ApiRuleItemInfoModel>(s.ApiRuleItems?.Select((s1, i1) =>
                            new ApiRuleItemInfoModel() {
                                CreateTime = s1.CreateTime,
                                Id = s1.Id,
                                ApiSortingId = s1.ApiSortingId,
                                ModifyTime = s1.ModifyTime,
                                Num = i1 + 1,
                                Remarks = s1.Remarks,
                                JsonContent = s1.JsonContent
                            }).ToList() ?? new List<ApiRuleItemInfoModel>()),
                        SortingRuleGroup = string.Join("\n",
                            s.ApiRuleItems?.Select(s2 => s2.JsonContent) ?? Array.Empty<string>())
                    })?.ToList();
                    ApiSortingItems.AddRange(infoModels);
                }
                catch (Exception e) {
                    ApiSortingMessageQueue.Enqueue(e.Message);
                }
                finally {
                    if (DialogHost.IsDialogOpen(model.Identifier)) {
                        DialogHost.Close(model.Identifier);
                    }
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
            if (ApiSortingItems?.Any() != true) {
                ApiSortingMessageQueue?.Enqueue(Languages.Language.ResourceManager.GetString("列表中没有数据") ?? string.Empty);
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
                    //先转换
                    try {
                        var result = ApiSortingItems
                            ?.SelectMany(s => s.ApiRuleItems?.Select(item => {
                                var ruleDto = JsonConvert.DeserializeObject<ApiRuleJsonDto>(item.JsonContent);

                                return new ExcelApiSortingItemInfoModel {
                                    CreateTime = s.CreateTime,
                                    ExitId = s.ExitId,
                                    ModifyTime = s.ModifyTime,
                                    Remarks = s.Remarks,
                                    ExitName = s.ExitName,
                                    SortingName = s.SortingName,
                                    Num = s.Num,
                                    Id = s.Id,
                                    IsUseJsonField = ruleDto?.IsUseJsonField ?? false,
                                    ResponseStatus = ruleDto?.ResponseStatus ?? UploadStatus.Succeeded,
                                    IsUseStringComparison = ruleDto?.IsUseStringComparison ?? false,
                                    SearchStringContent = ruleDto?.SearchStringContent ?? string.Empty,
                                    JsonField = ruleDto?.JsonField ?? string.Empty,
                                    JsonFieldValue = ruleDto?.JsonFieldValue ?? string.Empty
                                };
                            }) ?? Array.Empty<ExcelApiSortingItemInfoModel>())
                            ?.ToList() ?? new List<ExcelApiSortingItemInfoModel>();

                        var export = await _excel.Export(saveFileDialog.FileName,
                            $"Api分拣列表",
                            "Api分拣列表", result,
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
                                ApiSortingMessageQueue?.Enqueue(e.Message);
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
                        ApiSortingMessageQueue?.Enqueue(e.Message);
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

                    var models = await _excel.ReadExcel<ExcelApiSortingItemInfoModel>(openFileDialog.FileName, async p => {
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
                        ApiSortingMessageQueue?.Enqueue(e.Message);
                    });
                    await Task.Delay(500);
                    if (models?.Any() == true) {
                        try {
                            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                            o => o.CreateTime);
                            var dateTime = DateTime.Now;
                            var apiSortingInfoModels = models
                                .Select(s => new ApiSortingInfoModel() {
                                    CreateTime = dateTime,
                                    ExitId = packageExitDefinitionInfoModels.FirstOrDefault(f => f.ExitName.Equals(s.ExitName))?.Id ?? 0,
                                    ModifyTime = dateTime,
                                    SortingName = s.SortingName,
                                    Remarks = s.Remarks,
                                    ApiRuleItems = new List<ApiRuleInfoModel>
                                    {
                                        new()
                                        {
                                            CreateTime = dateTime,
                                            ModifyTime = dateTime,
                                            JsonContent = JsonConvert.SerializeObject(new ApiRuleJsonDto
                                            {
                                                IsUseJsonField = s.IsUseJsonField,
                                                IsUseStringComparison = s.IsUseStringComparison,
                                                IsUseStringSearch = !s.IsUseJsonField,
                                                JsonField = s.JsonField,
                                                JsonFieldValue = s.JsonFieldValue,
                                                ResponseStatus = s.ResponseStatus,
                                                SearchStringContent = s.SearchStringContent
                                            })
                                        }
                                    }
                                })
                                .GroupBy(s => s.ExitId)
                                .Select(group => new ApiSortingInfoModel() {
                                    CreateTime = group.First().CreateTime,
                                    ExitId = group.Key,
                                    SortingName = group.First().SortingName,
                                    ModifyTime = group.First().ModifyTime,
                                    Remarks = group.First().Remarks,
                                    ApiRuleItems = group.SelectMany(item => item.ApiRuleItems).ToList()
                                })
                                .ToList();


                            //批量添加
                            var range = await _apiSortingRepository.InsertRange(apiSortingInfoModels);
                            if (range) {
                                //取出数据库对应指令列表内容
                                var infoModels = await _apiSortingRepository.SelectOrderByDescending(
                                    s => s.CreateTime.Equals(dateTime),
                                    o => o.CreateTime);
                                foreach (var apiSorting in infoModels) {
                                    var apiRuleInfoModels = await _apiRuleRepository.Select(
                                        s => s.ApiSortingId.Equals(apiSorting.Id),
                                        o => o.Id);
                                    if (apiRuleInfoModels?.Any() == true) {
                                        await _apiRuleRepository.DeleteRange(apiRuleInfoModels);
                                    }

                                    var apiSortingInfoModel = apiSortingInfoModels?.FirstOrDefault(f =>
                                        f.ExitId.Equals(apiSorting.ExitId) &&
                                        f.CreateTime.Equals(dateTime));
                                    if (apiSortingInfoModel is not null) {
                                        var ruleInfoModels = apiSortingInfoModel?.ApiRuleItems?.Select(s =>
                                            new ApiRuleInfoModel() {
                                                ApiSortingId = apiSorting.Id,
                                                JsonContent = s.JsonContent,

                                            })?.ToList();
                                        await _apiRuleRepository.InsertRange(ruleInfoModels ?? new List<ApiRuleInfoModel>());
                                    }
                                }

                                ApiSortingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                ApiSortingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        catch (Exception e) {

                            ApiSortingMessageQueue.Enqueue(e.Message);
                        }

                    }
                }
            }
        }
    }
}