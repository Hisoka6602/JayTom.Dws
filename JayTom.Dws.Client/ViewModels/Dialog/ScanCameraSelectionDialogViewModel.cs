using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Models.Cameras;

namespace JayTom.Dws.Client.ViewModels.Dialog {

    public class ScanCameraSelectionDialogViewModel : BindableBase, IDialogAware {
        private ObservableCollection<CameraFinderItemInfoModel> _cameras = new();
        private CameraFinderItemInfoModel _selectedCamera = new();

        public ObservableCollection<CameraFinderItemInfoModel> Cameras {
            get => _cameras;
            set => SetProperty(ref _cameras, value);
        }

        public CameraFinderItemInfoModel SelectedCamera {
            get => _selectedCamera;
            set => SetProperty(ref _selectedCamera, value);
        }

        public bool CanCloseDialog() {
            return true;
        }

        public void OnDialogClosed() {
        }

        public void OnDialogOpened(IDialogParameters parameters) {
            var infoModels = parameters.GetValue<ObservableCollection<CameraFinderItemInfoModel>>("Cameras")?
                .Where(w => w is { HasBinding: true, BoundType: CameraBindingType.OcrCamera or CameraBindingType.ScannerCamera })
                ?.ToList();
            Cameras.AddRange(infoModels);
        }

        public ICommand CloseCommand {
            get => new DelegateCommand<object>(CloseDelegate);
        }

        private void CloseDelegate(object obj) {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        public ICommand OkCommand {
            get => new DelegateCommand<object>(OkDelegate);
        }

        private void OkDelegate(object obj) {
            OnRequestClose(new DialogResult(ButtonResult.OK, new DialogParameters()
            {
                {"SelectedCamera",SelectedCamera},
            }));
        }

        public string Title => "全景绑定";

        public event Action<IDialogResult>? RequestClose;

        protected virtual void OnRequestClose(IDialogResult obj) {
            RequestClose?.Invoke(obj);
        }
    }
}