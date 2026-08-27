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
using JayTom.Dws.Client.Models.PackageSorting.Excel;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.RuleConfig;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages
{

    public class LogisticsSortingViewModel : BulkOperationsTemplateViewModel<LogisticsSortingItemInfoModel>
    {
        private readonly ISortingConfigurationCatalog<LogisticsSortingInfoModel> _sortingCatalog;
        private readonly IPackageExitCatalog _packageExitCatalog;
        private readonly ISortingConfigurationCatalog<LogisticsCodeRecognitionInfoModel> _logisticsCatalog;
        private bool _isLoaded;
        private ObservableCollection<LogisticsSortingItemInfoModel> _logisticsSortingItems = new();

        public LogisticsSortingViewModel(ISortingConfigurationCatalog<LogisticsSortingInfoModel> sortingCatalog,
            IPackageExitCatalog packageExitCatalog,
            ISortingConfigurationCatalog<LogisticsCodeRecognitionInfoModel> logisticsCatalog,
            IExcel excel, IEventBus eventBus) : base(eventBus, excel)
        {
            _sortingCatalog = sortingCatalog;
            _packageExitCatalog = packageExitCatalog;
            _logisticsCatalog = logisticsCatalog;
        }

        public ObservableCollection<LogisticsSortingItemInfoModel> LogisticsSortingItems
        {
            get => _logisticsSortingItems;
            set => SetProperty(ref _logisticsSortingItems, value);
        }

        protected override async void AddDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var logisticsSortingRuleEditor = new LogisticsSortingRuleEditor();
                if (logisticsSortingRuleEditor.DataContext is LogisticsSortingRuleEditorViewModel model)
                {
                    model.Identifier = Identifier;
                    await DialogHost.Show(logisticsSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent))
                    {
                        MessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk)
                    {
                        //添加到数据库
                        var logisticsSortingInfoModel = new LogisticsSortingInfoModel()
                        {
                            CreateTime = model.LogisticsSortingItemInfo.CreateTime,
                            ModifyTime = model.LogisticsSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.LogisticsSortingItemInfo.Remarks,
                            SortingName = model.LogisticsSortingItemInfo.SortingName,
                            LogisticsRuleItems = model.LogisticsRuleItems.Select(s => new LogisticsRuleInfoModel()
                            {
                                CreateTime = s.CreateTime,
                                ModifyTime = s.ModifyTime,
                                LogisticsId = s.LogisticsId,
                                Remarks = s.Remarks,
                            })?.ToList()
                        };
                        var insert = await _sortingCatalog.AddAsync(logisticsSortingInfoModel);
                        if (insert)
                        {
                            _eventBus.Publish(logisticsSortingInfoModel);
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
        public override string ExcelTitle => "物流分拣规则列表";
        public override string SheetName => "物流分拣规则列表";

        public override string SettingsName => "LogisticsSortingItemsSettings";

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
            if (obj is LogisticsSortingItemInfoModel item)
            {
                return await _sortingCatalog.DeleteByIdAsync(item.Id);
            }

            return false;
        }

        protected override async void ModifyDelegate(object obj)
        {
            if (obj is LogisticsSortingItemInfoModel item)
            {
                await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var logisticsSortingRuleEditor = new LogisticsSortingRuleEditor();
                    if (logisticsSortingRuleEditor.DataContext is LogisticsSortingRuleEditorViewModel model)
                    {
                        model.Identifier = Identifier;
                        model.LogisticsSortingItemInfo = item;
                        model.LogisticsRuleItems = item.LogisticsRuleItems;
                        await DialogHost.Show(logisticsSortingRuleEditor, model.Identifier);
                        if (!string.IsNullOrEmpty(model.ExceptionContent))
                        {
                            MessageQueue.Enqueue(model.ExceptionContent);
                            RefreshData();
                            return;
                        }
                        if (model.IsOk)
                        {
                            //添加到数据库
                            var logisticsSortingInfoModel = new LogisticsSortingInfoModel()
                            {
                                CreateTime = model.LogisticsSortingItemInfo.CreateTime,
                                ModifyTime = model.LogisticsSortingItemInfo.ModifyTime,
                                ExitId = model.SelectPackageExitDefinitionInfo.Id,
                                Remarks = model.LogisticsSortingItemInfo.Remarks,
                                SortingName = model.LogisticsSortingItemInfo.SortingName,
                                Id = model.LogisticsSortingItemInfo.Id,
                                LogisticsRuleItems = model.LogisticsRuleItems.Select(s => new LogisticsRuleInfoModel()
                                {
                                    CreateTime = s.CreateTime,
                                    ModifyTime = s.ModifyTime,
                                    LogisticsId = s.LogisticsId,
                                    Remarks = s.Remarks,
                                })?.ToList()
                            };
                            var update = await _sortingCatalog.UpdateAsync(logisticsSortingInfoModel);
                            if (update)
                            {
                                _eventBus.Publish(logisticsSortingInfoModel);
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
            var logisticsCodeRecognitionInfoModels = await _logisticsCatalog.ListAsync();

            var packageExitDefinitionInfoModels = await _packageExitCatalog.ListAsync();
            var models = await _sortingCatalog.ListAsync();
            LogisticsSortingItems.Clear();
            var infoModels = models?.Select((s, i) => new LogisticsSortingItemInfoModel()
            {
                CreateTime = s.CreateTime,
                Id = s.Id,
                ModifyTime = s.ModifyTime,
                Num = i + 1,
                Remarks = s.Remarks,
                ExitId = s.ExitId,
                SortingName = s.SortingName,
                ExitName = packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ?? string.Empty,
                LogisticsRuleItems = new ObservableCollection<LogisticsRuleItemInfoModel>(s.LogisticsRuleItems?.Select((s1, i1) => new LogisticsRuleItemInfoModel()
                {
                    CreateTime = s1.CreateTime,
                    Id = s1.Id,
                    LogisticsSortingId = s1.LogisticsSortingId,
                    ModifyTime = s1.ModifyTime,
                    Num = i1 + 1,
                    Remarks = s1.Remarks,
                    LogisticsId = s1.LogisticsId,
                    LogisticsName = logisticsCodeRecognitionInfoModels.FirstOrDefault(f => f.Id.Equals(s1.LogisticsId))?.LogisticsName ?? string.Empty
                }).ToList() ?? new List<LogisticsRuleItemInfoModel>()),
                SortingRuleGroup = string.Join(",", s.LogisticsRuleItems?.Select(s2 =>
                    logisticsCodeRecognitionInfoModels.FirstOrDefault(f => f.Id.Equals(s2.LogisticsId))?.LogisticsName ?? string.Empty) ?? Array.Empty<string>())
            })?.ToList();
            LogisticsSortingItems.AddRange(infoModels);
        }

        protected override bool IsSelectAnyItem() => LogisticsSortingItems.Any(a => a.IsSelect);

        protected override async Task BulkDeleteProcess()
        {
            var selectIds = LogisticsSortingItems.Where(w => w.IsSelect)
                .Select(s => s.Id).ToList();
            await _sortingCatalog.DeleteByIdsAsync(selectIds);
        }

        protected override List<LogisticsSortingItemInfoModel> ExportProcess()
        {
            return LogisticsSortingItems
                ?.SelectMany(s => s.SortingRuleGroup.Split(",")
                    .Select(item => new LogisticsSortingItemInfoModel()
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
                ?.ToList() ?? new List<LogisticsSortingItemInfoModel>();
        }

        protected override async Task<bool> ImportProcess(List<LogisticsSortingItemInfoModel> items)
        {
            if (items?.Any() == true)
            {
                var packageExitDefinitionInfoModels = await _packageExitCatalog.ListAsync();
                var logisticsCodeRecognitionInfoModels = await _logisticsCatalog.ListAsync();
                var dateTime = DateTime.Now;
                var logisticsSortingInfoModels = items
                    .Select(s => new LogisticsSortingInfoModel()
                    {
                        CreateTime = dateTime,
                        ExitId = packageExitDefinitionInfoModels.FirstOrDefault(f => f.ExitName.Equals(s.ExitName))?.Id ?? 0,
                        ModifyTime = dateTime,
                        SortingName = s.SortingName,
                        Remarks = s.Remarks,
                        LogisticsRuleItems = new List<LogisticsRuleInfoModel>
                        {
                            new()
                            {
                                CreateTime = dateTime,
                                ModifyTime = dateTime,
                                LogisticsId = logisticsCodeRecognitionInfoModels?.FirstOrDefault(f=>f.LogisticsName.Equals(s.SortingRuleGroup))?.Id??0,
                            }
                        }
                    })
                    .GroupBy(s => s.SortingName)
                    .Select(group => new LogisticsSortingInfoModel
                    {
                        CreateTime = group.First().CreateTime,
                        ExitId = group.First().ExitId,
                        SortingName = group.Key,
                        ModifyTime = group.First().ModifyTime,
                        Remarks = group.First().Remarks,
                        LogisticsRuleItems = [.. group.SelectMany(item => item.LogisticsRuleItems ?? new List<LogisticsRuleInfoModel>())]
                    })
                    .ToList();

                //批量添加
                return await _sortingCatalog.AddRangeAsync(logisticsSortingInfoModels);
            }

            return false;
        }
    }
}
