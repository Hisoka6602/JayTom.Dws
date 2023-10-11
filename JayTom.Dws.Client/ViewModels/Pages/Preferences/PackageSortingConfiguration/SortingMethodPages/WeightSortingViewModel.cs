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
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages {

    public class WeightSortingViewModel : BindableBase {
        private SnackbarMessageQueue _weightSortingMessageQueue = new(TimeSpan.FromSeconds(2));

        private ObservableCollection<WeightSortingItemInfoModel> _weightSortingItems = new()
        {
            new WeightSortingItemInfoModel()
            {
                CreateTime = DateTime.Now,
                ExitId = 1,
                ExitName = "aa",
                ModifyTime = DateTime.Now,
                Num = 1,
                Remarks = "备注",
                SortingRuleGroup = "121212",
            }
        };

        public SnackbarMessageQueue WeightSortingMessageQueue {
            get => _weightSortingMessageQueue;
            set => SetProperty(ref _weightSortingMessageQueue, value);
        }

        public ObservableCollection<WeightSortingItemInfoModel> WeightSortingItems {
            get => _weightSortingItems;
            set => SetProperty(ref _weightSortingItems, value);
        }

        public ICommand AddCommand {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj) {
            //WeightSortingRuleEditor

            await Application.Current.Dispatcher.InvokeAsync(async () => {
                var weightSortingRuleEditor = new WeightSortingRuleEditor();
                if (weightSortingRuleEditor.DataContext is WeightSortingRuleEditorViewModel model) {
                    model.Identifier = "WeightSortingDialog";
                    await DialogHost.Show(weightSortingRuleEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent)) {
                        WeightSortingMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }
                    if (model.IsOk) {
                        //添加到数据库
                    }
                }
            });
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadeDelegate);
        }

        private void LoadeDelegate(object obj) {
        }

        public ICommand ModifyCommand {
            get => new DelegateCommand<WeightSortingItemInfoModel>(ModifyDelegate);
        }

        private void ModifyDelegate(WeightSortingItemInfoModel obj) {
        }

        public ICommand DeleteCommand {
            get => new DelegateCommand<WeightSortingItemInfoModel>(DeleteDelegate);
        }

        private void DeleteDelegate(WeightSortingItemInfoModel obj) {
        }

        private async void RefreshData() {
        }
    }
}