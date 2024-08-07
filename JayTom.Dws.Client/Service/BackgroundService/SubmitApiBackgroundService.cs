using System;
using System.Linq;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using JayTom.Dws.Domain.Interface;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.ImageService;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// Api提交处理器
    /// </summary>
    public class SubmitApiBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfigRepository _configRepository;
        private readonly IImageStorageService _imageStorageService;
        private ApiSettingsDto? _apiSettingsDto;
        private IApiUploader<BaseApiParameters>? _submissionUploader;

        #region 非通用版本变量(临时)

        private static string _sunnenApiPackage = string.Empty;
        private static bool _isWindowsClose;

        #endregion 非通用版本变量(临时)

        public SubmitApiBackgroundService(IHttpClientFactory httpClientFactory,
            IConfigRepository configRepository, IImageStorageService imageStorageService) {
            _httpClientFactory = httpClientFactory;
            _configRepository = configRepository;
            _imageStorageService = imageStorageService;

            //包裹信息完成
            EventAggregator.Instance.Subscribe<PackageInfo>(async item => {
                if (item is { BarCodeInfo: not null } model) {
                    //扫描
                    SubmitApiInfoManager.ScanPackage(model);
                    //上传
                    SubmitApiInfoManager.SubmitUploadInformation(model);
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                await Task.Yield();
                if (item is { } model) {
                    if (model.SettingsName.Equals("ApiSettings", StringComparison.CurrentCultureIgnoreCase)
                        || SubmitApiInfoManager.GetConfigParameterNames()?.Any(f => f.Equals(model.SettingsName, StringComparison.CurrentCultureIgnoreCase)) == true) {
                        var settingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>(model.SettingsName);

                        _submissionUploader = await SubmitApiInfoManager.ApiInitialization(_httpClientFactory, settingsDto.ApiName,
                            _configRepository);
                    }
                }
            });
            EventAggregator.Instance.Subscribe<PluginParamChangedEvent>(item => {
                if (item is { } model) {
                    if (model is { Type: PluginType.HomeTool, PluginName: "SunnenPlugin" }) {
                        _sunnenApiPackage = model.Content;
                    }
                }
            });
            _imageStorageService.ImageSaved += async delegate (object? sender, ImageSavedEventArgs args) {
                //上传图片
                await Task.Delay(300);
                if (args.ImageType == SaveImageType.BarcodeImage && !string.IsNullOrEmpty(args.BarCode)) {
                    SubmitApiInfoManager.SendImage(args.BarCode,
                        new List<UploadImageInfo>()
                        {
                            new()
                            {
                                CameraSerialNumber = args.CameraSerialNumber??string.Empty,
                                CameraName = args.CameraSerialNumber??string.Empty,
                                ScanTime = args.ScanTime,
                                Image = Image.FromFile(args.FilePath ?? string.Empty)
                            }
                        });
                }
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });
            //集包推送
            EventAggregator.Instance.Subscribe<PackageAggregationInfo>(async item => {
                //加入队列
                if (item is { } info) {
                    //提交集包
                    SubmitApiInfoManager.SendConsolidationReport(info.PackageExitDefinitionInfo.ExitName,
                        info.AggregatePackageCode, info.PackagingTime, info.PackageItems.Select(s => s.BarCodeInfo?.Barcode ?? string.Empty).ToList());
                }
            });

            //更新格口信息
            EventAggregator.Instance.Subscribe<PackageExitUpdateEvent>(async item => {
                if (item is { } model) {
                    var packageInfo = PackageInfoManager.GetPackage(f => f.Value != null &&
                                                                         f.Value.Timestamp.Equals(model.Timestamp));
                    //获取包裹

                    if (packageInfo is not null) {
                        //更新格口信息
                        if (packageInfo.ExitInfo is null) {
                            packageInfo.ExitInfo = new ExitInfoModel() {
                                PhysicalExit = model.ExitName,
                            };
                        }
                        else {
                            packageInfo.ExitInfo.PhysicalExit = model.ExitName;
                        }
                        //发送分拣报告
                        SubmitApiInfoManager.SendSortingReport(packageInfo);

                        //推送落格信息
                        EventAggregator.Instance.Publish(new PushPackageInfo() {
                            PackageInfo = packageInfo,
                            PackageExitUpdateEvent = model
                        });
                    }
                    else {
                        NLog.LogManager.GetCurrentClassLogger().Error($"未匹配到包裹:{model.InstructionInfos?.FirstOrDefault()?.InstructionContent} 操作指令");
                    }
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //读参数
            await ReadDefaultConfig();
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                await Task.Delay(3000, stoppingToken);
            }
        }

        private async Task ReadDefaultConfig() {
            //上传类型
            _apiSettingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>("ApiSettings") ?? new ApiSettingsDto();
            _submissionUploader = await SubmitApiInfoManager.ApiInitialization(_httpClientFactory, _apiSettingsDto.ApiName,
                _configRepository);
        }
    }
}