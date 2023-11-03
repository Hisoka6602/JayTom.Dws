using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Windows.Controls;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.DataModels;

namespace JayTom.Dws.Client.ViewModels.Dialog {
    public class BarCodeDetailsDialogViewModel : BindableBase, IDialogAware {
        private BarCodeItemModel _barCodeItem = new();

        public BarCodeItemModel BarCodeItem {
            get => _barCodeItem;
            set => SetProperty(ref _barCodeItem, value);
        }

        public bool CanCloseDialog() {
            return true;
        }

        public void OnDialogClosed() {
        }

        public void OnDialogOpened(IDialogParameters parameters) {
            foreach (Window window in Application.Current.Windows) {
                if (window.Name.Equals("BarCodeDetailsDialog")) {
                    window.Close();
                }
            }
            BarCodeItem = parameters.GetValue<BarCodeItemModel>("BarCodeItem");
        }

        public string Title => "包裹详情";

        public event Action<IDialogResult>? RequestClose;

        public ICommand CloseWinCommand {
            get => new DelegateCommand<object>(CloseWinDelegate);
        }

        private void CloseWinDelegate(object obj) {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }
        public ICommand LoadedCommand {
            get => new DelegateCommand<UserControl>(LoadedDelegate);
        }

        private void LoadedDelegate(UserControl obj) {
            var dialogWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            if (dialogWindow is not null) {
                dialogWindow.Owner = null;
                dialogWindow.Name = "BarCodeDetailsDialog";
            }
        }
    }
}