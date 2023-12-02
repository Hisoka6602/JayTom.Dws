using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using JayTom.Dws.Camera;
using System.Windows.Input;
using Prism.Services.Dialogs;

namespace JayTom.Dws.Client.ViewModels.Dialog {
    public class TriggerModeSelectionViewModel : BindableBase, IDialogAware {
        private TriggerMode? _cameraTriggerMode;
        private int _sourceLine;
        private bool _isShowSourceLine;

        public TriggerMode? CameraTriggerMode {
            get => _cameraTriggerMode;
            set => SetProperty(ref _cameraTriggerMode, value);
        }

        public int SourceLine {
            get => _sourceLine;
            set => SetProperty(ref _sourceLine, value);
        }

        public bool IsShowSourceLine {
            get => _isShowSourceLine;
            set => SetProperty(ref _isShowSourceLine, value);
        }

        public bool CanCloseDialog() {
            return true;
        }

        public void OnDialogClosed() {
        }

        public void OnDialogOpened(IDialogParameters parameters) {
            var value = parameters.GetValue<string>("Brand");

            if (value?.Contains("Hik") == true) {
                IsShowSourceLine = true;
            }
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