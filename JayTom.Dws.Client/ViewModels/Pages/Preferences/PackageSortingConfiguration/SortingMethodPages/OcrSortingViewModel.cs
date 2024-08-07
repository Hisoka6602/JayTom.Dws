using System;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;
using JayTom.Dws.Plugin;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Client.Models.PackageSorting.Excel;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages {

    public class OcrSortingViewModel : BulkOperationsTemplateViewModel<ExcelOcrSortingItemInfoModel> {
        private readonly IOcrSortingRepository _ocrSortingRepository;
        private readonly IOcrRuleRepository _ocrRuleRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private ObservableCollection<OcrSortingItemInfoModel> _ocrSortingItems = new();
        private bool _isLoaded;

        public OcrSortingViewModel(IOcrSortingRepository ocrSortingRepository,
            IOcrRuleRepository ocrRuleRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IExcel excel) : base(excel) {
            _ocrSortingRepository = ocrSortingRepository;
            _ocrRuleRepository = ocrRuleRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
        }

        public ObservableCollection<OcrSortingItemInfoModel> OcrSortingItems {
            get => _ocrSortingItems;
            set => SetProperty(ref _ocrSortingItems, value);
        }

        protected override async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var ocrSortingRuleEditor = new OcrSortingRuleEditor();
                if (ocrSortingRuleEditor.DataContext is OcrSortingRuleEditorViewModel model) {
                    model.Identifier = Identifier;
                    await DialogHost.Show(ocrSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        MessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk) {
                        var ocrSortingInfoModel = new OcrSortingInfoModel() {
                            CreateTime = model.OcrSortingItemInfo.CreateTime,
                            ModifyTime = model.OcrSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.OcrSortingItemInfo.Remarks,
                            SortingName = model.OcrSortingItemInfo.SortingName,
                            OcrRuleItems = model.OcrRuleItems.Select(s => new OcrRuleInfoModel() {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                                JsonContent = s.JsonContent
                            })?.ToList()
                        };
                        var insert = await _ocrSortingRepository.InsertDetailAsync(ocrSortingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(ocrSortingInfoModel);
                            EventAggregator.Instance.Publish(new SettingsChangedEvent {
                                SettingsName = SettingsName,
                                IsLocallySaved = true
                            });
                            MessageQueue.Enqueue("保存成功");
                        }
                        else {
                            MessageQueue.Enqueue("保存失败");
                        }
                    }
                    RefreshData();
                }
            });
        }

        public override string Identifier => "SortingMethodDialog";
        public override string ExcelTitle => "Ocr分拣规则列表";
        public override string SheetName => "Ocr分拣规则列表";

        public override string SettingsName => "OcrSortingItemsSettings";

        public override void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                RefreshData();
            }
        }

        protected override async Task<bool> DeleteProcess(object obj) {
            if (obj is OcrSortingItemInfoModel item) {
                var ocrSortingInfoModel = await _ocrSortingRepository.
                    FirstOrDefault(f => f.Id.Equals(item.Id));
                if (ocrSortingInfoModel is not null) {
                    return await _ocrSortingRepository.Delete(ocrSortingInfoModel);
                }
            }

            return false;
        }

        protected override async void ModifyDelegate(object obj) {
            if (obj is OcrSortingItemInfoModel item) {
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    var ocrSortingRuleEditor = new OcrSortingRuleEditor();
                    if (ocrSortingRuleEditor.DataContext is OcrSortingRuleEditorViewModel model) {
                        model.Identifier = Identifier;
                        model.OcrSortingItemInfo = item;
                        model.OcrRuleItems = item.OcrRuleItems ?? new ObservableCollection<OcrRuleItemInfoModel>();
                        await DialogHost.Show(ocrSortingRuleEditor, model.Identifier);
                        if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                            MessageQueue.Enqueue(model.ExceptionContent);
                            RefreshData();
                            return;
                        }

                        if (model.IsOk) {
                            var ocrSortingInfoModel = new OcrSortingInfoModel() {
                                CreateTime = model.OcrSortingItemInfo.CreateTime,
                                ModifyTime = model.OcrSortingItemInfo.ModifyTime,
                                ExitId = model.SelectPackageExitDefinitionInfo.Id,
                                Remarks = model.OcrSortingItemInfo.Remarks,
                                SortingName = model.OcrSortingItemInfo.SortingName,
                                Id = model.OcrSortingItemInfo.Id,
                                OcrRuleItems = model.OcrRuleItems.Select(s => new OcrRuleInfoModel() {
                                    CreateTime = s.CreateTime,
                                    ModifyTime = s.ModifyTime,
                                    OcrSortingId = model.OcrSortingItemInfo.Id,
                                    Remarks = s.Remarks,
                                    JsonContent = s.JsonContent
                                })?.ToList()
                            };
                            var insert = await _ocrSortingRepository.UpdateDetailAsync(ocrSortingInfoModel);
                            if (insert) {
                                EventAggregator.Instance.Publish(ocrSortingInfoModel);
                                EventAggregator.Instance.Publish(new SettingsChangedEvent {
                                    SettingsName = SettingsName,
                                    IsLocallySaved = true
                                });
                                MessageQueue.Enqueue("保存成功");
                            }
                            else {
                                MessageQueue.Enqueue("保存失败");
                            }
                        }
                        RefreshData();
                    }
                });
            }
        }

        protected override async Task<bool> ImportProcess(List<ExcelOcrSortingItemInfoModel> items) {
            if (items?.Any() == true) {
                var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                    o => o.CreateTime);
                var dateTime = DateTime.Now;
                var ocrSortingInfoModels = items
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
                                JsonContent = JsonConvert.SerializeObject(new OcrRuleJsonDto
                                {
                                    IsUseSenderAddressValidation = s.IsUseSenderAddressValidation,
                                    IsUseRecipientAddressValidation = s.IsUseRecipientAddressValidation,
                                    IsUseSenderPhoneNumberValidation = s.IsUseSenderPhoneNumberValidation,
                                    IsUseThreeSegmentCodeValidation = s.IsUseThreeSegmentCodeValidation,
                                    RecipientAddressContainsChars = s.RecipientAddressContainsChars,
                                    SenderAddressContainsChars = s.SenderAddressContainsChars,
                                    SenderPhoneNumberEndsWith = s.SenderPhoneNumberEndsWith,
                                    ThreeSegmentCodeContainsChars = s.ThreeSegmentCodeContainsChars
                                }),
                            }
                        }
                    })
                    .GroupBy(s => s.SortingName)
                    .Select(group => new OcrSortingInfoModel {
                        CreateTime = group.First().CreateTime,
                        ExitId = group.First().ExitId,
                        SortingName = group.Key,
                        ModifyTime = group.First().ModifyTime,
                        Remarks = group.First().Remarks,
                        OcrRuleItems = group.SelectMany(item => item.OcrRuleItems).ToList()
                    })
                    .ToList();

                //批量添加
                return await _ocrSortingRepository.InsertRangeDetailAsync(ocrSortingInfoModels);
            }

            return false;
        }

        protected override async Task ClearProcess() {
            var ocrSortingInfoModels = await _ocrSortingRepository.Select(s => s.Id > 0, o => o.Id);
            await _ocrSortingRepository.DeleteRange(ocrSortingInfoModels);
        }

        protected override async Task RefreshDataProcess() {
            await Task.Delay(300);
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _ocrSortingRepository
                .OcrSortingItems(s => s.Id > 0);
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
                    JsonContent = s1.JsonContent,
                    FormatJsonContent = FormatRule(s1.JsonContent)
                }).ToList() ?? new List<OcrRuleItemInfoModel>()),
                SortingRuleGroup = string.Join("\n", s.OcrRuleItems?.Select(s2 => FormatRule(s2.JsonContent)) ?? Array.Empty<string>())
            })?.ToList();
            OcrSortingItems.AddRange(infoModels);
        }

        protected override bool IsSelectAnyItem() => OcrSortingItems.Any(a => a.IsSelect);

        protected override async Task BulkDeleteProcess() {
            var selectIds = OcrSortingItems.Where(w => w.IsSelect).Select(s => s.Id)
                .ToList();
            var ocrSortingInfoModels = await _ocrSortingRepository.Select(w => selectIds.Contains(w.Id),
                o => o.Id);

            await _ocrSortingRepository.DeleteRange(ocrSortingInfoModels);
        }

        protected override List<ExcelOcrSortingItemInfoModel> ExportProcess() {
            return OcrSortingItems
                ?.SelectMany(s => s.OcrRuleItems?
                    .Select(item => {
                        var ruleDto = JsonConvert.DeserializeObject<OcrRuleJsonDto>(item.JsonContent);
                        return new ExcelOcrSortingItemInfoModel() {
                            CreateTime = s.CreateTime,
                            ExitId = s.ExitId,
                            ModifyTime = s.ModifyTime,
                            Remarks = s.Remarks,
                            ExitName = s.ExitName,
                            SortingName = s.SortingName,
                            Num = s.Num,
                            Id = s.Id,
                            IsUseRecipientAddressValidation = ruleDto?.IsUseRecipientAddressValidation ?? false,
                            IsUseThreeSegmentCodeValidation = ruleDto?.IsUseThreeSegmentCodeValidation ?? false,
                            IsUseSenderAddressValidation = ruleDto?.IsUseSenderAddressValidation ?? false,
                            IsUseSenderPhoneNumberValidation = ruleDto?.IsUseSenderPhoneNumberValidation ?? false,
                            RecipientAddressContainsChars = ruleDto?.RecipientAddressContainsChars ?? string.Empty,
                            SenderAddressContainsChars = ruleDto?.SenderAddressContainsChars ?? string.Empty,
                            SenderPhoneNumberEndsWith = ruleDto?.SenderPhoneNumberEndsWith ?? string.Empty,
                            ThreeSegmentCodeContainsChars = ruleDto?.ThreeSegmentCodeContainsChars ?? string.Empty
                        };
                    }) ?? Array.Empty<ExcelOcrSortingItemInfoModel>())
                ?.ToList() ?? new List<ExcelOcrSortingItemInfoModel>();
        }

        public string FormatRule(string jsonContent) {
            var content = string.Empty;
            try {
                var ocrRuleJsonDto = JsonConvert.DeserializeObject<OcrRuleJsonDto>(jsonContent);
                if (ocrRuleJsonDto is not null) {
                    if (ocrRuleJsonDto.IsUseThreeSegmentCodeValidation) {
                        content += $"三段码包含:[{ocrRuleJsonDto.ThreeSegmentCodeContainsChars}]  ";
                    }
                    if (ocrRuleJsonDto.IsUseRecipientAddressValidation) {
                        content += $"收件人地址包含:[{ocrRuleJsonDto.RecipientAddressContainsChars}]  ";
                    }
                    if (ocrRuleJsonDto.IsUseSenderAddressValidation) {
                        content += $"发件人地址包含:[{ocrRuleJsonDto.SenderAddressContainsChars}]  ";
                    }
                    if (ocrRuleJsonDto.IsUseSenderPhoneNumberValidation) {
                        content += $"发件人手机尾号包含:[{ocrRuleJsonDto.SenderPhoneNumberEndsWith}]  ";
                    }

                    return content;
                }
            }
            catch (Exception e) {
                return "解析错误";
            }
            return "解析错误";
        }
    }
}