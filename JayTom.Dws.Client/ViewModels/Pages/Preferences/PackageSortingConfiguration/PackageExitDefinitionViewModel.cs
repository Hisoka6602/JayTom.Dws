using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using JayTom.Dws.Plugin;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Collections.Generic;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.Models.PackageSorting;
using Application = System.Windows.Application;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration
{

    //包裹出口定义页面
    public class PackageExitDefinitionViewModel : BulkOperationsTemplateViewModel<PackageExitDefinitionItemInfoModel>
    {
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;

        private readonly ICommunicationConnectionConfigRepository _communicationConnectionConfigRepository;
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();

        public PackageExitDefinitionViewModel(IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IExcel excel,
            ICommunicationConnectionConfigRepository communicationConnectionConfigRepository) : base(excel)
        {
            _packageExitDefinitionRepository = packageExitDefinitionRepository;

            _communicationConnectionConfigRepository = communicationConnectionConfigRepository;
        }

        public ObservableCollection<PackageExitDefinitionItemInfoModel> PackageExitDefinitionItems
        {
            get => _packageExitDefinitionItems;
            set => SetProperty(ref _packageExitDefinitionItems, value);
        }

        protected override async void AddDelegate(object obj)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var packageExitDefinitionEditor = new PackageExitDefinitionEditor();
                if (packageExitDefinitionEditor.DataContext is PackageExitDefinitionEditorViewModel model)
                {
                    model.Identifier = Identifier;
                    model.PackageExitDefinitionItems = new ObservableCollection<PackageExitDefinitionItemInfoModel>(PackageExitDefinitionItems.
                        Where(w => w.Type == ExitType.PackageExit)
                        ?.ToList() ?? new List<PackageExitDefinitionItemInfoModel>());
                    await DialogHost.Show(packageExitDefinitionEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent))
                    {
                        MessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk)
                    {
                        //保存到数据库
                        var packageExitDefinitionInfoModel = new PackageExitDefinitionInfoModel()
                        {
                            CreateTime = DateTime.Now,
                            ExitName = model.ExitName,
                            IsActive = model.IsActive,
                            ModifyTime = DateTime.Now,
                            Remarks = model.Remarks,
                            Type = model.Type,
                            CommunicationConnectionId = model.SelectConnectionItem.Id,
                            Pid = model.SelectExitDefinitionInfo.Id
                        };
                        var insertOrUpdate = await _packageExitDefinitionRepository.Insert(packageExitDefinitionInfoModel);
                        if (insertOrUpdate)
                        {
                            EventAggregator.Instance.Publish(packageExitDefinitionInfoModel);
                            EventAggregator.Instance.Publish(new SettingsChangedEvent
                            {
                                SettingsName = SettingsName,
                                IsLocallySaved = true
                            });

                            MessageQueue.Enqueue("保存成功");
                            //刷新列表
                            RefreshData();
                        }
                        else
                        {
                            MessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            }, DispatcherPriority.Background);
        }

        protected override async Task<bool> DeleteProcess(object obj)
        {
            if (obj is PackageExitDefinitionItemInfoModel item)
            {
                var model = await _packageExitDefinitionRepository.FirstOrDefault(f => f.Id.Equals(item.Id));
                if (model is not null)
                {
                    return await _packageExitDefinitionRepository.Delete(model);
                }
            }

            return false;
        }

        protected override async Task BulkDeleteProcess()
        {
            var selectIds = PackageExitDefinitionItems.Where(w => w.IsSelect)
                .Select(s => s.Id).ToList();
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository
                .Select(s => selectIds.Contains(s.Id),
                o => o.Id);
            await _packageExitDefinitionRepository.DeleteRange(packageExitDefinitionInfoModels);
        }

        protected override List<PackageExitDefinitionItemInfoModel> ExportProcess() => [.. PackageExitDefinitionItems];

        protected override async Task<bool> ImportProcess(List<PackageExitDefinitionItemInfoModel> items)
        {
            if (items?.Any() == true)
            {
                //批量添加到数据库
                var configInfoModels = await _communicationConnectionConfigRepository.Select(s =>
                    s.Id > 0, o => o.Id);

                var connectionIdsByName = configInfoModels
                    .Where(item => !string.IsNullOrWhiteSpace(item.ConnectionName))
                    .GroupBy(item => item.ConnectionName, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.Ordinal);
                var infoModels = new List<PackageExitDefinitionInfoModel>(items.Count);
                foreach (var item in items)
                {
                    if (!connectionIdsByName.TryGetValue(
                            item.CommunicationConnectionName ?? string.Empty,
                            out var connectionId))
                    {
                        continue;
                    }

                    infoModels.Add(new PackageExitDefinitionInfoModel
                    {
                        CreateTime = DateTime.Now,
                        ExitName = item.ExitName,
                        IsActive = item.IsActive,
                        ModifyTime = DateTime.Now,
                        Remarks = item.Remarks,
                        Type = item.Type,
                        CommunicationConnectionId = connectionId
                    });
                }

                var insertOrUpdate = await _packageExitDefinitionRepository.InsertRange(infoModels);
                if (insertOrUpdate)
                {
                    //如果存在Pid则更新Pid
                    var itemInfoModels = items?.Where(w => !string.IsNullOrEmpty(w.MainExitName))?.ToList();
                    if (itemInfoModels?.Any() == true)
                    {
                        var list = itemInfoModels.Select(s => s.MainExitName).ToList();
                        var upDataList = itemInfoModels.Select(s => s.ExitName).ToList();
                        //取出主Id
                        var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => list.Contains(s.ExitName),
                            o => o.Id);
                        var exitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => upDataList.Contains(s.ExitName),
                            o => o.Id);
                        foreach (var packageExitDefinitionInfoModel in exitDefinitionInfoModels)
                        {
                            var infoModel = itemInfoModels.FirstOrDefault(f =>
                                f.ExitName.Equals(packageExitDefinitionInfoModel.ExitName));
                            var definitionInfoModel = packageExitDefinitionInfoModels.FirstOrDefault(f =>
                                f.ExitName.Equals(infoModel?.MainExitName));
                            packageExitDefinitionInfoModel.Pid = definitionInfoModel?.Id ?? 0;
                        }

                        var updateRange = await _packageExitDefinitionRepository.UpdateRange(exitDefinitionInfoModels);
                        if (!updateRange)
                        {
                            return false;
                        }
                    }
                }

                return insertOrUpdate;
            }

            return false;
        }

        protected override async void ModifyDelegate(object obj)
        {
            if (obj is PackageExitDefinitionItemInfoModel item)
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    var packageExitDefinitionEditor = new PackageExitDefinitionEditor();
                    if (packageExitDefinitionEditor.DataContext is PackageExitDefinitionEditorViewModel model)
                    {
                        model.PackageExitDefinitionItems = new ObservableCollection<PackageExitDefinitionItemInfoModel>(PackageExitDefinitionItems.
                            Where(w => w.Type == ExitType.PackageExit &&
                                       !w.Id.Equals(item.Id))
                            ?.ToList() ?? new List<PackageExitDefinitionItemInfoModel>());
                        model.Identifier = Identifier;
                        model.Type = item.Type;
                        model.ExitName = item.ExitName;
                        model.IsActive = item.IsActive;
                        model.Id = item.Id;
                        model.Remarks = item.Remarks;
                        model.CommunicationConnectionId = item.CommunicationConnectionId;
                        model.SelectExitDefinitionInfo =
                            PackageExitDefinitionItems.FirstOrDefault(f =>
                                f.Id.Equals(item.Pid)) ??
                            new PackageExitDefinitionItemInfoModel();
                        await DialogHost.Show(packageExitDefinitionEditor, model.Identifier);
                        if (!string.IsNullOrEmpty(model.ExceptionContent))
                        {
                            MessageQueue.Enqueue(model.ExceptionContent);
                            return;
                        }
                        if (model.IsOk)
                        {
                            //保存到数据库
                            var insertOrUpdate = await _packageExitDefinitionRepository.Update(new PackageExitDefinitionInfoModel()
                            {
                                CreateTime = item.CreateTime,
                                ExitName = model.ExitName,
                                IsActive = model.IsActive,
                                ModifyTime = DateTime.Now,
                                Remarks = model.Remarks,
                                Type = model.Type,
                                Id = model.Id,
                                CommunicationConnectionId = model.CommunicationConnectionId,
                                Pid = model.SelectExitDefinitionInfo.Id
                            });
                            if (insertOrUpdate)
                            {
                                MessageQueue.Enqueue("保存成功");
                                //刷新列表
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
                        }
                    }
                }, DispatcherPriority.Background);
            }
        }

        public ICommand ActiveCommand => new DelegateCommand<PackageExitDefinitionItemInfoModel>(ActiveDelegate);

        private async void ActiveDelegate(PackageExitDefinitionItemInfoModel obj)
        {
            var model = await _packageExitDefinitionRepository.FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (model is not null)
            {
                model.IsActive = obj.IsActive;
                var update = await _packageExitDefinitionRepository.Update(model);
                if (!update)
                {
                    MessageQueue.Enqueue("保存失败");
                }
                return;
            }
            obj.IsActive = !obj.IsActive;
        }

        public override string Identifier => "PackageSortingSettingsDialog";
        public override string ExcelTitle => "定义格口列表";
        public override string SheetName => "定义格口列表";

        public override string SettingsName => "PackageExitDefinitionItemSettings";

        public override void LoadedDelegate(object obj)
        {
            RefreshData();
        }

        protected override async Task ClearProcess()
        {
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                o => o.Id);
            await _packageExitDefinitionRepository.DeleteRange(packageExitDefinitionInfoModels);
        }

        protected override async Task RefreshDataProcess()
        {
            await Task.Delay(300);
            var configInfoModels = await _communicationConnectionConfigRepository.CommunicationConnectionConfigItems(s => s.Id > 0);
            var models = await _packageExitDefinitionRepository.
                Select(s => s.Id > 0,
                    o => o.ModifyTime);

            PackageExitDefinitionItems.Clear();
            var infoModels = models?.Select((s, i) => new PackageExitDefinitionItemInfoModel
            {
                Pid = s.Pid,
                CreateTime = s.CreateTime,
                ExitName = s.ExitName,
                Id = s.Id,
                IsActive = s.IsActive,
                ModifyTime = s.ModifyTime,
                Num = i + 1,
                Remarks = s.Remarks,
                Type = s.Type,
                MainExitName = models?.FirstOrDefault(f => f.Id.Equals(s.Pid))?.ExitName ?? string.Empty,
                CommunicationConnectionId = configInfoModels?.FirstOrDefault(f => f.Id.Equals(s.CommunicationConnectionId))?.Id ?? 0,
                CommunicationConnectionName = configInfoModels?.FirstOrDefault(f => f.Id.Equals(s.CommunicationConnectionId))?.ConnectionName ?? string.Empty
            })?.ToList();
            PackageExitDefinitionItems.AddRange(infoModels);
        }

        protected override bool IsSelectAnyItem() => PackageExitDefinitionItems.Any(a => a.IsSelect);
    }
}
