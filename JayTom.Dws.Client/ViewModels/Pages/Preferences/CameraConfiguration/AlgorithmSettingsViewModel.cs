using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Dto.CameraConfiguration;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class AlgorithmSettingsViewModel : SettingsPageTemplateViewModel {
        private BarcodeReaderSettingsInfoModel _barcodeReaderSettingsInfo = new();
        private ObservableCollection<int> _deblurLevelItems = new(Enumerable.Range(0, 10).ToList());
        private ObservableCollection<int> _textureDetectionSensitivityItems = new(Enumerable.Range(0, 10).ToList());

        public AlgorithmSettingsViewModel(IConfigRepository configRepository) : base(configRepository) {
        }

        public override string Identifier => "AlgorithmSettingsDialogHost";
        public override string SettingsName => "AlgorithmSettings";

        public BarcodeReaderSettingsInfoModel BarcodeReaderSettingsInfo {
            get => _barcodeReaderSettingsInfo;
            set => SetProperty(ref _barcodeReaderSettingsInfo, value);
        }

        public ObservableCollection<int> DeblurLevelItems {
            get => _deblurLevelItems;
            set => SetProperty(ref _deblurLevelItems, value);
        }

        public ObservableCollection<int> TextureDetectionSensitivityItems {
            get => _textureDetectionSensitivityItems;
            set => SetProperty(ref _textureDetectionSensitivityItems, value);
        }

        public override async void LoadedDelegate(object obj) {
            //加载设置
            var usbBarcodeReaderDto = await _configRepository.FirstOrDefaultEntity<UsbBarcodeReaderDto>(SettingsName) ??
                                      new UsbBarcodeReaderDto();
            BarcodeReaderSettingsInfo = new BarcodeReaderSettingsInfoModel() {
                IsUseOrCode = usbBarcodeReaderDto.IsUseOrCode,
                IsUseMicroQr = usbBarcodeReaderDto.IsUseMicroQr,
                IsUseCode39 = usbBarcodeReaderDto.IsUseCode39,
                IsUseCode93 = usbBarcodeReaderDto.IsUseCode93,
                IsUseCode128 = usbBarcodeReaderDto.IsUseCode128,
                IsUseCodeBar = usbBarcodeReaderDto.IsUseCodeBar,
                IsUseItf = usbBarcodeReaderDto.IsUseItf,
                IsUseEan13 = usbBarcodeReaderDto.IsUseEan13,
                IsUseEan8 = usbBarcodeReaderDto.IsUseEan8,
                LocalizationMode = usbBarcodeReaderDto.LocalizationMode,
                DeblurLevel = usbBarcodeReaderDto.DeblurLevel,
                ExpectedBarcodesCount = usbBarcodeReaderDto.ExpectedBarcodesCount,
                ScaleDownThreshold = usbBarcodeReaderDto.ScaleDownThreshold,
                IsUseTextFilterMode = usbBarcodeReaderDto.IsUseTextFilterMode,
                IsUseRegionPredetectionMode = usbBarcodeReaderDto.IsUseRegionPredetectionMode,
                GrayscaleTransformationMode = usbBarcodeReaderDto.GrayscaleTransformationMode,
                ImagePreprocessingMode = usbBarcodeReaderDto.ImagePreprocessingMode,
                MinResultConfidence = usbBarcodeReaderDto.MinResultConfidence,
                TextureDetectionSensitivity = usbBarcodeReaderDto.TextureDetectionSensitivity,
                BinarizationBlockSize = usbBarcodeReaderDto.BinarizationBlockSize,
                RecognitionMode = usbBarcodeReaderDto.RecognitionMode,
                RecognitionSkipFrames = usbBarcodeReaderDto.RecognitionSkipFrames,
                ScalePercentage = usbBarcodeReaderDto.ScalePercentage,
            };

            base.LoadedDelegate(obj);
        }

        protected override async Task<bool> SaveSettingsProcess() {
            return false;
        }

        //BarcodeReaderSettingsInfoModel
    }
}