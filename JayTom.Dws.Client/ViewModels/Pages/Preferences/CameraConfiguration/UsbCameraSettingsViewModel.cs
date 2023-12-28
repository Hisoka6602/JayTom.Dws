using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.Cameras;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class UsbCameraSettingsViewModel : BindableBase {
        private BarcodeReaderSettingsInfoModel _barcodeReaderSettingsInfo = new();
        private UsbCameraSettingsInfoModel _usbCameraSettingsInfo = new();

        public BarcodeReaderSettingsInfoModel BarcodeReaderSettingsInfo {
            get => _barcodeReaderSettingsInfo;
            set => SetProperty(ref _barcodeReaderSettingsInfo, value);
        }

        public UsbCameraSettingsInfoModel UsbCameraSettingsInfo {
            get => _usbCameraSettingsInfo;
            set => SetProperty(ref _usbCameraSettingsInfo, value);
        }
    }
}