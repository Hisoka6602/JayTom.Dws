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
using JayTom.Dws.Models.LocalData;
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

    public class WeightSortingViewModel : BulkOperationsTemplateViewModel<WeightSortingItemInfoModel>
    {
        private readonly ISortingConfigurationCatalog<WeightSortingInfoModel> _sortingCatalog;
        private readonly IPackageExitCatalog _packageExitCatalog;
        private bool _isLoaded;
        private ObservableCollection<WeightSortingItemInfoModel> _weightSortingItems = new();

        public WeightSortingViewModel(ISortingConfigurationCatalog<WeightSortingInfoModel> sortingCatalog,
            IPackageExitCatalog packageExitCatalog,
            IExcel excel, IEventBus eventBus) : base(eventBus, excel)
        {
            _sortingCatalog = sortingCatalog;
            _packageExitCatalog = packageExitCatalog;
        }

        public ObservableCollection<WeightSortingItemInfoModel> WeightSortingItems
        {
            get => _weightSortingItems;
            set => SetProperty(ref _weightSortingItems, value);
        }

        protected override async void AddDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var weightSortingRuleEditor = new WeightSortingRuleEditor();
                if (weightSortingRuleEditor.DataContext is WeightSortingRuleEditorViewModel model)
                {
                    model.Identifier = Identifier;
                    await DialogHost.Show(weightSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent))
                    {
                        MessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk)
                    {
                        //添加到数据库
                        var weightSortingInfoModel = new WeightSortingInfoModel()
                        {
                            CreateTime = model.WeightSortingItemInfo.CreateTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            ModifyTime = model.WeightSortingItemInfo.ModifyTime,
                            Remarks = model.WeightSortingItemInfo.Remarks,
                            SortingName = model.WeightSortingItemInfo.SortingName,
                            WeightRuleItems = model.WeightRuleItems.Select(s => new WeightRuleInfoModel
                            {
                                CreateTime = s.CreateTime,
                                Formula = s.Formula,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                            })?.ToList()
                        };
                        var insert = await _sortingCatalog.AddAsync(weightSortingInfoModel);
                        if (insert)
                        {
                            _eventBus.Publish(weightSortingInfoModel);
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
        public override string ExcelTitle => "重量分拣列表";
        public override string SheetName => "重量分拣列表";

        public override string SettingsName => "WeightSortingItemsSettings";

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
            if (obj is WeightSortingItemInfoModel item)
            {
                return await _sortingCatalog.DeleteByIdAsync(item.Id);
            }

            return false;
        }

        protected override async void ModifyDelegate(object obj)
        {
            if (obj is WeightSortingItemInfoModel item)
            {
                await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var weightSortingRuleEditor = new WeightSortingRuleEditor();
                    if (weightSortingRuleEditor.DataContext is WeightSortingRuleEditorViewModel model)
                    {
                        model.Identifier = Identifier;
                        model.WeightSortingItemInfo = item;
                        model.WeightRuleItems = item.WeightRuleItems;
                        await DialogHost.Show(weightSortingRuleEditor, model.Identifier);
                        if (!string.IsNullOrEmpty(model.ExceptionContent))
                        {
                            MessageQueue.Enqueue(model.ExceptionContent);
                            RefreshData();
                            return;
                        }
                        if (model.IsOk)
                        {
                            //添加到数据库
                            var weightSortingInfoModel = new WeightSortingInfoModel()
                            {
                                CreateTime = model.WeightSortingItemInfo.CreateTime,
                                ExitId = model.SelectPackageExitDefinitionInfo.Id,
                                ModifyTime = model.WeightSortingItemInfo.ModifyTime,
                                Remarks = model.WeightSortingItemInfo.Remarks,
                                SortingName = model.WeightSortingItemInfo.SortingName,
                                Id = model.WeightSortingItemInfo.Id,
                                WeightRuleItems = model.WeightRuleItems.Select(s => new WeightRuleInfoModel
                                {
                                    CreateTime = s.CreateTime,
                                    Formula = s.Formula,
                                    ModifyTime = s.ModifyTime,
                                    Remarks = s.Remarks,
                                    WeightSortingId = model.WeightSortingItemInfo.Id,
                                })?.ToList()
                            };
                            var insert = await _sortingCatalog.UpdateAsync(weightSortingInfoModel);
                            if (insert)
                            {
                                _eventBus.Publish(weightSortingInfoModel);
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
            WeightSortingItems.Clear();
            var infoModels = models?.Select((s, i) => new WeightSortingItemInfoModel()
            {
                CreateTime = s.CreateTime,
                Id = s.Id,
                ModifyTime = s.ModifyTime,
                Num = i + 1,
                Remarks = s.Remarks,
                ExitId = s.ExitId,
                SortingName = s.SortingName,
                ExitName = packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ?? string.Empty,
                WeightRuleItems = new ObservableCollection<WeightRuleItemInfoModel>(s.WeightRuleItems?.Select((s1, i1) => new WeightRuleItemInfoModel()
                {
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
        }

        protected override bool IsSelectAnyItem() => WeightSortingItems.Any(a => a.IsSelect);

        protected override async Task BulkDeleteProcess()
        {
            var selectIds = WeightSortingItems.Where(w => w.IsSelect)
                .Select(s => s.Id)
                .ToList();
            await _sortingCatalog.DeleteByIdsAsync(selectIds);
        }

        protected override List<WeightSortingItemInfoModel> ExportProcess()
        {
            return WeightSortingItems
                ?.SelectMany(s => s.SortingRuleGroup.Split("\n")
                    .Select(item => new WeightSortingItemInfoModel
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
                ?.ToList() ?? new List<WeightSortingItemInfoModel>();
        }

        protected override async Task<bool> ImportProcess(List<WeightSortingItemInfoModel> items)
        {
            if (items?.Any() == true)
            {
                var packageExitDefinitionInfoModels = await _packageExitCatalog.ListAsync();
                var dateTime = DateTime.Now;
                var weightSortingInfoModels = items
                    .Select(s => new WeightSortingInfoModel()
                    {
                        CreateTime = dateTime,
                        ExitId = packageExitDefinitionInfoModels.FirstOrDefault(f => f.ExitName.Equals(s.ExitName))?.Id ?? 0,
                        ModifyTime = dateTime,
                        SortingName = s.SortingName,
                        Remarks = s.Remarks,
                        WeightRuleItems = new List<WeightRuleInfoModel>
                        {
                            new()
                            {
                                CreateTime = dateTime,
                                ModifyTime = dateTime,
                                Formula = s.SortingRuleGroup
                            }
                        }
                    })
                    .GroupBy(s => s.SortingName)
                    .Select(group => new WeightSortingInfoModel
                    {
                        CreateTime = group.First().CreateTime,
                        ExitId = group.First().ExitId,
                        SortingName = group.Key,
                        ModifyTime = group.First().ModifyTime,
                        Remarks = group.First().Remarks,
                        WeightRuleItems = [.. group.SelectMany(item => item.WeightRuleItems)]
                    })
                    .ToList();

                //批量添加
                return await _sortingCatalog.AddRangeAsync(weightSortingInfoModels);
            }

            return false;
        }
    }
}
