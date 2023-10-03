using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    //包裹出口定义页面
    public class PackageExitDefinitionViewModel : BindableBase {
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();

        public PackageExitDefinitionViewModel() {
            _packageExitDefinitionItems = new()
            {
                new PackageExitDefinitionItemInfoModel()
                {
                    CreateTime = DateTime.Today,
                    ExitName = "格口1",
                    Id = 1,
                    IsActive = true,
                    ModifyTime = DateTime.Now,
                    Num = 1,
                    Remarks = "这是备注",
                    Type = ExitType.PackageExit
                },
                new PackageExitDefinitionItemInfoModel()
                {
                    CreateTime = DateTime.Today,
                    ExitName = "异常口",
                    Id = 2,
                    IsActive = true,
                    ModifyTime = DateTime.Now,
                    Num = 2,
                    Remarks = "这是异常口",
                    Type = ExitType.AbnormalExit
                }
            };
        }

        public ObservableCollection<PackageExitDefinitionItemInfoModel> PackageExitDefinitionItems {
            get => _packageExitDefinitionItems;
            set => SetProperty(ref _packageExitDefinitionItems, value);
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
                }
            }, DispatcherPriority.Background);
        }
    }
}