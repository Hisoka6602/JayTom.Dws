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
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration
{

    //分拣指令绑定页面
    public class SortingInstructionBindingViewModel : BulkOperationsTemplateViewModel<SortingInstructionBindingItemInfoModel>
    {
        private readonly ISortingInstructionBindingRepository _sortingInstructionBindingRepository;
        private readonly ISortingInstructionRepository _sortingInstructionRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;

        private readonly ISortingConnectionService _sortingConnectionService;

        private ObservableCollection<SortingInstructionBindingItemInfoModel> _sortingInstructionBindingItems = new();

        public SortingInstructionBindingViewModel(ISortingInstructionBindingRepository sortingInstructionBindingRepository,
            ISortingInstructionRepository sortingInstructionRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IExcel excel, ISortingConnectionService sortingConnectionService) : base(excel)
        {
            _sortingInstructionBindingRepository = sortingInstructionBindingRepository;
            _sortingInstructionRepository = sortingInstructionRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;

            _sortingConnectionService = sortingConnectionService;

            _sortingConnectionService.SendError += delegate (object? sender, ExceptionEventArgs args)
            {
                MessageQueue.Enqueue(args.ExceptionMessage);
            };
        }

        public ObservableCollection<SortingInstructionBindingItemInfoModel> SortingInstructionBindingItems
        {
            get => _sortingInstructionBindingItems;
            set => SetProperty(ref _sortingInstructionBindingItems, value);
        }

        protected override async void AddDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var bindingEditor = new SortingInstructionBindingEditor();

                if (bindingEditor.DataContext is SortingInstructionBindingEditorViewModel model)
                {
                    model.Identifier = Identifier;
                    await DialogHost.Show(bindingEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent))
                    {
                        MessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk)
                    {
                        bool canActive = true;
                        if (model.SortingInstructionBindingItemInfo.IsActive)
                        {
                            var orDefault = await _sortingInstructionBindingRepository.FirstOrDefault(f =>
                                f.ExitId.Equals(model.SelectExitDefinitionInfo.Id) &&
                                f.IsActive);
                            canActive = orDefault is null;
                        }

                        //添加到数据库
                        var insertOrUpdate = await _sortingInstructionBindingRepository.InsertDetailAsync(new SortingInstructionBindingInfoModel()
                        {
                            CreateTime = DateTime.Now,
                            DelaySendMilliseconds = model.SortingInstructionBindingItemInfo.DelaySendMilliseconds,
                            ExitId = model.SelectExitDefinitionInfo.Id,
                            SendIntervalMilliseconds =
                                model.SortingInstructionBindingItemInfo.SendIntervalMilliseconds,
                            IsActive = canActive && model.SortingInstructionBindingItemInfo.IsActive,
                            ModifyTime = DateTime.Now,
                            Remarks = model.SortingInstructionBindingItemInfo.Remarks,
                            InstructionItems = model.SortingInstructionItems.Select(s => new SortingInstructionInfoModel
                            {
                                CreateTime = s.CreateTime,
                                Remarks = s.Remarks,
                                Instruction = s.Instruction,
                                ReplyContent = s.ReplyContent,
                                ModifyTime = s.ModifyTime,
                            })?.ToList()
                        });
                        if (insertOrUpdate)
                        {
                            MessageQueue.Enqueue("保存成功");
                            RefreshData();
                            EventAggregator.Instance.Publish(new SettingsChangedEvent
                            {
                                SettingsName = SettingsName,
                                IsLocallySaved = true
                            });
                        }
                        else
                        {
                            MessageQueue.Enqueue("保存失败");
                        }

                        //添加到指令列表对应内容
                    }
                }
            });
        }

        public override string Identifier => "PackageSortingSettingsDialog";
        public override string ExcelTitle => "指令绑定列表";
        public override string SheetName => "指令绑定列表";

        public override string SettingsName => "SortingInstructionBindingItemSettings";

        public override void LoadedDelegate(object obj)
        {
            RefreshData();
        }

        protected override async Task<bool> DeleteProcess(object obj)
        {
            if (obj is SortingInstructionBindingItemInfoModel item)
            {
                var sortingInstructionBindingInfoModel = await _sortingInstructionBindingRepository.
                    FirstOrDefault(f => f.Id.Equals(item.Id));
                if (sortingInstructionBindingInfoModel is not null)
                {
                    return await _sortingInstructionBindingRepository.Delete(sortingInstructionBindingInfoModel);
                }
            }

            return false;
        }

        protected override async void ModifyDelegate(object obj)
        {
            if (obj is SortingInstructionBindingItemInfoModel item)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var bindingEditor = new SortingInstructionBindingEditor();

                    if (bindingEditor.DataContext is SortingInstructionBindingEditorViewModel model)
                    {
                        model.Identifier = Identifier;
                        model.SortingInstructionBindingItemInfo = item;
                        model.SortingInstructionItems = item.SortingInstructionItems;
                        await DialogHost.Show(bindingEditor, model.Identifier);
                        if (!string.IsNullOrEmpty(model.ExceptionContent))
                        {
                            MessageQueue.Enqueue(model.ExceptionContent);
                            return;
                        }
                        if (model.IsOk)
                        {
                            bool canActive = true;
                            if (model.SortingInstructionBindingItemInfo.IsActive)
                            {
                                var orDefault = await _sortingInstructionBindingRepository.FirstOrDefault(f =>
                                    f.ExitId.Equals(model.SelectExitDefinitionInfo.Id) &&
                                    f.IsActive);
                                canActive = orDefault is null;
                            }

                            //添加到数据库
                            var insertOrUpdate = await _sortingInstructionBindingRepository.UpdateDetailAsync(new SortingInstructionBindingInfoModel()
                            {
                                CreateTime = DateTime.Now,
                                DelaySendMilliseconds = model.SortingInstructionBindingItemInfo.DelaySendMilliseconds,
                                ExitId = model.SelectExitDefinitionInfo.Id,
                                SendIntervalMilliseconds =
                                    model.SortingInstructionBindingItemInfo.SendIntervalMilliseconds,
                                IsActive = canActive && model.SortingInstructionBindingItemInfo.IsActive,
                                ModifyTime = DateTime.Now,
                                Remarks = model.SortingInstructionBindingItemInfo.Remarks,
                                Id = model.SortingInstructionBindingItemInfo.Id,
                                InstructionItems = model.SortingInstructionItems.Select(s => new SortingInstructionInfoModel
                                {
                                    CreateTime = s.CreateTime,
                                    Remarks = s.Remarks,
                                    Instruction = s.Instruction,
                                    ReplyContent = s.ReplyContent,
                                    ModifyTime = s.ModifyTime,
                                })?.ToList()
                            });
                            if (insertOrUpdate)
                            {
                                MessageQueue.Enqueue("保存成功");
                                RefreshData();
                                EventAggregator.Instance.Publish(new SettingsChangedEvent
                                {
                                    SettingsName = SettingsName,
                                    IsLocallySaved = true
                                });
                            }
                            else
                            {
                                MessageQueue.Enqueue("保存失败");
                            }

                            //添加到指令列表对应内容
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 发送指令
        /// </summary>
        public ICommand SendInstructionCommand => new DelegateCommand<SortingInstructionBindingItemInfoModel>(SendInstructionDelegate);

        private void SendInstructionDelegate(SortingInstructionBindingItemInfoModel obj)
        {
            _sortingConnectionService.SendInstructions(new object(), obj.ExitId ?? 0,
                obj.SortingInstructionItems.Select(s => s.Instruction)
                    ?.ToList() ?? new List<string>(),
                TimeSpan.FromMilliseconds(obj.SendIntervalMilliseconds),
                new InstructionsAttach()
                {
                    Timestamp = 0,
                });
        }

        /// <summary>
        /// 是否激活
        /// </summary>
        public ICommand ActiveCommand => new DelegateCommand<SortingInstructionBindingItemInfoModel>(ActiveDelegate);

        private async void ActiveDelegate(SortingInstructionBindingItemInfoModel obj)
        {
            var model = await _sortingInstructionBindingRepository.FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (model is not null)
            {
                model.IsActive = obj.IsActive;
                var update = await _sortingInstructionBindingRepository.Update(model);
                if (!update)
                {
                    MessageQueue.Enqueue("保存失败");
                }
                return;
            }
            obj.IsActive = !obj.IsActive;
        }

        protected override async Task ClearProcess()
        {
            var sortingInstructionBindingInfoModels = await _sortingInstructionBindingRepository.Select(s => s.Id > 0,
                o => o.Id);
            await _sortingInstructionBindingRepository.DeleteRange(sortingInstructionBindingInfoModels);
        }

        protected override async Task RefreshDataProcess()
        {
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _sortingInstructionBindingRepository.
                InstructionBindings(s => s.Id > 0);
            SortingInstructionBindingItems.Clear();
            var infoModels = models?.Select((s, i) => new SortingInstructionBindingItemInfoModel
            {
                CreateTime = s.CreateTime,
                Id = s.Id,
                ModifyTime = s.ModifyTime,
                Num = i + 1,
                Remarks = s.Remarks,
                ExitId = s.ExitId,
                ExitName = packageExitDefinitionInfoModels?.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ?? string.Empty,
                DelaySendMilliseconds = s.DelaySendMilliseconds,
                SendIntervalMilliseconds = s.SendIntervalMilliseconds,
                IsActive = s.IsActive,

                SortingInstructionItems = new ObservableCollection<SortingInstructionItemInfoModel>(s.InstructionItems?.Select((s1, i1) => new SortingInstructionItemInfoModel
                {
                    CreateTime = s1.CreateTime,
                    Id = s1.Id,
                    InstructionBindingId = s1.InstructionBindingId,
                    ModifyTime = s1.ModifyTime,
                    Num = i1 + 1,
                    Remarks = s1.Remarks,
                    Instruction = s1.Instruction
                }).ToList() ?? new List<SortingInstructionItemInfoModel>()),
                SortingInstructionGroup = string.Join("\n", s.InstructionItems?.Select(s2 => s2.Instruction) ?? Array.Empty<string>())
            })?.ToList();
            SortingInstructionBindingItems.AddRange(infoModels);
        }

        protected override bool IsSelectAnyItem() => SortingInstructionBindingItems.Any(a => a.IsSelect);

        protected override async Task BulkDeleteProcess()
        {
            var selectIds = SortingInstructionBindingItems.Where(w => w.IsSelect)
                .Select(s => s.Id).ToList();
            var sortingInstructionBindingInfoModels = await _sortingInstructionBindingRepository
                .Select(s => selectIds.Contains(s.Id),
                o => o.Id);
            await _sortingInstructionBindingRepository.DeleteRange(sortingInstructionBindingInfoModels);
        }

        protected override List<SortingInstructionBindingItemInfoModel> ExportProcess()
        {
            return SortingInstructionBindingItems
                ?.SelectMany(s => s.SortingInstructionGroup.Split("\n")
                    .Select(item => new SortingInstructionBindingItemInfoModel
                    {
                        CreateTime = s.CreateTime,
                        DelaySendMilliseconds = s.DelaySendMilliseconds,
                        ExitId = s.ExitId,
                        SendIntervalMilliseconds = s.SendIntervalMilliseconds,
                        IsActive = s.IsActive,
                        ModifyTime = s.ModifyTime,
                        Remarks = s.Remarks,
                        ExitName = s.ExitName,
                        Num = s.Num,
                        Id = s.Id,
                        SortingInstructionGroup = item,
                    }))
                ?.ToList() ?? new List<SortingInstructionBindingItemInfoModel>();
        }

        protected override async Task<bool> ImportProcess(List<SortingInstructionBindingItemInfoModel> items)
        {
            if (items?.Any() == true)
            {
                var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                    o => o.CreateTime);
                var dateTime = DateTime.Now;
                var sortingInstructionBindingInfoModels = items
                    .Select(s => new SortingInstructionBindingInfoModel
                    {
                        CreateTime = dateTime,
                        DelaySendMilliseconds = s.DelaySendMilliseconds,
                        ExitId = packageExitDefinitionInfoModels.FirstOrDefault(f => f.ExitName.Equals(s.ExitName))?.Id ?? 0,
                        SendIntervalMilliseconds = s.SendIntervalMilliseconds,
                        IsActive = s.IsActive,
                        ModifyTime = dateTime,
                        Remarks = s.Remarks,
                        InstructionItems = new List<SortingInstructionInfoModel>
                        {
                                    new()
                                    {
                                        CreateTime = dateTime,
                                        ModifyTime = dateTime,
                                        Instruction = s.SortingInstructionGroup,
                                    }
                        }
                    })
                    .GroupBy(s => s.ExitId)
                    .Select(group => new SortingInstructionBindingInfoModel
                    {
                        CreateTime = group.First().CreateTime,
                        DelaySendMilliseconds = group.First().DelaySendMilliseconds,
                        ExitId = group.Key,
                        SendIntervalMilliseconds = group.First().SendIntervalMilliseconds,
                        IsActive = group.First().IsActive,
                        ModifyTime = group.First().ModifyTime,
                        Remarks = group.First().Remarks,
                        InstructionItems = [.. group.SelectMany(item => item.InstructionItems)]
                    })
                    .ToList();

                //批量添加
                return await _sortingInstructionBindingRepository.InsertRangeDetailAsync(sortingInstructionBindingInfoModels);
            }

            return false;
        }
    }
}