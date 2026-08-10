using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Mvc.Filters;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration
{
    public class PackageExitLockEditorViewModel : BindableBase
    {
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private string _identifier = string.Empty;
        private string _exceptionContent = string.Empty;
        private PackageExitLockBindingItemInfoModel _packageExitLockBindingItemInfo = new();
        private bool _isOk;
        private string _address = string.Empty;
        private int _length;
        private string _lockingFlag = string.Empty;
        private string _unlockingFlag = string.Empty;
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();
        private PackageExitDefinitionItemInfoModel _selectExitDefinitionInfo = new();

        public PackageExitLockEditorViewModel(IPackageExitDefinitionRepository packageExitDefinitionRepository)
        {
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
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

        public PackageExitLockBindingItemInfoModel PackageExitLockBindingItemInfo
        {
            get => _packageExitLockBindingItemInfo;
            set => SetProperty(ref _packageExitLockBindingItemInfo, value);
        }

        public bool IsOk
        {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

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
        /// 地址
        /// </summary>
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        /// <summary>
        /// 长度
        /// </summary>
        public int Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        /// <summary>
        /// 锁格标识
        /// </summary>
        public string LockingFlag
        {
            get => _lockingFlag;
            set => SetProperty(ref _lockingFlag, value);
        }

        /// <summary>
        /// 解锁标识
        /// </summary>
        public string UnlockingFlag
        {
            get => _unlockingFlag;
            set => SetProperty(ref _unlockingFlag, value);
        }

        public ICommand LoadedCommand
        {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj)
        {
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                o => o.CreateTime);

            await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
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
                        f.Id.Equals(PackageExitLockBindingItemInfo.ExitId));
                    SelectExitDefinitionInfo = packageExitDefinitionItemInfoModel ?? new PackageExitDefinitionItemInfoModel();
                }
                Address = PackageExitLockBindingItemInfo.Address;
                Length = PackageExitLockBindingItemInfo.Length;
                LockingFlag = PackageExitLockBindingItemInfo.LockingFlag;
                UnlockingFlag = PackageExitLockBindingItemInfo.UnlockingFlag;
            });
        }

        public ICommand SaveCommand
        {
            get => new DelegateCommand(SaveDelegate);
        }

        private void SaveDelegate()
        {
            //检查参数
            try
            {
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(Address, "地址不能为空");
                Pitcher.Throw.ArgumentOutOfRange.WhenLessThan(Length, 1, "长度不能小于1");
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(LockingFlag, "锁格标识不能为空");
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(UnlockingFlag, "解锁标识不能为空");
                IsOk = true;
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

        public ICommand CancelCommand
        {
            get => new DelegateCommand(CancelDelegate);
        }

        private void CancelDelegate()
        {
            IsOk = false;
            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }
    }
}