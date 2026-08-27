using JayTom.Dws.Application.Configuration;
using System;
using JayTom.Dws.Application.Audio;
using JayTom.Dws.Application.Storage;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.IO.Ports;
using Mono.Unix.Native;
using System.Threading;
using System.Diagnostics;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Models.LocalConf;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;
using JayTom.Dws.Legacy.Contracts.Dto.BaseInfoModels;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;
using JayTom.Dws.Legacy.Contracts.Dto.PackageExitLockDto;
using JayTom.Dws.Abstractions.Devices;
using JayTom.Dws.Abstractions.Graphics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace JayTom.Dws.Client.Service.DefaultConfiguration
{

    public class DefaultConfigurationService : IDefaultConfigurationService
    {
        private readonly ISettingsStore _settingsStore;
        private readonly ISoundCatalog _soundCatalog;
        private readonly IBinaryAssetStore _assetStore;

        public DefaultConfigurationService(ISettingsStore settingsStore,
            ISoundCatalog soundCatalog,
            IBinaryAssetStore assetStore)
        {
            _settingsStore = settingsStore;
            _soundCatalog = soundCatalog;
            _assetStore = assetStore;
        }

        public async Task WriteDefaultConfiguration()
        {
            try
            {
                if (await _settingsStore.AnyAsync())
                {
                    return;
                }

                var failPath = Path.Combine(AppContext.BaseDirectory, "Sound", "fail.wav");
                var successPath = Path.Combine(AppContext.BaseDirectory, "Sound", "success.wav");
                var failAsset = await SaveBundledSoundAsync(failPath);
                var successAsset = await SaveBundledSoundAsync(successPath);
                if (!failAsset.IsSuccess || !successAsset.IsSuccess) {
                    throw new InvalidOperationException("默认声音资源外置失败。");
                }

                var fail = new SoundInfoModel()
                {
                    SoundName = Path.GetFileName(failPath),
                    SoundFileReference = failAsset.Value.Value
                };
                var success = new SoundInfoModel()
                {
                    SoundName = Path.GetFileName(successPath),
                    SoundFileReference = successAsset.Value.Value
                };
                //重量
                var task1 = _settingsStore.SaveAsync("WeightSettings",new WeightSettingsDto
                    {
                        Mode = WeightMode.None,
                        Connection = new SerialPortSettingsInfo
                        {
                            BaudRate = 9600,
                            DataBits = 8,
                            DataFormat = DataFormatType.Ascii,
                            Parity = SerialParity.None,
                            StopBits = SerialStopBits.One
                        },
                        CommonWeight = new CommonWeightParams
                        {
                            MaxWeight = 50,
                            MinWeight = 0
                        },
                    });
                //存图
                var task2 = _settingsStore.SaveAsync("SaveImageSettings",new ImageSettingsDto
                    {
                        ImageRootDirectory = $"{GetMaxFreeSpaceDrive()}Images",
                        IsSaveBarcodeImage = true,
                        IsSavePanoramaImage = false,
                        IsSaveVolumeImage = false,
                        IsSaveOriginalImage = true,
                        IsUseWatermark = false,
                        WatermarkInfo = new WatermarkInfo
                        {
                            WatermarkColor = new RgbaColor(Color.DodgerBlue.A, Color.DodgerBlue.R,
                                Color.DodgerBlue.G, Color.DodgerBlue.B),
                            WatermarkFontSize = 10,
                            WatermarkPosition = WatermarkPosition.TopLeft
                        },
                        SubDirectoryTemplate = new List<ItemTemplateInfo>() {
                            new()
                            {
                                ApplicationType = ItemApplicationType.SubDirectory,
                                Type =1,
                                Content = "{ImageType}"
                            },
                            new()
                            {
                                ApplicationType = ItemApplicationType.SubDirectory,
                                Type =1,
                                Content = "{Year}"
                            },
                            new()
                            {
                                ApplicationType = ItemApplicationType.SubDirectory,
                                Type =1,
                                Content = "{Month}"
                            },
                            new()
                            {
                                ApplicationType = ItemApplicationType.SubDirectory,
                                Type =1,
                                Content = "{Day}"
                            },
                        },
                        ImageNamingTemplate = new List<ItemTemplateInfo>()
                        {
                            new()
                            {
                                ApplicationType = ItemApplicationType.ImageNaming,
                                Type =1,
                                Content = "{BarCode}"
                            },
                            new()
                            {
                                ApplicationType = ItemApplicationType.ImageNaming,
                                Type =1,
                                Content = "{ScanTime}"
                            },
                        },
                        IsFtpUploadEnabled = false,
                        FtpInfo = new FtpInfo()
                        {
                            IpAddress = "127.0.0.1",
                            Password = "123",
                            Port = 21,
                            Timeout = 5000,
                            Username = "Root"
                        }
                    });

                //体积
                var task4 = _settingsStore.SaveAsync("VolumeSettings",new VolumeSettingsDto
                    {
                        Unit = VolumeUnit.Millimeter,
                    });
                //输出
                var task5 = _soundCatalog.SaveAsync(fail);
                //(声音)
                var task9 = _soundCatalog.SaveAsync(success);

                //声音
                var task6 = _settingsStore.SaveAsync("ResultOutputSettings",new ResultOutputSettingsDto
                    {
                        DataTemplate = new List<ItemTemplateInfo>(),
                        UploadSettingsInfo = new UploadSettingsInfo(),
                        IsUseTcpOutput = false,
                        TcpSettingsInfo = new TcpSettingsInfo()
                        {
                            ClientConfig = new TcpInfo()
                            {
                                IpAddress = "127.0.0.1",
                                Port = 2000
                            },
                            ServerConfig = new TcpInfo()
                            {
                                IpAddress = "127.0.0.1",
                                Port = 2000
                            },
                            ConnectionMode = TcpConnectionMode.Client
                        },
                        IsUseSerialOutput = false,
                        SerialPortSettingsInfo = new SerialPortSettingsInfo()
                        {
                            BaudRate = 9600,
                            DataBits = 8,
                            DataFormat = DataFormatType.Ascii,
                            Parity = SerialParity.None,
                            StopBits = SerialStopBits.One
                        },
                        SerialPortResultOutputInfo = new SerialPortResultOutputInfo()
                        {
                            IsUseCustomContentOutput = false,
                            IsUseDataTemplateOutput = true
                        },
                        IsUseAudioOutput = true,
                        AudioOutputSettingsInfo = new AudioOutputSettingsInfo()
                        {
                            FailureAudio = "fail.wav",
                            SuccessAudio = "success.wav",
                            TriggerPosition = TriggerPositionEnum.PackageTrigger,
                            Result = ResultEnum.PackageRecognition
                        },
                        IsUseLocationOutput = false,
                    });
                //空间清理
                var task8 = _settingsStore.SaveAsync("CacheClearSettings",new CacheClearSettingsDto()
                    {
                        BarcodeDataAgoDays = 60,
                        FtpImageAgoDays = 60,
                        LogDataAgoDays = 60,
                        MinimumSpaceRetention = 100,
                        PanoramaImageAgoDays = 60,
                        ScanImageAgoDays = 60
                    });
                //Api
                var task10 = _settingsStore.SaveAsync("ApiSettings",new ApiSettingsDto()
                    {
                        Type = ApiType.None
                    });
                var task11 = _settingsStore.SaveAsync("CreatePackageSettings",new CreatePackageSettingsDto()
                    {
                    });
                var task12 = _settingsStore.SaveAsync("PackageExitLockSettings",new PackageExitLockSettingsDto()
                    {
                    });
                var task13 = _settingsStore.SaveAsync("StackedPackageDetectionSettings",new StackedPackageDetectionSettingsDto()
                    {
                    });
                await Task.WhenAll(task1,
                    task2,
                    task4,
                    task5,
                    task6,
                    task8,
                    task9,
                    task10,
                    task11,
                    task12,
                    task13);
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error($"写默认配置失败!");
            }
        }

        private async Task<JayTom.Dws.Abstractions.Results.OperationResult<BinaryAssetReference>>
            SaveBundledSoundAsync(string path)
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return await _assetStore.SaveAsync(
                "sounds",
                Path.GetFileName(path),
                stream,
                CancellationToken.None);
        }

        public string GetMaxFreeSpaceDrive()
        {
            try
            {
                var drives = DriveInfo.GetDrives();

                // 使用 LINQ 查询找到剩余容量最大的磁盘
                var maxFreeSpaceDrive = drives
                    .Where(d => d.IsReady).MaxBy(d => d.AvailableFreeSpace);

                return maxFreeSpaceDrive != null ? maxFreeSpaceDrive.Name :
                    // 没有可用的磁盘
                    "C:\\";
            }
            catch (Exception e)
            {
                return "C:\\";
            }
        }
    }
}
