using JayTom.Dws.Application.Configuration;
using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Dto.CameraConfiguration;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration
{

    public class AlgorithmSettingsViewModel : SettingsPageTemplateViewModel
    {
        private BarcodeReaderSettingsInfoModel _barcodeReaderSettingsInfo = new();
        private ObservableCollection<int> _deblurLevelItems = new([.. Enumerable.Range(0, 10)]);
        private ObservableCollection<int> _textureDetectionSensitivityItems = new([.. Enumerable.Range(0, 10)]);

        public AlgorithmSettingsViewModel(ISettingsStore settingsStore) : base(settingsStore)
        {
        }

        public override string Identifier => "AlgorithmSettingsDialogHost";
        public override string SettingsName => "AlgorithmSettings";

        public BarcodeReaderSettingsInfoModel BarcodeReaderSettingsInfo
        {
            get => _barcodeReaderSettingsInfo;
            set => SetProperty(ref _barcodeReaderSettingsInfo, value);
        }

        public ObservableCollection<int> DeblurLevelItems
        {
            get => _deblurLevelItems;
            set => SetProperty(ref _deblurLevelItems, value);
        }

        public ObservableCollection<int> TextureDetectionSensitivityItems
        {
            get => _textureDetectionSensitivityItems;
            set => SetProperty(ref _textureDetectionSensitivityItems, value);
        }

        public override async void LoadedDelegate(object obj)
        {
            //加载设置
            var usbBarcodeReaderDto = await _settingsStore.GetAsync<UsbBarcodeReaderDto>(SettingsName) ??
                                      new UsbBarcodeReaderDto();
            BarcodeReaderSettingsInfo = new BarcodeReaderSettingsInfoModel()
            {
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
                BarcodeType = usbBarcodeReaderDto.BarcodeType,
            };
            var includedEnums = Enum.GetValues(typeof(BarcodeType))
                .Cast<BarcodeType>()
                .Where(e => usbBarcodeReaderDto.BarcodeType.HasFlag(e))
                .ToList();
            foreach (var infoModel in includedEnums.Select(methodsEnum => BarcodeReaderSettingsInfo.BarcodeTypeItems.FirstOrDefault(f =>
                         f.EnumValue.Equals(methodsEnum))).OfType<BarcodeTypeItemInfoModel>())
            {
                infoModel.IsChecked = true;
            }
        }

        protected override async Task<bool> SaveSettingsProcess()
        {
            BarcodeReaderSettingsInfo.BarcodeType = BarcodeType.None;
            foreach (var item in BarcodeReaderSettingsInfo.BarcodeTypeItems.Where(w => w.IsChecked).ToList())
            {
                BarcodeReaderSettingsInfo.BarcodeType |= item.EnumValue;
            }
            var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,new UsbBarcodeReaderDto
                {
                    /*IsUseOrCode = BarcodeReaderSettingsInfo.IsUseOrCode,
                    IsUseMicroQr = BarcodeReaderSettingsInfo.IsUseMicroQr,
                    IsUseCode39 = BarcodeReaderSettingsInfo.IsUseCode39,
                    IsUseCode93 = BarcodeReaderSettingsInfo.IsUseCode93,
                    IsUseCode128 = BarcodeReaderSettingsInfo.IsUseCode128,
                    IsUseCodeBar = BarcodeReaderSettingsInfo.IsUseCodeBar,
                    IsUseItf = BarcodeReaderSettingsInfo.IsUseItf,
                    IsUseEan13 = BarcodeReaderSettingsInfo.IsUseEan13,
                    IsUseEan8 = BarcodeReaderSettingsInfo.IsUseEan8,*/
                    LocalizationMode = BarcodeReaderSettingsInfo.LocalizationMode,
                    DeblurLevel = BarcodeReaderSettingsInfo.DeblurLevel,
                    ExpectedBarcodesCount = BarcodeReaderSettingsInfo.ExpectedBarcodesCount,
                    ScaleDownThreshold = BarcodeReaderSettingsInfo.ScaleDownThreshold,
                    IsUseTextFilterMode = BarcodeReaderSettingsInfo.IsUseTextFilterMode,
                    IsUseRegionPredetectionMode = BarcodeReaderSettingsInfo.IsUseRegionPredetectionMode,
                    GrayscaleTransformationMode = BarcodeReaderSettingsInfo.GrayscaleTransformationMode,
                    ImagePreprocessingMode = BarcodeReaderSettingsInfo.ImagePreprocessingMode,
                    MinResultConfidence = BarcodeReaderSettingsInfo.MinResultConfidence,
                    TextureDetectionSensitivity = BarcodeReaderSettingsInfo.TextureDetectionSensitivity,
                    BinarizationBlockSize = BarcodeReaderSettingsInfo.BinarizationBlockSize,
                    RecognitionMode = BarcodeReaderSettingsInfo.RecognitionMode,
                    RecognitionSkipFrames = BarcodeReaderSettingsInfo.RecognitionSkipFrames,
                    ScalePercentage = BarcodeReaderSettingsInfo.ScalePercentage,
                    BarcodeType = BarcodeReaderSettingsInfo.BarcodeType
                });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            base.MessageQueue.Enqueue("请重启程序");
            return insertOrUpdate;
        }
    }
}