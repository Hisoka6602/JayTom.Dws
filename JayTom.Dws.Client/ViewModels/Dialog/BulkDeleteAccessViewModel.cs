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

namespace JayTom.Dws.Client.ViewModels.Dialog {

    public class BulkDeleteAccessViewModel : BindableBase {
        private string _identifier = string.Empty;
        private bool _isOk;
        private string _tipContent = "是否删除内容?";

        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public bool IsOk {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        public string TipContent {
            get => _tipContent;
            set => SetProperty(ref _tipContent, value);
        }

        public ICommand DeleteCommand => new DelegateCommand<object>(DeleteDelegate);

        private void DeleteDelegate(object obj) {
            IsOk = true;

            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand CancelCommand => new DelegateCommand(CancelDelegate);

        private void CancelDelegate() {
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }
    }
}