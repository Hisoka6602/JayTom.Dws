using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using JayTom.Dws.Plugin;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using SettingsChangedEvent = JayTom.Dws.Domain.EventMediators.SettingsChangedEvent;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages
{

    public class VolumeSortingViewModel : BulkOperationsTemplateViewModel<VolumeSortingItemInfoModel>
    {
        private readonly IVolumeSortingRepository _volumeSortingRepository;
        private readonly IVolumeRuleRepository _volumeRuleRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;

        private ObservableCollection<VolumeSortingItemInfoModel> _volumeSortingItems = new();

        private bool _isLoaded;

        public VolumeSortingViewModel(IVolumeSortingRepository volumeSortingRepository,
            IVolumeRuleRepository volumeRuleRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IExcel excel) : base(excel)
        {
            _volumeSortingRepository = volumeSortingRepository;
            _volumeRuleRepository = volumeRuleRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
        }

        public ObservableCollection<VolumeSortingItemInfoModel> VolumeSortingItems
        {
            get => _volumeSortingItems;
            set => SetProperty(ref _volumeSortingItems, value);
        }

        protected override async void AddDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var volumeSortingRuleEditor = new VolumeSortingRuleEditor();
                if (volumeSortingRuleEditor.DataContext is VolumeSortingRuleEditorViewModel model)
                {
                    model.Identifier = Identifier;
                    await DialogHost.Show(volumeSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent))
                    {
                        MessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk)
                    {
                        //添加到数据库
                        var volumeSortingInfoModel = new VolumeSortingInfoModel()
                        {
                            CreateTime = model.VolumeSortingItemInfo.CreateTime,
                            ModifyTime = model.VolumeSortingItemInfo.ModifyTime,
                            ExitId = model.SelectPackageExitDefinitionInfo.Id,
                            Remarks = model.VolumeSortingItemInfo.Remarks,
                            SortingName = model.VolumeSortingItemInfo.SortingName,
                            VolumeRuleItems = model.VolumeRuleItems?.Select(s => new VolumeRuleInfoModel()
                            {
                                CreateTime = s.CreateTime,
                                Formula = s.Formula,
                                ModifyTime = s.ModifyTime,
                                Remarks = s.Remarks,
                            })?.ToList()
                        };
                        var insert = await _volumeSortingRepository.InsertDetailAsync(volumeSortingInfoModel);
                        if (insert)
                        {
                            EventAggregator.Instance.Publish(volumeSortingInfoModel);
                            EventAggregator.Instance.Publish(new SettingsChangedEvent
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
        public override string ExcelTitle => "体积分拣列表";
        public override string SheetName => "体积分拣列表";

        public override string SettingsName => "VolumeSortingItemsSettings";

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
            if (obj is VolumeSortingItemInfoModel item)
            {
                var volumeSortingInfoModel = await _volumeSortingRepository.
                    FirstOrDefault(f => f.Id.Equals(item.Id));
                if (volumeSortingInfoModel is not null)
                {
                    return await _volumeSortingRepository.Delete(volumeSortingInfoModel);
                }
            }

            return false;
        }

        protected override async void ModifyDelegate(object obj)
        {
            if (obj is VolumeSortingItemInfoModel item)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var volumeSortingRuleEditor = new VolumeSortingRuleEditor();
                    if (volumeSortingRuleEditor.DataContext is VolumeSortingRuleEditorViewModel model)
                    {
                        model.Identifier = Identifier;
                        model.VolumeSortingItemInfo = item;
                        model.VolumeRuleItems = item.VolumeRuleItems;
                        await DialogHost.Show(volumeSortingRuleEditor, model.Identifier);
                        if (!string.IsNullOrEmpty(model.ExceptionContent))
                        {
                            MessageQueue.Enqueue(model.ExceptionContent);
                            RefreshData();
                            return;
                        }
                        if (model.IsOk)
                        {
                            //添加到数据库
                            var volumeSortingInfoModel = new VolumeSortingInfoModel()
                            {
                                CreateTime = model.VolumeSortingItemInfo.CreateTime,
                                ModifyTime = model.VolumeSortingItemInfo.ModifyTime,
                                ExitId = model.SelectPackageExitDefinitionInfo.Id,
                                Remarks = model.VolumeSortingItemInfo.Remarks,
                                SortingName = model.VolumeSortingItemInfo.SortingName,
                                Id = model.VolumeSortingItemInfo.Id,
                                VolumeRuleItems = model.VolumeRuleItems?.Select(s => new VolumeRuleInfoModel()
                                {
                                    CreateTime = s.CreateTime,
                                    Formula = s.Formula,
                                    ModifyTime = s.ModifyTime,
                                    Remarks = s.Remarks,
                                    VolumeSortingId = model.VolumeSortingItemInfo.Id,
                                })?.ToList()
                            };
                            var insert = await _volumeSortingRepository.UpdateDetailAsync(volumeSortingInfoModel);
                            if (insert)
                            {
                                EventAggregator.Instance.Publish(volumeSortingInfoModel);
                                EventAggregator.Instance.Publish(new SettingsChangedEvent
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
            var volumeSortingInfoModels = await _volumeSortingRepository.Select(s => s.Id > 0,
                o => o.Id);
            await _volumeSortingRepository.DeleteRange(volumeSortingInfoModels);
        }

        protected override async Task RefreshDataProcess()
        {
            await Task.Delay(300);
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _volumeSortingRepository
                .VolumeSortingItems(s => s.Id > 0);
            VolumeSortingItems.Clear();
            var infoModels = models?.Select((s, i) => new VolumeSortingItemInfoModel()
            {
                CreateTime = s.CreateTime,
                Id = s.Id,
                ModifyTime = s.ModifyTime,
                Num = i + 1,
                Remarks = s.Remarks,
                ExitId = s.ExitId,
                SortingName = s.SortingName,
                ExitName = packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ?? string.Empty,
                VolumeRuleItems = new ObservableCollection<VolumeRuleItemInfoModel>(s.VolumeRuleItems?.Select((s1, i1) => new VolumeRuleItemInfoModel()
                {
                    CreateTime = s1.CreateTime,
                    Id = s1.Id,
                    VolumeSortingId = s1.VolumeSortingId,
                    ModifyTime = s1.ModifyTime,
                    Num = i1 + 1,
                    Remarks = s1.Remarks,
                    Formula = s1.Formula
                }).ToList() ?? new List<VolumeRuleItemInfoModel>()),
                SortingRuleGroup = string.Join("\n", s.VolumeRuleItems?.Select(s2 => s2.Formula) ?? Array.Empty<string>())
            })?.ToList();
            VolumeSortingItems.AddRange(infoModels);
        }

        protected override bool IsSelectAnyItem() => VolumeSortingItems.Any(a => a.IsSelect);

        protected override async Task BulkDeleteProcess()
        {
            var selectIds = VolumeSortingItems.Where(w => w.IsSelect)
                .Select(s => s.Id).ToList();
            var volumeSortingInfoModels = await _volumeSortingRepository.Select(s => selectIds.Contains(s.Id),
                o => o.Id);
            await _volumeSortingRepository.DeleteRange(volumeSortingInfoModels);
        }

        protected override List<VolumeSortingItemInfoModel> ExportProcess()
        {
            return VolumeSortingItems
                ?.SelectMany(s => s.SortingRuleGroup.Split("\n")
                    .Select(item => new VolumeSortingItemInfoModel()
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
                ?.ToList() ?? new List<VolumeSortingItemInfoModel>();
        }

        protected override async Task<bool> ImportProcess(List<VolumeSortingItemInfoModel> items)
        {
            if (items?.Any() == true)
            {
                var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                    o => o.CreateTime);
                var dateTime = DateTime.Now;
                var volumeSortingInfoModels = items
                    .Select(s => new VolumeSortingInfoModel()
                    {
                        CreateTime = dateTime,
                        ExitId = packageExitDefinitionInfoModels.FirstOrDefault(f => f.ExitName.Equals(s.ExitName))?.Id ?? 0,
                        ModifyTime = dateTime,
                        SortingName = s.SortingName,
                        Remarks = s.Remarks,
                        VolumeRuleItems = new List<VolumeRuleInfoModel>
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
                    .Select(group => new VolumeSortingInfoModel
                    {
                        CreateTime = group.First().CreateTime,
                        ExitId = group.First().ExitId,
                        SortingName = group.Key,
                        ModifyTime = group.First().ModifyTime,
                        Remarks = group.First().Remarks,
                        VolumeRuleItems = [.. group.SelectMany(item => item.VolumeRuleItems ?? new List<VolumeRuleInfoModel>())]
                    })
                    .ToList();

                //批量添加
                return await _volumeSortingRepository.InsertRangeDetailAsync(volumeSortingInfoModels);
            }

            return false;
        }
    }
}