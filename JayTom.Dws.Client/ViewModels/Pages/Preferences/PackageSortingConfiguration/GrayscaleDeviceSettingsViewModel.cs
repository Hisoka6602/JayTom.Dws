using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
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
                    }
                };
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
                }
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