using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    public class GrayscaleDeviceSettingsViewModel : SettingsPageTemplateViewModel {
        private GrayscaleDeviceInfoModel _grayscaleDeviceInfo = new();

        public GrayscaleDeviceSettingsViewModel(IConfigRepository configRepository) : base(configRepository) {
        }

        private ObservableCollection<DataFormatTypeInfoModel> _dataFormatTypeItems = new()
        {
            new DataFormatTypeInfoModel()
            {
                Name = "Ascii",
                Value = DataFormatType.Ascii
            },
            new DataFormatTypeInfoModel()
            {
                Name = "Hex",
                Value = DataFormatType.Hex
            },
        };

        private int _additionalFrameRegionX1;
        private int _additionalFrameRegionY1;
        private int _additionalFrameRegionX2;
        private int _additionalFrameRegionY2;
        private int _mainFrameRegionX1;
        private int _mainFrameRegionY1;
        private int _mainFrameRegionX2;
        private int _mainFrameRegionY2;

        public override string Identifier => "PackageSortingSettingsDialog";
        public override string SettingsName => "GrayscaleDeviceSettings";

        public ObservableCollection<DataFormatTypeInfoModel> DataFormatTypeItems {
            get => _dataFormatTypeItems;
            set => SetProperty(ref _dataFormatTypeItems, value);
        }

        public GrayscaleDeviceInfoModel GrayscaleDeviceInfo {
            get => _grayscaleDeviceInfo;
            set => SetProperty(ref _grayscaleDeviceInfo, value);
        }

        public int AdditionalFrameRegionX1 {
            get => _additionalFrameRegionX1;
            set => SetProperty(ref _additionalFrameRegionX1, value);
        }

        public int AdditionalFrameRegionY1 {
            get => _additionalFrameRegionY1;
            set => SetProperty(ref _additionalFrameRegionY1, value);
        }

        public int AdditionalFrameRegionX2 {
            get => _additionalFrameRegionX2;
            set => SetProperty(ref _additionalFrameRegionX2, value);
        }

        public int AdditionalFrameRegionY2 {
            get => _additionalFrameRegionY2;
            set => SetProperty(ref _additionalFrameRegionY2, value);
        }

        public int MainFrameRegionX1 {
            get => _mainFrameRegionX1;
            set => SetProperty(ref _mainFrameRegionX1, value);
        }

        public int MainFrameRegionY1 {
            get => _mainFrameRegionY1;
            set => SetProperty(ref _mainFrameRegionY1, value);
        }

        public int MainFrameRegionX2 {
            get => _mainFrameRegionX2;
            set => SetProperty(ref _mainFrameRegionX2, value);
        }

        public int MainFrameRegionY2 {
            get => _mainFrameRegionY2;
            set => SetProperty(ref _mainFrameRegionY2, value);
        }

        public override async void LoadedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var settingsDto = await _configRepository.FirstOrDefaultEntity<GrayscaleDeviceSettingsDto>(SettingsName) ??
                                  new GrayscaleDeviceSettingsDto();

                GrayscaleDeviceInfo = new GrayscaleDeviceInfoModel() {
                    IsCheckPackageOrientation = settingsDto.IsCheckPackageOrientation,
                    IsUseGrayscaleDetector = settingsDto.IsUseGrayscaleDetector,
                    TcpConnectionConfigInfo = new TcpConnectionConfigItemInfoModel() {
                        ConnectionMode = settingsDto.TcpConnectionConfigInfo?.ConnectionMode ??
                                         TcpConnectionMode.Client,
                        DataFormat = DataFormatTypeItems.FirstOrDefault(f =>
                                         f.Value.Equals(settingsDto.TcpConnectionConfigInfo?.DataFormat)) ??
                                     new DataFormatTypeInfoModel(),
                        ClientParameter = new TcpConfigItemInfoModel() {
                            IpAddress = settingsDto.TcpConnectionConfigInfo?.ClientConfig?.IpAddress ??
                                        string.Empty,
                            Port = settingsDto.TcpConnectionConfigInfo?.ClientConfig?.Port ?? 0,
                        },
                        ServerParameter = new TcpConfigItemInfoModel() {
                            IpAddress = settingsDto.TcpConnectionConfigInfo?.ServerConfig?.IpAddress ??
                                        string.Empty,
                            Port = settingsDto.TcpConnectionConfigInfo?.ServerConfig?.Port ?? 0,
                        }
                    },
                    AdditionalFrameRegion = settingsDto.AdditionalFrameRegion,
                    MainFrameRegion = settingsDto.MainFrameRegion,
                    RegionCarCount = settingsDto.RegionCarCount,
                    TimeOut = settingsDto.TimeOut,
                    IsDirectionReversed = settingsDto.IsDirectionReversed,
                    LineCarCount = settingsDto.LineCarCount,
                    CarNumberOffset = settingsDto.CarNumberOffset,
                    AdditionalBoxSpacePercentage = settingsDto.AdditionalBoxSpacePercentage
                };
                AdditionalFrameRegionX1 = GrayscaleDeviceInfo.AdditionalFrameRegion.X;
                AdditionalFrameRegionY1 = GrayscaleDeviceInfo.AdditionalFrameRegion.Y;
                AdditionalFrameRegionX2 = GrayscaleDeviceInfo.AdditionalFrameRegion.Width;
                AdditionalFrameRegionY2 = GrayscaleDeviceInfo.AdditionalFrameRegion.Height;
                MainFrameRegionX1 = GrayscaleDeviceInfo.MainFrameRegion.X;
                MainFrameRegionY1 = GrayscaleDeviceInfo.MainFrameRegion.Y;
                MainFrameRegionX2 = GrayscaleDeviceInfo.MainFrameRegion.Width;
                MainFrameRegionY2 = GrayscaleDeviceInfo.MainFrameRegion.Height;
            });
        }

        protected override async Task<bool> SaveSettingsProcess() {
            var grayscaleDeviceSettingsDto = new GrayscaleDeviceSettingsDto() {
                IsCheckPackageOrientation = GrayscaleDeviceInfo.IsCheckPackageOrientation,
                IsUseGrayscaleDetector = GrayscaleDeviceInfo.IsUseGrayscaleDetector,
                TcpConnectionConfigInfo = new TcpSettingsInfo() {
                    ConnectionMode = GrayscaleDeviceInfo.TcpConnectionConfigInfo?.ConnectionMode ??
                                     TcpConnectionMode.Client,
                    DataFormat = GrayscaleDeviceInfo.TcpConnectionConfigInfo?.DataFormat?.Value ??
                                 DataFormatType.Ascii,

                    ClientConfig = new TcpInfo() {
                        IpAddress = GrayscaleDeviceInfo.TcpConnectionConfigInfo?.ClientParameter
                                        ?.IpAddress ??
                                    string.Empty,
                        Port = GrayscaleDeviceInfo.TcpConnectionConfigInfo?.ClientParameter?.Port ??
                               0,
                    },
                    ServerConfig = new TcpInfo() {
                        IpAddress = GrayscaleDeviceInfo.TcpConnectionConfigInfo?.ServerParameter
                                        ?.IpAddress ??
                                    string.Empty,
                        Port = GrayscaleDeviceInfo.TcpConnectionConfigInfo?.ServerParameter?.Port ??
                               0,
                    }
                },
                AdditionalFrameRegion = new Rectangle(AdditionalFrameRegionX1, AdditionalFrameRegionY1,
                    AdditionalFrameRegionX2, AdditionalFrameRegionY2),
                MainFrameRegion = new Rectangle(MainFrameRegionX1, MainFrameRegionY1, MainFrameRegionX2, MainFrameRegionY2),
                RegionCarCount = GrayscaleDeviceInfo.RegionCarCount,
                TimeOut = GrayscaleDeviceInfo.TimeOut,
                IsDirectionReversed = GrayscaleDeviceInfo.IsDirectionReversed,
                LineCarCount = GrayscaleDeviceInfo.LineCarCount,
                CarNumberOffset = GrayscaleDeviceInfo.CarNumberOffset,
                AdditionalBoxSpacePercentage = GrayscaleDeviceInfo.AdditionalBoxSpacePercentage
            };

            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(grayscaleDeviceSettingsDto)
            });
            base.MessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(insertOrUpdate ?
                Languages.Language.ResourceManager.GetString("Success") :
                Languages.Language.ResourceManager.GetString("Failure"))}");
            return insertOrUpdate;
        }
    }
}