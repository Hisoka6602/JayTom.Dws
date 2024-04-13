using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Regions;
using Prism.Commands;
using System.Windows;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using JayTom.Dws.Plugin;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.Models.PackageSorting.PackageExitLockModels;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    public class PackageExitLockSettingsViewModel : SettingsPageTemplateViewModel {
        private readonly IPackageExitLockBindingRepository _packageExitLockBindingRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;

        private readonly IExcel _excel;
        private readonly IDeviceService _deviceService;
        private readonly IExitMonitor _exitMonitor;
        private ObservableCollection<PackageExitLockBindingItemInfoModel> _packageExitLockBindingItems = new();
        private PackageExitLockSettingsModel _packageExitLockSettings = new();
        private ObservableCollection<LockProtocolType> _lockProtocolTypeItems = new(Enum.GetValues(typeof(LockProtocolType)).Cast<LockProtocolType>());
        private bool _isLoaded;

        public PackageExitLockSettingsViewModel(IPackageExitLockBindingRepository packageExitLockBindingRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IConfigRepository configRepository, IExcel excel,
            IDeviceService deviceService, IExitMonitor exitMonitor) : base(configRepository) {
            _packageExitLockBindingRepository = packageExitLockBindingRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;

            _excel = excel;
            _deviceService = deviceService;
            _exitMonitor = exitMonitor;
            //锁格事件回调
            _exitMonitor.LockExitEvent += async (sender, model) => {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var packageExitLockBindingItemInfoModel = PackageExitLockBindingItems.FirstOrDefault(f => f.ExitName.Equals(model.ExitName));
                    if (packageExitLockBindingItemInfoModel is not null) {
                        packageExitLockBindingItemInfoModel.CurrentStatus = ExitLockStatus.Lock;
                    }
                });
            };
            _exitMonitor.UnLockExitEvent += async (sender, model) => {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var packageExitLockBindingItemInfoModel = PackageExitLockBindingItems.FirstOrDefault(f => f.ExitName.Equals(model.ExitName));
                    if (packageExitLockBindingItemInfoModel is not null) {
                        packageExitLockBindingItemInfoModel.CurrentStatus = ExitLockStatus.Unlock;
                    }
                });
            };
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

        public override async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    var packageExitLockSettingsDto = await _configRepository.FirstOrDefaultEntity<PackageExitLockSettingsDto>(SettingsName);
                    if (packageExitLockSettingsDto is not null) {
                        PackageExitLockSettings = new PackageExitLockSettingsModel() {
                            IsUsePackageExitLock = packageExitLockSettingsDto.IsUsePackageExitLock,
                            ProtocolType = packageExitLockSettingsDto.ProtocolType,
                            S7Config = new S7ConfigModel() {
                                Db = packageExitLockSettingsDto.S7Config.Db,
                                Ip = packageExitLockSettingsDto.S7Config.Ip,
                                Slot = packageExitLockSettingsDto.S7Config.Slot,
                                Rack = packageExitLockSettingsDto.S7Config.Rack,
                                Timeout = packageExitLockSettingsDto.S7Config.Timeout
                            }
                        };
                    }
                    else {
                        base.MessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("加载设置失败") ?? string.Empty}");
                    }
                });
            }
            RefreshData();
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
                        base.MessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk) {
                        //保存到数据库
                        var packageExitLockBindingInfoModel = new PackageExitLockBindingInfoModel() {
                            Address = model.Address,
                            CreateTime = DateTime.Now,
                            ExitId = model.SelectExitDefinitionInfo.Id,
                            Length = model.Length,
                            LockingFlag = model.LockingFlag,
                            UnlockingFlag = model.UnlockingFlag,
                            ModifyTime = DateTime.Now,
                        };
                        //检查是否已存在相同格口
                        var bindingInfoModel = await _packageExitLockBindingRepository.FirstOrDefault(
                            s => s.ExitId.Equals(model.SelectExitDefinitionInfo.Id));
                        if (bindingInfoModel is not null) {
                            base.MessageQueue.Enqueue($"格口:{model.SelectExitDefinitionInfo.ExitName} 重复绑定");
                            return;
                        }

                        var insert = await _packageExitLockBindingRepository.Insert(packageExitLockBindingInfoModel);
                        if (insert) {
                            EventAggregator.Instance.Publish(packageExitLockBindingInfoModel);
                            EventAggregator.Instance.Publish(new SettingsChangedEvent {
                                SettingsName = "PackageExitLockBindingItemSettings",
                                IsLocallySaved = true
                            });
                            base.MessageQueue.Enqueue("保存成功");
                            //刷新列表
                            RefreshData();
                        }
                        else {
                            base.MessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            });
        }

        public ICommand ImportCommand {
            get => new DelegateCommand<object>(ImportDelegate);
        }

        private async void ImportDelegate(object obj) {
            //导入
            var openFileDialog = new Microsoft.Win32.OpenFileDialog() {
                Title = "Please select the file to import.",
                Filter = $"{Languages.Language.ResourceManager.GetString("Excel文件") ?? string.Empty}(xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            };
            if (openFileDialog.ShowDialog() == true) {
                var exportDialog = new ExportDialog();
                if (exportDialog.DataContext is ExportDialogViewModel model) {
                    model.FilePath = openFileDialog.FileName;
                    model.Identifier = "MainDialog";
                    model.Message = "Retrieving data...";
                    DialogHost.Show(exportDialog, model.Identifier);

                    var models = await _excel.ReadExcel<PackageExitLockBindingItemInfoModel>(openFileDialog.FileName, async p => {
                        model.Progress = p;
                        model.ProgressText = $"{p}%";
                        if (p == 100) {
                            await Application.Current.Dispatcher.InvokeAsync(() => {
                                if (DialogHost.IsDialogOpen(model.Identifier)) {
                                    DialogHost.Close(model.Identifier);
                                }
                            });
                        }
                    }, async e => {
                        await Application.Current.Dispatcher.InvokeAsync(() => {
                            if (DialogHost.IsDialogOpen(model.Identifier)) {
                                DialogHost.Close(model.Identifier);
                            }
                        });
                        base.MessageQueue?.Enqueue(e.Message);
                    });
                    await Task.Delay(500);
                    if (models?.Any() == true) {
                        var duplicateExitNames = models.GroupBy(g => g.ExitName)
                            .Where(g => g.Count() > 1)  // 选择具有重复键的组
                            .Select(g => g.Key)
                            .ToList();
                        if (duplicateExitNames.Any()) {
                            var join = string.Join(",", duplicateExitNames);
                            base.MessageQueue.Enqueue($"{join},重复了");
                            return;
                        }
                        //批量添加到数据库
                        var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);

                        var infoModels = models.Select(s => new PackageExitLockBindingInfoModel {
                            Address = s.Address,
                            CreateTime = s.CreateTime,
                            ModifyTime = s.ModifyTime,
                            CurrentStatus = s.CurrentStatus,
                            ExitId = packageExitDefinitionInfoModels.FirstOrDefault(f => f.ExitName.Equals(s.ExitName))?.Id ?? 0,
                            Length = s.Length,
                            LockingFlag = s.LockingFlag,
                            Remarks = s.Remarks,
                            UnlockingFlag = s.UnlockingFlag,
                        }).Where(w => !w.ExitId.Equals(0)).ToList();

                        var insertOrUpdate = await _packageExitLockBindingRepository.InsertOrUpdateRange(infoModels);
                        if (insertOrUpdate) {
                            EventAggregator.Instance.Publish(infoModels.FirstOrDefault());
                            base.MessageQueue.Enqueue("保存成功");
                            //刷新列表
                            RefreshData();
                            EventAggregator.Instance.Publish(new SettingsChangedEvent {
                                SettingsName = "PackageExitLockBindingItemSettings",
                                IsLocallySaved = true
                            });
                        }
                        else {
                            base.MessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            }
        }

        public ICommand ExportCommand {
            get => new DelegateCommand<object>(ExportDelegate);
        }

        private async void ExportDelegate(object obj) {
            //导出
            if (PackageExitLockBindingItems?.Any() != true) {
                base.MessageQueue?.Enqueue(Languages.Language.ResourceManager.GetString("列表中没有数据") ?? string.Empty);
                return;
            }

            //导出

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog() {
                Title = "Please select the location to save the file.",
                Filter = $"{Languages.Language.ResourceManager.GetString("Excel文件") ?? string.Empty}(xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            };
            if (saveFileDialog.ShowDialog() == true) {
                var exportDialog = new ExportDialog();
                if (exportDialog.DataContext is ExportDialogViewModel model) {
                    model.FilePath = saveFileDialog.FileName;
                    model.Identifier = "MainDialog";
                    model.Message = "Retrieving data...";
                    DialogHost.Show(exportDialog, model.Identifier);
                    var export = await _excel.Export(saveFileDialog.FileName,
                        $"锁格配置列表",
                        "锁格配置列表", PackageExitLockBindingItems?.ToList() ?? new List<PackageExitLockBindingItemInfoModel>(),
                        new List<string>(), async p => {
                            model.Progress = p;
                            model.ProgressText = $"{p}%";
                            if (p == 100) {
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                                    if (DialogHost.IsDialogOpen(model.Identifier)) {
                                        DialogHost.Close(model.Identifier);
                                    }
                                });
                            }
                        }, e => {
                            base.MessageQueue?.Enqueue(e.Message);
                        });
                    if (!export) {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            if (DialogHost.IsDialogOpen(model.Identifier)) {
                                DialogHost.Close(model.Identifier);
                            }
                        });
                    }
                }
            }
        }

        public ICommand ModifyCommand {
            get => new DelegateCommand<PackageExitLockBindingItemInfoModel>(ModifyDelegate);
        }

        private async void ModifyDelegate(PackageExitLockBindingItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var packageExitLockEditor = new PackageExitLockEditor();
                if (packageExitLockEditor.DataContext is PackageExitLockEditorViewModel model) {
                    model.Identifier = "PackageExitLockDialog";
                    model.PackageExitLockBindingItemInfo = obj;
                    await DialogHost.Show(packageExitLockEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        base.MessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk) {
                        //更新到数据库
                        var packageExitLockBindingInfoModel = new PackageExitLockBindingInfoModel() {
                            Address = model.Address,
                            CreateTime = DateTime.Now,
                            ExitId = model.SelectExitDefinitionInfo.Id,
                            Length = model.Length,
                            LockingFlag = model.LockingFlag,
                            UnlockingFlag = model.UnlockingFlag,
                            ModifyTime = DateTime.Now,
                            Id = model.PackageExitLockBindingItemInfo.Id
                        };

                        var update = await _packageExitLockBindingRepository.Update(packageExitLockBindingInfoModel);
                        if (update) {
                            EventAggregator.Instance.Publish(packageExitLockBindingInfoModel);
                            base.MessageQueue.Enqueue("保存成功");
                            //刷新列表
                            RefreshData();
                            EventAggregator.Instance.Publish(new SettingsChangedEvent {
                                SettingsName = "PackageExitLockBindingItemSettings",
                                IsLocallySaved = true
                            });
                        }
                        else {
                            base.MessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            });
        }

        public ICommand DeleteCommand {
            get => new DelegateCommand<PackageExitLockBindingItemInfoModel>(DeleteDelegate);
        }

        private async void DeleteDelegate(PackageExitLockBindingItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var model = await _packageExitLockBindingRepository.
                    FirstOrDefault(f =>
                        f.ExitId.Equals(obj.ExitId));
                if (model is not null) {
                    var delete = await _packageExitLockBindingRepository.Delete(model);
                    if (delete) {
                        RefreshData();
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "PackageExitLockBindingItemSettings",
                            IsLocallySaved = true
                        });
                    }
                }
            });
        }

        public override string Identifier => "PackageExitLockSettingsDialogHost";
        public override string SettingsName => "PackageExitLockSettings";

        protected override async Task<bool> SaveSettingsProcess() {
            if (_deviceService.RunningStatus) {
                IsSavingInProgress = false;
                MessageQueue.Enqueue($"设备工作中,无法设置");
                return false;
            }
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new PackageExitLockSettingsDto() {
                    IsUsePackageExitLock = PackageExitLockSettings.IsUsePackageExitLock,
                    ProtocolType = PackageExitLockSettings.ProtocolType,
                    S7Config = new S7ConfigDto {
                        Db = PackageExitLockSettings.S7Config.Db,
                        Ip = PackageExitLockSettings.S7Config.Ip,
                        Slot = PackageExitLockSettings.S7Config.Slot,
                        Rack = PackageExitLockSettings.S7Config.Rack,
                        Timeout = PackageExitLockSettings.S7Config.Timeout
                    }
                })
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return true;
        }

        private async void RefreshData() {
            var loadingDialog = new LoadingDialog();
            if (loadingDialog.DataContext is not LoadingDialogViewModel model) return;
            await Application.Current.Dispatcher.InvokeAsync(() => {
                model.Identifier = "PackageExitLockListViewDialog";
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.CreateTime);

            var infoModels = await _packageExitLockBindingRepository.Select(s => s.Id > 0,
                o => o.Id);
            var (key, value) = await _exitMonitor.GetAllPackageExitStatus();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                PackageExitLockBindingItems.Clear();

                if (infoModels?.Any() == true) {
                    var packageExitLockBindingItemInfoModels = infoModels.Select((s, i) => new PackageExitLockBindingItemInfoModel {
                        Address = s.Address,
                        CreateTime = s.CreateTime,
                        ModifyTime = s.ModifyTime,
                        CurrentStatus = value.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.IsLockExit == true ? ExitLockStatus.Lock : ExitLockStatus.Unlock,
                        ExitId = s.ExitId,
                        ExitName =
                            packageExitDefinitionInfoModels.FirstOrDefault(f => f.Id.Equals(s.ExitId))?.ExitName ??
                            string.Empty,
                        Id = s.Id,
                        Length = s.Length,
                        LockingFlag = s.LockingFlag,
                        Num = i + 1,
                        Remarks = s.Remarks,
                        UnlockingFlag = s.UnlockingFlag,
                    })?.ToList() ?? new List<PackageExitLockBindingItemInfoModel>();
                    PackageExitLockBindingItems.AddRange(packageExitLockBindingItemInfoModels);
                }
                if (DialogHost.IsDialogOpen(model.Identifier)) {
                    DialogHost.Close(model.Identifier);
                }
            });
        }
    }
}