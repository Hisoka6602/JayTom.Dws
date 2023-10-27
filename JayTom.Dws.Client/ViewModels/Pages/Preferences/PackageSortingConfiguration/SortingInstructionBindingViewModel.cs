using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using Prism.Commands;
using System.Windows.Input;
using NPOI.SS.Util.CellWalk;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    //分拣指令绑定页面
    public class SortingInstructionBindingViewModel : BindableBase {
        private readonly ISortingInstructionBindingRepository _sortingInstructionBindingRepository;
        private readonly ISortingInstructionRepository _sortingInstructionRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly IInventoryManagementService _inventoryManagementService;

        private ObservableCollection<SortingInstructionBindingItemInfoModel> _sortingInstructionBindingItems = new();

        private SnackbarMessageQueue _sortingInstructionBindingMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public SortingInstructionBindingViewModel(ISortingInstructionBindingRepository sortingInstructionBindingRepository,
            ISortingInstructionRepository sortingInstructionRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            ISortingService sortingService,
            IInventoryManagementService inventoryManagementService) {
            _sortingInstructionBindingRepository = sortingInstructionBindingRepository;
            _sortingInstructionRepository = sortingInstructionRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;

            _inventoryManagementService = inventoryManagementService;
            _inventoryManagementService.SendError += delegate (object? sender, ExceptionEventArgs args) {
                SortingInstructionBindingMessageQueue.Enqueue(args.ExceptionMessage);
            };
        }

        public SnackbarMessageQueue SortingInstructionBindingMessageQueue {
            get => _sortingInstructionBindingMessageQueue;
            set => SetProperty(ref _sortingInstructionBindingMessageQueue, value);
        }

        public ObservableCollection<SortingInstructionBindingItemInfoModel> SortingInstructionBindingItems {
            get => _sortingInstructionBindingItems;
            set => SetProperty(ref _sortingInstructionBindingItems, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var bindingEditor = new SortingInstructionBindingEditor();

                if (bindingEditor.DataContext is SortingInstructionBindingEditorViewModel model) {
                    model.Identifier = "SortingInstructionBindingDialog";
                    await DialogHost.Show(bindingEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        SortingInstructionBindingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        bool canActive = true;
                        if (model.SortingInstructionBindingItemInfo.IsActive) {
                            var orDefault = await _sortingInstructionBindingRepository.FirstOrDefault(f =>
                                f.ExitId.Equals(model.SelectExitDefinitionInfo.Id) &&
                                f.IsActive);
                            canActive = orDefault is null;
                        }

                        //添加到数据库
                        var insertOrUpdate = await _sortingInstructionBindingRepository.Insert(new SortingInstructionBindingInfoModel() {
                            CreateTime = DateTime.Now,
                            DelaySendMilliseconds = model.SortingInstructionBindingItemInfo.DelaySendMilliseconds,
                            ExitId = model.SelectExitDefinitionInfo.Id,
                            SendIntervalMilliseconds =
                                model.SortingInstructionBindingItemInfo.SendIntervalMilliseconds,
                            IsActive = canActive && model.SortingInstructionBindingItemInfo.IsActive,
                            ModifyTime = DateTime.Now,
                            Remarks = model.SortingInstructionBindingItemInfo.Remarks,
                        });
                        if (insertOrUpdate) {
                            //取出数据库对应指令列表内容
                            var infoModels = await _sortingInstructionBindingRepository.SelectOrderByDescending(
                                s => s.ExitId.Equals(model.SelectExitDefinitionInfo.Id),
                                o => o.CreateTime);
                            var instructionBindingInfoModel = infoModels?.FirstOrDefault() ?? new SortingInstructionBindingInfoModel();
                            //取出model指令列表内容
                            var instructionInfoModels = model.SortingInstructionItems.Select(s => new SortingInstructionInfoModel {
                                CreateTime = s.CreateTime,
                                Remarks = s.Remarks,
                                Instruction = s.Instruction,
                                ReplyContent = s.ReplyContent,
                                InstructionBindingId = instructionBindingInfoModel?.Id ?? 0,
                                ModifyTime = s.ModifyTime,
                            })?.ToList();
                            //删除数据库指令列表对应内容
                            var sortingInstructionInfoModels = await _sortingInstructionRepository.Select(
                                s => s.InstructionBindingId.Equals(instructionBindingInfoModel.Id),
                                o => o.Id);
                            if (sortingInstructionInfoModels?.Any() == true) {
                                await _sortingInstructionRepository.DeleteRange(sortingInstructionInfoModels);
                            }

                            var insertRange = await _sortingInstructionRepository.InsertRange(instructionInfoModels ?? new List<SortingInstructionInfoModel>());
                            if (insertRange) {
                                SortingInstructionBindingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                SortingInstructionBindingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            SortingInstructionBindingMessageQueue.Enqueue("保存失败");
                        }

                        //添加到指令列表对应内容
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

        /// <summary>
        /// 修改
        /// </summary>
        public ICommand ModifyCommand {
            get => new DelegateCommand<SortingInstructionBindingItemInfoModel>(ModifyDelegate);
        }

        private async void ModifyDelegate(SortingInstructionBindingItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var bindingEditor = new SortingInstructionBindingEditor();

                if (bindingEditor.DataContext is SortingInstructionBindingEditorViewModel model) {
                    model.Identifier = "SortingInstructionBindingDialog";
                    model.SortingInstructionBindingItemInfo = obj;
                    model.SortingInstructionItems = obj.SortingInstructionItems;
                    await DialogHost.Show(bindingEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        SortingInstructionBindingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        bool canActive = true;
                        if (model.SortingInstructionBindingItemInfo.IsActive) {
                            var orDefault = await _sortingInstructionBindingRepository.FirstOrDefault(f =>
                                f.ExitId.Equals(model.SelectExitDefinitionInfo.Id) &&
                                f.IsActive);
                            canActive = orDefault is null;
                        }

                        //添加到数据库
                        var insertOrUpdate = await _sortingInstructionBindingRepository.Update(new SortingInstructionBindingInfoModel() {
                            CreateTime = DateTime.Now,
                            DelaySendMilliseconds = model.SortingInstructionBindingItemInfo.DelaySendMilliseconds,
                            ExitId = model.SelectExitDefinitionInfo.Id,
                            SendIntervalMilliseconds =
                                model.SortingInstructionBindingItemInfo.SendIntervalMilliseconds,
                            IsActive = canActive && model.SortingInstructionBindingItemInfo.IsActive,
                            ModifyTime = DateTime.Now,
                            Remarks = model.SortingInstructionBindingItemInfo.Remarks,
                            Id = model.SortingInstructionBindingItemInfo.Id,
                        });
                        if (insertOrUpdate) {
                            //取出数据库对应指令列表内容
                            var infoModels = await _sortingInstructionBindingRepository.SelectOrderByDescending(
                                s => s.ExitId.Equals(model.SelectExitDefinitionInfo.Id),
                                o => o.CreateTime);
                            var instructionBindingInfoModel = infoModels?.FirstOrDefault() ?? new SortingInstructionBindingInfoModel();
                            //取出model指令列表内容
                            var instructionInfoModels = model.SortingInstructionItems.Select(s => new SortingInstructionInfoModel {
                                CreateTime = s.CreateTime,
                                Remarks = s.Remarks,
                                Instruction = s.Instruction,
                                ReplyContent = s.ReplyContent,
                                InstructionBindingId = instructionBindingInfoModel?.Id ?? 0,
                                ModifyTime = s.ModifyTime,
                            })?.ToList();
                            //删除数据库指令列表对应内容
                            var sortingInstructionInfoModels = await _sortingInstructionRepository.Select(
                                s => s.InstructionBindingId.Equals(instructionBindingInfoModel.Id),
                                o => o.Id);
                            if (sortingInstructionInfoModels?.Any() == true) {
                                await _sortingInstructionRepository.DeleteRange(sortingInstructionInfoModels);
                            }

                            var insertRange = await _sortingInstructionRepository.InsertRange(instructionInfoModels ?? new List<SortingInstructionInfoModel>());
                            if (insertRange) {
                                SortingInstructionBindingMessageQueue.Enqueue("保存成功");
                                RefreshData();
                            }
                            else {
                                SortingInstructionBindingMessageQueue.Enqueue("保存失败");
                            }
                        }
                        else {
                            SortingInstructionBindingMessageQueue.Enqueue("保存失败");
                        }

                        //添加到指令列表对应内容
                    }
                }
            });
        }

        /// <summary>
        /// 删除
        /// </summary>
        public ICommand DeleteCommand {
            get => new DelegateCommand<SortingInstructionBindingItemInfoModel>(DeleteDelegate);
        }

        private async void DeleteDelegate(SortingInstructionBindingItemInfoModel obj) {
            var sortingInstructionBindingInfoModel = await _sortingInstructionBindingRepository.
                FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (sortingInstructionBindingInfoModel is not null) {
                var delete = await _sortingInstructionBindingRepository.Delete(sortingInstructionBindingInfoModel);
                if (delete) {
                    //刷新列表
                    RefreshData();
                }
            }
        }

        /// <summary>
        /// 发送指令
        /// </summary>
        public ICommand SendInstructionCommand {
            get => new DelegateCommand<SortingInstructionBindingItemInfoModel>(SendInstructionDelegate);
        }

        private void SendInstructionDelegate(SortingInstructionBindingItemInfoModel obj) {
            _inventoryManagementService.SendInstructions(new object(),
                obj.SortingInstructionItems.Select(s => s.Instruction)
                    ?.ToList() ?? new List<string>(),
                TimeSpan.FromMilliseconds(obj.SendIntervalMilliseconds),
                new InstructionsAttach() {
                    Timestamp = 0,
                });
        }

        /// <summary>
        /// 是否激活
        /// </summary>
        public ICommand ActiveCommand {
            get => new DelegateCommand<SortingInstructionBindingItemInfoModel>(ActiveDelegate);
        }

        private async void ActiveDelegate(SortingInstructionBindingItemInfoModel obj) {
            var model = await _sortingInstructionBindingRepository.FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (model is not null) {
                model.IsActive = obj.IsActive;
                var update = await _sortingInstructionBindingRepository.Update(model);
                if (!update) {
                    SortingInstructionBindingMessageQueue.Enqueue("保存失败");
                }
                return;
            }
            obj.IsActive = !obj.IsActive;
        }

        private async void RefreshData() {
            var loadingDialog = new LoadingDialog();
            if (loadingDialog.DataContext is not LoadingDialogViewModel model) return;
            await Application.Current.Dispatcher.InvokeAsync(() => {
                model.Identifier = "SortingInstructionBindingDialog";
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);
            var models = await _sortingInstructionBindingRepository.
                InstructionBindings(s => s.Id > 0);
            await Application.Current.Dispatcher.InvokeAsync(() => {
                SortingInstructionBindingItems.Clear();
                var infoModels = models?.Select((s, i) => new SortingInstructionBindingItemInfoModel {
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

                    SortingInstructionItems = new ObservableCollection<SortingInstructionItemInfoModel>(s.InstructionItems?.Select((s1, i1) => new SortingInstructionItemInfoModel {
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
                if (DialogHost.IsDialogOpen(model.Identifier)) {
                    DialogHost.Close(model.Identifier);
                }
            });
        }
    }
}