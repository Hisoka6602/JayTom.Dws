using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    //物流代码识别页面
    public class LogisticsCodeRecognitionViewModel : BindableBase {
        private ObservableCollection<LogisticsCodeRecognitionItemInfoModel> _logisticsCodeRecognitionItems = new();

        public LogisticsCodeRecognitionViewModel() {
            _logisticsCodeRecognitionItems = new()
            {
                new LogisticsCodeRecognitionItemInfoModel()
                {
                    CreateTime = DateTime.Now,
                    ExitName ="格口3",
                    LogisticsCode = "YUNDA",
                    LogisticsName = "韵达",
                    ModifyTime = DateTime.Now,
                    Num = 1,
                    Remarks = "这是备注",
                }
            };
        }

        public ObservableCollection<LogisticsCodeRecognitionItemInfoModel> LogisticsCodeRecognitionItems {
            get => _logisticsCodeRecognitionItems;
            set => SetProperty(ref _logisticsCodeRecognitionItems, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var recognitionEditor = new LogisticsCodeRecognitionEditor();
                if (recognitionEditor.DataContext is LogisticsCodeRecognitionEditorViewModel model) {
                    model.Identifier = "LogisticsCodeRecognitionDialog";
                    await DialogHost.Show(recognitionEditor, model.Identifier);
                    /*if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        PackageExitDefinitionMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }*/
                }
            });
        }
    }
}