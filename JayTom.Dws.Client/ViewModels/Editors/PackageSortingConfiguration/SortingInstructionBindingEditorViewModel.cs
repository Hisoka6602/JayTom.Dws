using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Application.PackageExits;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration
{

    public class SortingInstructionBindingEditorViewModel : BindableBase
    {
        private readonly IPackageExitCatalog _packageExitCatalog;
        private string _identifier = string.Empty;
        private string _exceptionContent = string.Empty;
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();

        private ObservableCollection<SortingInstructionItemInfoModel> _sortingInstructionItems = new();

        private string _instruction = string.Empty;
        private SortingInstructionBindingItemInfoModel _sortingInstructionBindingItemInfo = new();
        private PackageExitDefinitionItemInfoModel _selectExitDefinitionInfo = new();
        private bool _isOk;
        private string _replyContent = string.Empty;

        public SortingInstructionBindingEditorViewModel(IPackageExitCatalog packageExitCatalog)
        {
            _packageExitCatalog = packageExitCatalog;
        }

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        /// <summary>
        /// 异常内容
        /// </summary>
        public string ExceptionContent
        {
            get => _exceptionContent;
            set => SetProperty(ref _exceptionContent, value);
        }

        /// <summary>
        /// 格口列表
        /// </summary>
        public ObservableCollection<PackageExitDefinitionItemInfoModel> PackageExitDefinitionItems
        {
            get => _packageExitDefinitionItems;
            set => SetProperty(ref _packageExitDefinitionItems, value);
        }

        public PackageExitDefinitionItemInfoModel SelectExitDefinitionInfo
        {
            get => _selectExitDefinitionInfo;
            set => SetProperty(ref _selectExitDefinitionInfo, value);
        }

        /// <summary>
        /// 指令列表
        /// </summary>
        public ObservableCollection<SortingInstructionItemInfoModel> SortingInstructionItems
        {
            get => _sortingInstructionItems;
            set => SetProperty(ref _sortingInstructionItems, value);
        }

        public SortingInstructionBindingItemInfoModel SortingInstructionBindingItemInfo
        {
            get => _sortingInstructionBindingItemInfo;
            set => SetProperty(ref _sortingInstructionBindingItemInfo, value);
        }

        public bool IsOk
        {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        /// <summary>
        /// 指令
        /// </summary>
        public string Instruction
        {
            get => _instruction;
            set => SetProperty(ref _instruction, value);
        }

        /// <summary>
        /// 应答内容
        /// </summary>
        public string ReplyContent
        {
            get => _replyContent;
            set => SetProperty(ref _replyContent, value);
        }

        /// <summary>
        /// 删除指令
        /// </summary>
        public ICommand DeleteInstructionCommand => new DelegateCommand<SortingInstructionItemInfoModel>(DeleteInstructionDelegate);

        private async void DeleteInstructionDelegate(SortingInstructionItemInfoModel obj)
        {
            await UiThread.Dispatcher.InvokeAsync(() =>
            {
                SortingInstructionItems.Remove(obj);
                //调整Num
                if (SortingInstructionItems?.Any() == true)
                {
                    for (int i = 0; i < SortingInstructionItems.Count; i++)
                    {
                        SortingInstructionItems[i].Num = i + 1;
                    }
                }
            });
        }

        /// <summary>
        /// 添加指令
        /// </summary>
        public ICommand AddInstructionCommand
        {
            get => new DelegateCommand<object>(AddInstructionDelegate);
        }

        private async void AddInstructionDelegate(object obj)
        {
            //添加指令
            await UiThread.Dispatcher.InvokeAsync(() =>
            {
                if (SortingInstructionItems?.Any(a => a.Instruction.Equals(Instruction)) != true)
                {
                    SortingInstructionItems?.Add(new SortingInstructionItemInfoModel()
                    {
                        CreateTime = DateTime.Now,
                        Instruction = Instruction,
                        ReplyContent = ReplyContent,
                        InstructionBindingId = SortingInstructionBindingItemInfo.Id,
                        ModifyTime = DateTime.Now,
                        Remarks = SortingInstructionBindingItemInfo.Remarks,
                        Num = SortingInstructionItems.Count + 1
                    });
                }
            });
        }

        /// <summary>
        /// 加载完成
        /// </summary>
        public ICommand LoadedCommand
        {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj)
        {
            var packageExitDefinitionInfoModels = await _packageExitCatalog.ListAsync();

            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                PackageExitDefinitionItems.Clear();
                var packageExitDefinitionItemInfoModels = packageExitDefinitionInfoModels?.Select((s, i) => new PackageExitDefinitionItemInfoModel
                {
                    CreateTime = s.CreateTime,
                    ExitName = $"{s.ExitName}{(s.IsActive ? "" : "(未生效)")}",
                    Id = s.Id,
                    IsActive = s.IsActive,
                    ModifyTime = s.ModifyTime,
                    Num = i + 1,
                    Remarks = s.Remarks,
                    Type = s.Type
                })?.ToList();

                if (packageExitDefinitionItemInfoModels?.Any() == true)
                {
                    PackageExitDefinitionItems.AddRange(packageExitDefinitionItemInfoModels);
                    var packageExitDefinitionItemInfoModel = PackageExitDefinitionItems.FirstOrDefault(f =>
                        f.Id.Equals(SortingInstructionBindingItemInfo.ExitId));
                    SelectExitDefinitionInfo = packageExitDefinitionItemInfoModel ?? new PackageExitDefinitionItemInfoModel();
                }
            });
        }

        /// <summary>
        /// 保存方法
        /// </summary>
        public ICommand SaveCommand
        {
            get => new DelegateCommand(SaveDelegate);
        }

        private void SaveDelegate()
        {
            //保存方法
            try
            {
                IsOk = (SortingInstructionItems.Any() && SelectExitDefinitionInfo.Id > 0);
                Pitcher.Throw.ArgumentNull.WhenNull(SortingInstructionBindingItemInfo, nameof(SortingInstructionBindingItemInfo));
                Pitcher.Throw.ArgumentNull.WhenNull(SelectExitDefinitionInfo.Type, nameof(SelectExitDefinitionInfo.Type));
            }
            catch (Exception e)
            {
                IsOk = false;
                ExceptionContent = e.Message;
            }

            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }

        /// <summary>
        /// 取消方法
        /// </summary>
        public ICommand CancelCommand
        {
            get => new DelegateCommand(CancelDelegate);
        }

        private void CancelDelegate()
        {
            //取消方法
            IsOk = false;
            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }
    }
}
