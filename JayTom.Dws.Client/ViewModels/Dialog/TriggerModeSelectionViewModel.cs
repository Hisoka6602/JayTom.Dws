using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;

namespace JayTom.Dws.Client.ViewModels.Dialog {

    public class TriggerModeSelectionViewModel : BindableBase, IDialogAware {
        private TriggerMode? _cameraTriggerMode;

        public TriggerMode? CameraTriggerMode {
            get => _cameraTriggerMode;
            set => SetProperty(ref _cameraTriggerMode, value);
        }

        public bool CanCloseDialog() {
            return true;
        }

        public void OnDialogClosed() {
        }

        public void OnDialogOpened(IDialogParameters parameters) {
        }

        public ICommand CloseCommand {
            get => new DelegateCommand<object>(CloseDelegate);
        }

        private void CloseDelegate(object obj) {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        public ICommand TriggerSelectionCommand {
            get => new DelegateCommand<object>(TriggerSelectionDelegate);
        }

        private void TriggerSelectionDelegate(object obj) {
            if (obj is string triggerString) {
                CameraTriggerMode = triggerString.Equals("SoftTrigger") ? TriggerMode.Software : TriggerMode.Hardware;
            }
            OnRequestClose(new DialogResult(ButtonResult.OK, new DialogParameters()
           {
                {"CameraTriggerMode",CameraTriggerMode},
           }));
        }

        public string Title => string.Empty;

        public event Action<IDialogResult>? RequestClose;

        protected virtual void OnRequestClose(IDialogResult obj) {
            RequestClose?.Invoke(obj);
        }
    }
}