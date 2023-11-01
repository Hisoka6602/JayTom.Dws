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
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.Models.PackageSorting;
using Application = System.Windows.Application;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    //包裹出口定义页面
    public class PackageExitDefinitionViewModel : BindableBase {
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly IExcel _excel;
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();
        private SnackbarMessageQueue _packageExitDefinitionMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public PackageExitDefinitionViewModel(IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IExcel excel) {
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _excel = excel;
        }

        public ObservableCollection<PackageExitDefinitionItemInfoModel> PackageExitDefinitionItems {
            get => _packageExitDefinitionItems;
            set => SetProperty(ref _packageExitDefinitionItems, value);
        }

        public SnackbarMessageQueue PackageExitDefinitionMessageQueue {
            get => _packageExitDefinitionMessageQueue;
            set => SetProperty(ref _packageExitDefinitionMessageQueue, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var packageExitDefinitionEditor = new PackageExitDefinitionEditor();
                if (packageExitDefinitionEditor.DataContext is PackageExitDefinitionEditorViewModel model) {
                    model.Identifier = "PackageExitDefinitionDialog";
                    await DialogHost.Show(packageExitDefinitionEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        PackageExitDefinitionMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        //保存到数据库
                        var packageExitDefinitionInfoModel = new PackageExitDefinitionInfoModel() {
                            CreateTime = DateTime.Now,
                            ExitName = model.ExitName,
                            IsActive = model.IsActive,
                            ModifyTime = DateTime.Now,
                            Remarks = model.Remarks,
                            Type = model.Type,
                        };
                        var insertOrUpdate = await _packageExitDefinitionRepository.Insert(packageExitDefinitionInfoModel);
                        if (insertOrUpdate) {
                            EventAggregator.Instance.Publish(packageExitDefinitionInfoModel);
                            PackageExitDefinitionMessageQueue.Enqueue("保存成功");
                            //刷新列表
                            RefreshData();
                        }
                        else {
                            PackageExitDefinitionMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            }, DispatcherPriority.Background);
        }

        public ICommand ModifyCommand {
            get => new DelegateCommand<PackageExitDefinitionItemInfoModel>(ModifyDelegate);
        }

        private async void ModifyDelegate(PackageExitDefinitionItemInfoModel obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var packageExitDefinitionEditor = new PackageExitDefinitionEditor();
                if (packageExitDefinitionEditor.DataContext is PackageExitDefinitionEditorViewModel model) {
                    model.Identifier = "PackageExitDefinitionDialog";
                    model.Type = obj.Type;
                    model.ExitName = obj.ExitName;
                    model.IsActive = obj.IsActive;
                    model.Id = obj.Id;
                    model.Remarks = obj.Remarks;
                    await DialogHost.Show(packageExitDefinitionEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        PackageExitDefinitionMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        //保存到数据库
                        var insertOrUpdate = await _packageExitDefinitionRepository.Update(new PackageExitDefinitionInfoModel() {
                            CreateTime = obj.CreateTime,
                            ExitName = model.ExitName,
                            IsActive = model.IsActive,
                            ModifyTime = DateTime.Now,
                            Remarks = model.Remarks,
                            Type = model.Type,
                            Id = model.Id,
                        });
                        if (insertOrUpdate) {
                            PackageExitDefinitionMessageQueue.Enqueue("保存成功");
                            //刷新列表
                            RefreshData();
                        }
                        else {
                            PackageExitDefinitionMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            }, DispatcherPriority.Background);
        }

        public ICommand DeleteCommand {
            get => new DelegateCommand<PackageExitDefinitionItemInfoModel>(DeleteDelegate);
        }

        private async void DeleteDelegate(PackageExitDefinitionItemInfoModel obj) {
            var model = await _packageExitDefinitionRepository.FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (model is not null) {
                var delete = await _packageExitDefinitionRepository.Delete(model);
                if (delete) {
                    //刷新列表
                    RefreshData();
                }
            }
        }

        public ICommand ActiveCommand {
            get => new DelegateCommand<PackageExitDefinitionItemInfoModel>(ActiveDelegate);
        }

        private async void ActiveDelegate(PackageExitDefinitionItemInfoModel obj) {
            var model = await _packageExitDefinitionRepository.FirstOrDefault(f => f.Id.Equals(obj.Id));
            if (model is not null) {
                model.IsActive = obj.IsActive;
                var update = await _packageExitDefinitionRepository.Update(model);
                if (!update) {
                    PackageExitDefinitionMessageQueue.Enqueue("保存失败");
                }
                return;
            }
            obj.IsActive = !obj.IsActive;
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

        private async void RefreshData() {
            var loadingDialog = new LoadingDialog();
            if (loadingDialog.DataContext is not LoadingDialogViewModel model) return;
            await Application.Current.Dispatcher.InvokeAsync(() => {
                model.Identifier = "PackageExitDefinitionDialog";
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            var models = await _packageExitDefinitionRepository.
                Select(s => s.Id > 0,
                    o => o.ModifyTime);

            await Application.Current.Dispatcher.InvokeAsync(() => {
                PackageExitDefinitionItems.Clear();
                var infoModels = models?.Select((s, i) => new PackageExitDefinitionItemInfoModel {
                    CreateTime = s.CreateTime,
                    ExitName = s.ExitName,
                    Id = s.Id,
                    IsActive = s.IsActive,
                    ModifyTime = s.ModifyTime,
                    Num = i + 1,
                    Remarks = s.Remarks,
                    Type = s.Type,
                })?.ToList();
                PackageExitDefinitionItems.AddRange(infoModels);
                if (DialogHost.IsDialogOpen(model.Identifier)) {
                    DialogHost.Close(model.Identifier);
                }
            });
        }

        /// <summary>
        /// 导出
        /// </summary>
        public ICommand ExportCommand {
            get => new DelegateCommand<PackageExitDefinitionItemInfoModel>(ExportDelegate);
        }

        private async void ExportDelegate(PackageExitDefinitionItemInfoModel obj) {
            //导出
            if (PackageExitDefinitionItems?.Any() != true) {
                PackageExitDefinitionMessageQueue?.Enqueue(Languages.Language.ResourceManager.GetString("列表中没有数据") ?? string.Empty);
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
                        $"定义格口列表",
                        "格口列表", PackageExitDefinitionItems?.ToList() ?? new List<PackageExitDefinitionItemInfoModel>(),
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
                            PackageExitDefinitionMessageQueue?.Enqueue(e.Message);
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

        public ICommand ImportCommand {
            get => new DelegateCommand<PackageExitDefinitionItemInfoModel>(ImportDelegate);
        }

        private async void ImportDelegate(PackageExitDefinitionItemInfoModel obj) {
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

                    var models = await _excel.ReadExcel<PackageExitDefinitionItemInfoModel>(openFileDialog.FileName, async p => {
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
                        PackageExitDefinitionMessageQueue?.Enqueue(e.Message);
                    });
                    await Task.Delay(500);
                    if (models?.Any() == true) {
                        //批量添加到数据库
                        var infoModels = models.Select(s => new PackageExitDefinitionInfoModel {
                            CreateTime = DateTime.Now,
                            ExitName = s.ExitName,
                            IsActive = s.IsActive,
                            ModifyTime = DateTime.Now,
                            Remarks = s.Remarks,
                            Type = s.Type,
                        }).ToList();

                        var insertOrUpdate = await _packageExitDefinitionRepository.InsertRange(infoModels);
                        if (insertOrUpdate) {
                            EventAggregator.Instance.Publish(infoModels.FirstOrDefault());
                            PackageExitDefinitionMessageQueue.Enqueue("保存成功");
                            //刷新列表
                            RefreshData();
                        }
                        else {
                            PackageExitDefinitionMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            }
        }
    }
}