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
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages {
    public class OcrSortingViewModel : BindableBase {
        private ObservableCollection<OcrSortingItemInfoModel> _ocrSortingItems = new();
        private SnackbarMessageQueue _ocrSortingMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public ObservableCollection<OcrSortingItemInfoModel> OcrSortingItems {
            get => _ocrSortingItems;
            set => SetProperty(ref _ocrSortingItems, value);
        }

        public SnackbarMessageQueue OcrSortingMessageQueue {
            get => _ocrSortingMessageQueue;
            set => SetProperty(ref _ocrSortingMessageQueue, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var ocrSortingRuleEditor = new OcrSortingRuleEditor();
                if (ocrSortingRuleEditor.DataContext is OcrSortingRuleEditorViewModel model) {
                    model.Identifier = "OcrSortingDialog";
                    await DialogHost.Show(ocrSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        OcrSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                }
            });
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj) {
        }

        public ICommand ModifyCommand {
            get => new DelegateCommand<OcrSortingItemInfoModel>(ModifyDelegate);
        }

        private void ModifyDelegate(OcrSortingItemInfoModel obj) {
            Console.WriteLine(11);
        }

        public ICommand DeleteCommand {
            get => new DelegateCommand<OcrSortingItemInfoModel>(DeleteDelegate);
        }

        private void DeleteDelegate(OcrSortingItemInfoModel obj) {
            Console.WriteLine(11);
        }

        private async void RefreshData() {
        }
    }
}