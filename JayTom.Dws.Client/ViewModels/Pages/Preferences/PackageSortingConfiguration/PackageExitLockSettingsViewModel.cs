using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using System.Collections.ObjectModel;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.Models.PackageSorting.PackageExitLockModels;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    public class PackageExitLockSettingsViewModel : BindableBase {
        private readonly IPackageExitLockBindingRepository _packageExitLockBindingRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly IConfigRepository _configRepository;
        private ObservableCollection<PackageExitLockBindingItemInfoModel> _packageExitLockBindingItems = new();
        private PackageExitLockSettingsModel _packageExitLockSettings = new();
        private ObservableCollection<LockProtocolType> _lockProtocolTypeItems = new(Enum.GetValues(typeof(LockProtocolType)).Cast<LockProtocolType>());
        private bool _isLoaded;
        private SnackbarMessageQueue _packageExitLockSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isSavingInProgress;

        public PackageExitLockSettingsViewModel(IPackageExitLockBindingRepository packageExitLockBindingRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IConfigRepository configRepository) {
            _packageExitLockBindingRepository = packageExitLockBindingRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _configRepository = configRepository;
            for (var i = 0; i < 20; i++) {
                PackageExitLockBindingItems.Add(new PackageExitLockBindingItemInfoModel() {
                    Address = "10.23.01.01",
                    CreateTime = DateTime.Now,
                    CurrentStatus = (ExitLockStatus)(Enum.GetValues(typeof(ExitLockStatus)).GetValue(new Random().Next(Enum.GetValues(typeof(ExitLockStatus)).Length)) ?? ExitLockStatus.Lock),
                    ExitId = 1,
                    Length = 1,
                    LockingFlag = "0",
                    UnlockingFlag = "1",
                    ExitName = $"格口{i + 1}",
                    Remarks = $"这是备注{i + 1}",
                    Num = i + 1
                });
                //PackageExitLockSettings.ProtocolType
            }
        }

        public SnackbarMessageQueue PackageExitLockSettingsMessageQueue {
            get => _packageExitLockSettingsMessageQueue;
            set => SetProperty(ref _packageExitLockSettingsMessageQueue, value);
        }

        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        public ObservableCollection<PackageExitLockBindingItemInfoModel> PackageExitLockBindingItems {
            get => _packageExitLockBindingItems;
            set => SetProperty(ref _packageExitLockBindingItems, value);
        }

        public PackageExitLockSettingsModel PackageExitLockSettings {
            get => _packageExitLockSettings;
            set => SetProperty(ref _packageExitLockSettings, value);
        }

        public ObservableCollection<LockProtocolType> LockProtocolTypeItems {
            get => _lockProtocolTypeItems;
            set => SetProperty(ref _lockProtocolTypeItems, value);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj) {
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var packageExitLockEditor = new PackageExitLockEditor();
                if (packageExitLockEditor.DataContext is PackageExitLockEditorViewModel model) {
                    model.Identifier = "PackageExitLockDialog";
                    await DialogHost.Show(packageExitLockEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        PackageExitLockSettingsMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                }
            });
        }

        public ICommand ImportCommand {
            get => new DelegateCommand<object>(ImportDelegate);
        }

        private void ImportDelegate(object obj) {
        }

        public ICommand ExportCommand {
            get => new DelegateCommand<object>(ExportDelegate);
        }

        private void ExportDelegate(object obj) {
        }

        public ICommand ModifyCommand {
            get => new DelegateCommand<PackageExitLockBindingItemInfoModel>(ModifyDelegate);
        }

        private void ModifyDelegate(PackageExitLockBindingItemInfoModel obj) {
        }

        public ICommand DeleteCommand {
            get => new DelegateCommand<PackageExitLockBindingItemInfoModel>(DeleteDelegate);
        }

        private void DeleteDelegate(PackageExitLockBindingItemInfoModel obj) {
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public ICommand SaveSettingsCommand {
            get => new DelegateCommand<object>(SaveSettingDelegate);
        }

        private void SaveSettingDelegate(object obj) {
        }
    }
}