using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using JayTom.Dws.Plugin;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Client.Models.PackageSorting.Excel;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages {
    public class ApiResponseSortingViewModel : BulkOperationsTemplateViewModel<ExcelApiSortingItemInfoModel> {
        private readonly IApiSortingRepository _apiSortingRepository;
        private readonly IApiRuleRepository _apiRuleRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private ObservableCollection<ApiSortingItemInfoModel> _apiSortingItems = new();
        private bool _isLoaded;

        public ApiResponseSortingViewModel(IApiSortingRepository apiSortingRepository,
            IApiRuleRepository apiRuleRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IExcel excel) : base(excel) {
            _apiSortingRepository = apiSortingRepository;
            _apiRuleRepository = apiRuleRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
        }

        public ObservableCollection<ApiSortingItemInfoModel> ApiSortingItems {
            get => _apiSortingItems;
            set => SetProperty(ref _apiSortingItems, value);
        }

        protected override async void AddDelegate(object obj) {
            var apiSortingRuleEditor = new ApiSortingRuleEditor();
            if (apiSortingRuleEditor.DataContext is ApiSortingRuleEditorViewModel model) {
                model.Identifier = Identifier;
                await DialogHost.Show(apiSortingRuleEditor, model.Identifier);
                if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                    base.MessageQueue.Enqueue(model.ExceptionContent);
                    return;
                }
                if (model.IsOk) {
                    await Application.Current.Dispatcher.InvokeAsync(async () => {
                        var apiSortingInfoModel = new ApiSortingInfoModel() {
                            CreateTime = model.ApiSortingItemInfo.CreateTime,
                            ModifyTime = model.ApiSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.ApiSortingItemInfo.Remarks,
                            SortingName = model.ApiSortingItemInfo.SortingName,
                            ApiRuleItems = model.ApiRuleItems.Select(s => new ApiRuleInfoModel() {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                                JsonContent = s.JsonContent,
                            })?.ToList()
                        };
                        var insert = await _apiSortingRepository.InsertDetailAsync(apiSortingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(apiSortingInfoModel);
                            base.MessageQueue.Enqueue("保存成功");
                        }
                        else {
                            base.MessageQueue.Enqueue("保存失败");
                        }
                    });
                    base.RefreshData();
                }
            }
        }

        public override string Identifier => "SortingMethodDialog";

        public override string ExcelTitle => "Api分拣列表";
        public override string SheetName => "Api分拣列表";

        public override void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                RefreshData();
            }
        }

        protected override async void ModifyDelegate(object obj) {
            if (obj is ApiSortingItemInfoModel item) {
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    var apiSortingRuleEditor = new ApiSortingRuleEditor();
                    if (apiSortingRuleEditor.DataContext is ApiSortingRuleEditorViewModel model) {
                        model.Identifier = Identifier;
                        model.ApiSortingItemInfo = item;
                        model.ApiRuleItems = item.ApiRuleItems ?? new ObservableCollection<ApiRuleItemInfoModel>();
                        await DialogHost.Show(apiSortingRuleEditor, model.Identifier);
                        if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                            base.MessageQueue.Enqueue(model.ExceptionContent);
                            RefreshData();
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
                                ApiRuleItems = model.ApiRuleItems.Select(s => new ApiRuleInfoModel() {
                                    CreateTime = s.CreateTime,
                                    ModifyTime = s.ModifyTime,
                                    ApiSortingId = model.ApiSortingItemInfo.Id,
                                    Remarks = s.Remarks,
                                    JsonContent = s.JsonContent,
                                })?.ToList()
                            };
                            var insert = await _apiSortingRepository.UpdateDetailAsync(apiSortingInfoModel);
                            if (insert) {
                                EventAggregator.Instance.Publish(apiSortingInfoModel);
                                base.MessageQueue.Enqueue("保存成功");
                            }
                            else {
                                base.MessageQueue.Enqueue("保存失败");
                            }
                        }
                        RefreshData();
                    }
                });
            }
        }

        protected override async Task<bool> DeleteProcess(object obj) {
            if (obj is ApiSortingItemInfoModel item) {
                var apiSortingInfoModel = await _apiSortingRepository.
                    FirstOrDefault(f => f.Id.Equals(item.Id));
                if (apiSortingInfoModel is not null) {
                    return await _apiSortingRepository.Delete(apiSortingInfoModel);
                }
            }

            return false;
        }

        protected override async Task ClearProcess() {
            var apiSortingInfoModels = await _apiSortingRepository.Select(f => f.Id > 0, o => o.Id);

            await _apiSortingRepository.DeleteRange(apiSortingInfoModels);
        }

        protected override async Task RefreshDataProcess() {
            await Task.Delay(300);
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _apiSortingRepository
                .ApiSortingItems(s => s.Id > 0);
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
                        JsonContent = s1.JsonContent,
                        FormatJsonContent = FormatRule(s1.JsonContent)
                    }).ToList() ?? new List<ApiRuleItemInfoModel>()),
                SortingRuleGroup = string.Join("\n",
                    s.ApiRuleItems?.Select(s2 => FormatRule(s2.JsonContent)) ?? Array.Empty<string>())
            })?.ToList();
            ApiSortingItems.AddRange(infoModels);
        }

        public string FormatRule(string jsonContent) {
            try {
                var apiRuleJsonDto = JsonConvert.DeserializeObject<ApiRuleJsonDto>(jsonContent);
                if (apiRuleJsonDto is not null) {
                    var status = apiRuleJsonDto.ResponseStatus switch {
                        UploadStatus.Failed => "失败",
                        UploadStatus.NotUploaded => "未上传",
                        UploadStatus.Succeeded => "成功",
                        _ => "未知"
                    };
                    var content = string.Empty;
                    if (!apiRuleJsonDto.IsUseStringComparison) return $"响应状态:{status} {content}";
                    if (apiRuleJsonDto.IsUseStringSearch) {
                        content += $"字符串查找:[{apiRuleJsonDto.SearchStringContent}]";
                    }
                    else if (apiRuleJsonDto.IsUseJsonField) {
                        content += $"Json字段:[{apiRuleJsonDto.JsonField}]  值:[{apiRuleJsonDto.JsonFieldValue}]";
                    }
                    return $"响应状态:{status} {content}";
                }
            }
            catch (Exception) {
                return "解析错误";
            }
            return "解析错误";
        }

        protected override bool IsSelectAnyItem() => ApiSortingItems.Any(a => a.IsSelect);

        protected override List<ExcelApiSortingItemInfoModel> ExportProcess() {
            return ApiSortingItems
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
        }

        protected override async Task<bool> ImportProcess(List<ExcelApiSortingItemInfoModel> items) {
            if (items?.Any() == true) {
                try {
                    var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                    o => o.CreateTime);
                    var dateTime = DateTime.Now;
                    var apiSortingInfoModels = items
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
                        .GroupBy(s => s.SortingName) // 根据 SortingName 进行分组
                        .Select(group => new ApiSortingInfoModel() {
                            CreateTime = group.First().CreateTime,
                            ExitId = group.First().ExitId,
                            SortingName = group.Key,
                            ModifyTime = group.First().ModifyTime,
                            Remarks = group.First().Remarks,
                            ApiRuleItems = group.SelectMany(item => item.ApiRuleItems ?? new List<ApiRuleInfoModel>()).ToList()
                        })
                        .ToList();
                    //批量添加
                    return await _apiSortingRepository.InsertRangeDetailAsync(apiSortingInfoModels);
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }

            return false;
        }

        //批量删除
        protected override async Task BulkDeleteProcess() {
            var selectIds = ApiSortingItems.
                Where(w => w.IsSelect).Select(s => s.Id).ToList();
            var apiSortingInfoModels = await _apiSortingRepository.Select(w => selectIds.Contains(w.Id), o => o.Id);
            await _apiSortingRepository.DeleteRange(apiSortingInfoModels);
        }
    }
}