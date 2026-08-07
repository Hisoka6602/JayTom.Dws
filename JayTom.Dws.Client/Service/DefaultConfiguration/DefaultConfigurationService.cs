using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.IO.Ports;
using Mono.Unix.Native;
using System.Threading;
using System.Diagnostics;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace JayTom.Dws.Client.Service.DefaultConfiguration
{

    public class DefaultConfigurationService : IDefaultConfigurationService
    {
        private readonly IConfigRepository _configRepository;
        private readonly ISoundRepository _soundRepository;

        public DefaultConfigurationService(IConfigRepository configRepository,
            ISoundRepository soundRepository)
        {
            _configRepository = configRepository;
            _soundRepository = soundRepository;
        }

        public async Task WriteDefaultConfiguration()
        {
            try
            {
                var configInfoModels = await _configRepository.Select(s => s.Id > 0, o => o.Id);
                if (configInfoModels?.Any() == true)
                {
                    return;
                }

                var fail = new SoundInfoModel()
                {
                    SoundName = new FileInfo($"{System.AppDomain.CurrentDomain.BaseDirectory}Sound\\fail.wav")
                        .Name,
                    SoundFile = File.ReadAllBytes(
                        $"{System.AppDomain.CurrentDomain.BaseDirectory}Sound\\fail.wav")
                };
                var success = new SoundInfoModel()
                {
                    SoundName = new FileInfo($"{System.AppDomain.CurrentDomain.BaseDirectory}Sound\\success.wav")
                        .Name,
                    SoundFile = File.ReadAllBytes(
                        $"{System.AppDomain.CurrentDomain.BaseDirectory}Sound\\success.wav")
                };
                //重量
                var task1 = _configRepository.InsertOrUpdate(new()
                {
                    ConfigName = "WeightSettings",
                    Value = JsonConvert.SerializeObject(new WeightSettingsDto
                    {
                        Mode = WeightMode.None,
                        Connection = new SerialPortSettingsInfo
                        {
                            BaudRate = 9600,
                            DataBits = 8,
                            DataFormat = DataFormatType.Ascii,
                            Parity = Parity.None,
                            StopBits = StopBits.One
                        },
                        CommonWeight = new CommonWeightParams
                        {
                            MaxWeight = 50,
                            MinWeight = 0
                        },
                    })
                });
                //存图
                var task2 = _configRepository.InsertOrUpdate(new ConfigInfoModel()
                {
                    ConfigName = "SaveImageSettings",
                    Value = JsonConvert.SerializeObject(new ImageSettingsDto
                    {
                        ImageRootDirectory = $"{GetMaxFreeSpaceDrive()}Images",
                        IsSaveBarcodeImage = true,
                        IsSavePanoramaImage = false,
                        IsSaveVolumeImage = false,
                        IsSaveOriginalImage = true,
                        IsUseWatermark = false,
                        WatermarkInfo = new WatermarkInfo
                        {
                            WatermarkColor = Color.DodgerBlue,
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
                    })
                });

                //体积
                var task4 = _configRepository.InsertOrUpdate(new ConfigInfoModel()
                {
                    ConfigName = "VolumeSettings",
                    Value = JsonConvert.SerializeObject(new VolumeSettingsDto
                    {
                        Unit = VolumeUnit.Millimeter,
                    })
                });
                //输出
                var task5 = _soundRepository.InsertOrUpdate(fail);
                //(声音)
                var task9 = _soundRepository.InsertOrUpdate(success);

                //声音
                var task6 = _configRepository.InsertOrUpdate(new ConfigInfoModel()
                {
                    ConfigName = "ResultOutputSettings",
                    Value = JsonConvert.SerializeObject(new ResultOutputSettingsDto
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
                            Parity = Parity.None,
                            StopBits = StopBits.One
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
                    })
                });
                //空间清理
                var task8 = _configRepository.InsertOrUpdate(new ConfigInfoModel()
                {
                    ConfigName = "CacheClearSettings",
                    Value = JsonConvert.SerializeObject(new CacheClearSettingsDto()
                    {
                        BarcodeDataAgoDays = 60,
                        FtpImageAgoDays = 60,
                        LogDataAgoDays = 60,
                        MinimumSpaceRetention = 100,
                        PanoramaImageAgoDays = 60,
                        ScanImageAgoDays = 60
                    })
                });
                //Api
                var task10 = _configRepository.InsertOrUpdate(new ConfigInfoModel()
                {
                    ConfigName = "ApiSettings",
                    Value = JsonConvert.SerializeObject(new ApiSettingsDto()
                    {
                        Type = ApiType.None
                    })
                });
                var task11 = _configRepository.InsertOrUpdate(new ConfigInfoModel()
                {
                    ConfigName = "CreatePackageSettings",
                    Value = JsonConvert.SerializeObject(new CreatePackageSettingsDto()
                    {
                    })
                });
                var task12 = _configRepository.InsertOrUpdate(new ConfigInfoModel()
                {
                    ConfigName = "PackageExitLockSettings",
                    Value = JsonConvert.SerializeObject(new PackageExitLockSettingsDto()
                    {
                    })
                });
                var task13 = _configRepository.InsertOrUpdate(new ConfigInfoModel()
                {
                    ConfigName = "StackedPackageDetectionSettings",
                    Value = JsonConvert.SerializeObject(new StackedPackageDetectionSettingsDto()
                    {
                    })
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