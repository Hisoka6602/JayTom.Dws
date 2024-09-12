using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Collections.Generic;

namespace JayTom.Dws.Client.ViewModels.Dialog.UpdaterDialog {

    public class UpgradeProgressViewModel : BindableBase, IDialogAware {

        public bool CanCloseDialog() {
            return false;
        }

        public void OnDialogClosed() {
        }

        public void OnDialogOpened(IDialogParameters parameters) {
        }

        public string Title { get; }

        public event Action<IDialogResult>? RequestClose;
    }
}