using System;
using JayTom.Dws.Application.PackageExits;
using JayTom.Dws.Application.SortingConfigurations;
using JayTom.Dws.Application.Messaging;
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
using JayTom.Dws.Application.Events;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.RuleConfig;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages
{

    public class BarcodeSortingViewModel : BulkOperationsTemplateViewModel<BarCodeSortingItemInfoModel>
    {
        private readonly ISortingConfigurationCatalog<BarCodeSortingInfoModel> _sortingCatalog;
        private readonly IPackageExitCatalog _packageExitCatalog;
        private ObservableCollection<BarCodeSortingItemInfoModel> _barCodeSortingItems = new();
        private bool _isLoaded;

        public BarcodeSortingViewModel(ISortingConfigurationCatalog<BarCodeSortingInfoModel> sortingCatalog,
            IPackageExitCatalog packageExitCatalog,
            IExcel excel, IEventBus eventBus) : base(eventBus, excel)
        {
            _sortingCatalog = sortingCatalog;
            _packageExitCatalog = packageExitCatalog;
        }

        public ObservableCollection<BarCodeSortingItemInfoModel> BarCodeSortingItems
        {
            get => _barCodeSortingItems;
            set => SetProperty(ref _barCodeSortingItems, value);
        }

        protected override async void AddDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var barcodeSortingRuleEditor = new BarcodeSortingRuleEditor();
                if (barcodeSortingRuleEditor.DataContext is BarcodeSortingRuleEditorViewModel model)
                {
                    model.Identifier = Identifier;
                    await DialogHost.Show(barcodeSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent))
                    {
                        MessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk)
                    {
                        //添加到数据库
                        var infoModel = new BarCodeSortingInfoModel()
                        {
                            CreateTime = model.BarCodeSortingItemInfo.CreateTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            ModifyTime = model.BarCodeSortingItemInfo.ModifyTime,
                            Remarks = model.BarCodeSortingItemInfo.Remarks,
                            SortingName = model.BarCodeSortingItemInfo.SortingName,
                            BarCodeRegexItems = model.BarCodeRegexItems.Select(s => new BarCodeRegexInfoModel()
                            {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                                RegexPattern = s.RegexPattern
                            })?.ToList()
                        };
                        var insertOrUpdate = await _sortingCatalog.AddAsync(infoModel);
                        if (insertOrUpdate)
                        {
                            _eventBus.Publish(infoModel);
                            _eventBus.Publish(new SettingsChangedEvent
                            {
                                SettingsName = SettingsName,
                                IsLocallySaved = true
                            });
                            MessageQueue.Enqueue("保存成功");
                        }
                        else
                        {
                            MessageQueue.Enqueue("保存失败");
                        }
                    }
                    RefreshData();
                }
            });
        }

        public override string Identifier => "SortingMethodDialog";
        public override string ExcelTitle => "条码分拣列表";
        public override string SheetName => "条码分拣列表";

        public override string SettingsName => "BarcodeSortingItemsSettings";

        public override void LoadedDelegate(object obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                RefreshData();
            }
        }

        protected override async Task<bool> DeleteProcess(object obj)
        {
            if (obj is BarCodeSortingItemInfoModel item)
            {
                return await _sortingCatalog.DeleteByIdAsync(item.Id);
            }

            return false;
        }

        protected override async void ModifyDelegate(object obj)
        {
            if (obj is BarCodeSortingItemInfoModel item)
            {
                await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var barcodeSortingRuleEditor = new BarcodeSortingRuleEditor();
                    if (barcodeSortingRuleEditor.DataContext is BarcodeSortingRuleEditorViewModel model)
                    {
                        model.Identifier = Identifier;
                        model.BarCodeSortingItemInfo = item;
                        model.BarCodeRegexItems = item.BarCodeRegexItems;
                        await DialogHost.Show(barcodeSortingRuleEditor, model.Identifier);
                        if (!string.IsNullOrEmpty(model.ExceptionContent))
                        {
                            MessageQueue.Enqueue(model.ExceptionContent);
                            RefreshData();
                            return;
                        }
                        if (model.IsOk)
                        {
                            //添加到数据库
                            var infoModel = new BarCodeSortingInfoModel()
                            {
                                CreateTime = model.BarCodeSortingItemInfo.CreateTime,
                                ExitId = model.SelectPackageExitDefinitionInfo.Id,
                                ModifyTime = model.BarCodeSortingItemInfo.ModifyTime,
                                Remarks = model.BarCodeSortingItemInfo.Remarks,
                                SortingName = model.BarCodeSortingItemInfo.SortingName,
                                Id = model.BarCodeSortingItemInfo.Id,
                                BarCodeRegexItems = model.BarCodeRegexItems.Select(s => new BarCodeRegexInfoModel
                                {
                                    CreateTime = s.CreateTime,
                                    ModifyTime = s.ModifyTime,
                                    Remarks = s.Remarks,
                                    RegexPattern = s.RegexPattern,
                                })?.ToList()
                            };
                            var insertOrUpdate = await _sortingCatalog.UpdateAsync(infoModel);
                            if (insertOrUpdate)
                            {
                                _eventBus.Publish(infoModel);
                                _eventBus.Publish(new SettingsChangedEvent
                                {
                                    SettingsName = SettingsName,
                                    IsLocallySaved = true
                                });
                                MessageQueue.Enqueue("保存成功");
                            }
                            else
                            {
                                MessageQueue.Enqueue("保存失败");
                            }
                        }
                        RefreshData();
                    }
                });
            }
        }

        protected override async Task ClearProcess()
        {
            await _sortingCatalog.DeleteAllAsync();
        }

        protected override async Task RefreshDataProcess()
        {
            await Task.Delay(300);
            var packageExitDefinitionInfoModels = await _packageExitCatalog.ListAsync();
            var models = await _sortingCatalog.ListAsync();
            BarCodeSortingItems.Clear();
            var infoModels = models?.Select((s, i) => new BarCodeSortingItemInfoModel
            {
                CreateTime = s.CreateTime,
                Id = s.Id,
                ModifyTime = s.ModifyTime,
                Num = i + 1,
                Remarks = s.Remarks,
                SortingName = s.SortingName,
                ExitId = s.ExitId,
                ExitName = packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ?? string.Empty,
                BarCodeRegexItems = new ObservableCollection<BarCodeRegexItemInfoModel>(s.BarCodeRegexItems?.Select((s1, i1) => new BarCodeRegexItemInfoModel
                {
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
        }

        protected override bool IsSelectAnyItem() => BarCodeSortingItems.Any(a => a.IsSelect);

        protected override async Task BulkDeleteProcess()
        {
            var selectIds = BarCodeSortingItems.Where(w => w.IsSelect)
                .Select(s => s.Id).ToList();
            await _sortingCatalog.DeleteByIdsAsync(selectIds);
        }

        protected override List<BarCodeSortingItemInfoModel> ExportProcess()
        {
            return BarCodeSortingItems
                ?.SelectMany(s => s.SortingRuleGroup.Split("\n")
                    .Select(item => new BarCodeSortingItemInfoModel
                    {
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
                ?.ToList() ?? new List<BarCodeSortingItemInfoModel>();
        }

        protected override async Task<bool> ImportProcess(List<BarCodeSortingItemInfoModel> items)
        {
            if (items?.Any() == true)
            {
                var packageExitDefinitionInfoModels = await _packageExitCatalog.ListAsync();
                var dateTime = DateTime.Now;
                var barCodeSortingInfoModels = items
                    .Select(s => new BarCodeSortingInfoModel()
                    {
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
                    .GroupBy(s => s.SortingName)
                    .Select(group => new BarCodeSortingInfoModel
                    {
                        CreateTime = group.First().CreateTime,
                        ExitId = group.First().ExitId,
                        SortingName = group.Key,
                        ModifyTime = group.First().ModifyTime,
                        Remarks = group.First().Remarks,
                        BarCodeRegexItems = [.. group.SelectMany(item => item.BarCodeRegexItems)]
                    })
                    .ToList();

                //批量添加
                return await _sortingCatalog.AddRangeAsync(barCodeSortingInfoModels);
            }

            return false;
        }
    }
}
