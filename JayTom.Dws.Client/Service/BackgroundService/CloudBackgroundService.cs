using Polly;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Polly.Retry;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Domain.Dto.CloudDto;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Data.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class CloudBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IConfigRepository _configRepository;
        private readonly ICloud _cloud;
        private readonly IPackageRepository _packageRepository;
        private readonly ICloudVideoUploadRepository _cloudVideoUploadRepository;
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private CloudVideoSettingsDto _cloudVideoSettingsDto = new();
        private DateTime _startTime = DateTime.Now;
        private SemaphoreSlim _cloudVideoUpLoadSlim = new(2);
        private List<NvrCameraBindingInfoModel> _nvrCameraBindingInfoModels = new();
        private SemaphoreSlim _setNvrCameraBindingSlim = new(1);

        public CloudBackgroundService(IConfigRepository configRepository,
            ICloud cloud, IPackageRepository packageRepository,
            ICloudVideoUploadRepository cloudVideoUploadRepository,
            INvrCameraBindingRepository nvrCameraBindingRepository) {
            _configRepository = configRepository;
            _cloud = cloud;
            _packageRepository = packageRepository;
            _cloudVideoUploadRepository = cloudVideoUploadRepository;
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is SettingsChangedEvent { SettingsName: "CloudVideoSettings" }) {
                    try {
                        var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("CloudVideoSettings"));
                        if (configInfoModel != null) {
                            _cloudVideoSettingsDto = JsonConvert.DeserializeObject<CloudVideoSettingsDto>(configInfoModel.Value) ?? new CloudVideoSettingsDto();
                            _cloudVideoUpLoadSlim = new SemaphoreSlim(_cloudVideoSettingsDto.Concurrency);
                            if (_cloudVideoSettingsDto.IsAutoUploadUnsyncedData) {
                                _startTime = new DateTime(1970, 1, 1);
                            }
                        }
                    }
                    catch (Exception e) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    }
                }
                else if (item is SettingsChangedEvent { SettingsName: "NvrCameraBindingInfoModel" }) {
                    try {
                        await _setNvrCameraBindingSlim.WaitAsync();
                        _nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.Select(s => s.Id > 0,
                            o => o.Id);
                    }
                    catch (Exception e) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    }
                    finally {
                        _setNvrCameraBindingSlim.Release();
                    }
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("CloudVideoSettings"), stoppingToken);
            if (configInfoModel != null) {
                _cloudVideoSettingsDto = JsonConvert.DeserializeObject<CloudVideoSettingsDto>(configInfoModel.Value) ?? new CloudVideoSettingsDto();
                _cloudVideoUpLoadSlim = new SemaphoreSlim(_cloudVideoSettingsDto.Concurrency);
                if (_cloudVideoSettingsDto.IsAutoUploadUnsyncedData) {
                    _startTime = new DateTime(1970, 1, 1);
                }
            }

            _nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.Select(s => s.Id > 0,
                o => o.Id, stoppingToken);

            while (!stoppingToken.IsCancellationRequested) {
                //设置参数
                //提交到云端
                if (_cloudVideoSettingsDto.IsUseCloudVideoUpload) {
                    if (_cloudVideoUpLoadSlim.CurrentCount > 0) {
                        var (key, value) = await _packageRepository.SelectPackage(s =>
                                s.PackageCreateTime.CompareTo(_startTime) > 0 &&
                                s.PackageCreateTime.CompareTo(
                                    DateTime.Now.AddSeconds(0 - _cloudVideoSettingsDto.UploadIntervalInSeconds)) <= 0 &&
                                (s.CloudVideoUploadInfo == null || s.CloudVideoUploadInfo.UploadTime == null),
                            o => o.PackageCreateTime, 0,
                            _cloudVideoSettingsDto.Concurrency, stoppingToken);

                        /*var (key, value) = await _packageRepository.SelectBarCode(s =>
                                s.ScanTime.CompareTo(_startTime) > 0 &&
                                s.ScanTime.CompareTo(DateTime.Now.AddSeconds(0 - _cloudVideoSettingsDto.UploadIntervalInSeconds)) <= 0 &&
                                (s.CloudVideoUploadInfo == null || s.CloudVideoUploadInfo.UploadTime == null),
                            o => o.ScanTime, 0,
                            _cloudVideoSettingsDto.Concurrency, stoppingToken);*/
                        if (key && value is { } packageInfoModels) {
                            if (packageInfoModels?.Any() == true) {
                                foreach (var packageInfoModel in packageInfoModels) {
                                    PolicyCloudVideoUpLoad(packageInfoModel, stoppingToken);
                                }

                                _startTime = packageInfoModels.Max(m => m.PackageCreateTime);
                            }
                        }
                    }
                }
                await Task.Delay(50, stoppingToken);
            }
        }

        private async void PolicyCloudVideoUpLoad(PackageInfoModel packageInfoModel, CancellationToken token) {
            try {
                await _cloudVideoUpLoadSlim.WaitAsync(token);
                var retryPolicy = Policy.HandleResult<bool>(result => !result)
               .Or<Exception>().RetryAsync(_cloudVideoSettingsDto.RetryAttempts, (a, b) => {
                   EventAggregator.Instance.Publish(new CloudVideoUploadRetryMessage {
                       Barcode = packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                       RetryCount = b
                   });
               });
                await retryPolicy.ExecuteAsync(async () => {
                    //获取数据
                    //创建多线程

                    //位置输出*/
                    var (key, value) = await _cloud.SetParameters(new Dictionary<string, object>() {
                    { "WebDoMain", _cloudVideoSettingsDto.WebDoMain },
                    { "Timeout", _cloudVideoSettingsDto.RequestTimeout },
                    });
                    if (key) {
                        var cameraSerialNumber = packageInfoModel.ImageInfos?.FirstOrDefault(f => f.Type == 0)?.CameraSerialNumber;
                        //取出绑定信息
                        List<NvrCameraBindingInfoModel> nvrCameraBindingInfoModels;
                        try {
                            await _setNvrCameraBindingSlim.WaitAsync(token);
                            nvrCameraBindingInfoModels = _nvrCameraBindingInfoModels.Where(f => !string.IsNullOrEmpty(cameraSerialNumber)
                                                                                                                                 && f.BarcodeScannerSerialNumber.Equals(
                                                                                                                                     cameraSerialNumber))?.ToList() ??
                                                                                          new List<NvrCameraBindingInfoModel>();
                        }
                        finally {
                            _setNvrCameraBindingSlim.Release();
                        }

                        var cloudUploadResponse = await _cloud.UploadData(packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                            packageInfoModel.BarCodeInfo?.ScanTime ?? DateTime.Now, packageInfoModel.WeightInfo?.FormattedWeight ?? 0,
                            _cloudVideoSettingsDto.NodeName,
                            null, packageInfoModel.ImageInfos?.Select(s =>
                                new CloudUploadImageInfo {
                                    CameraSerialNumber = s.CameraSerialNumber,
                                    CameraName = s.CameraName,
                                    CustomCameraName = s.CustomCameraName,
                                    Type = s.Type,
                                    Image = File.Exists(s.LocalPath) ? Image.FromFile(s.LocalPath) : null
                                })?.ToList(), nvrCameraBindingInfos: nvrCameraBindingInfoModels.Select(nvr =>
                           new CloudNvrCameraBindingInfo {
                               BarcodeScannerSerialNumber = nvr.BarcodeScannerSerialNumber,
                               Channel = nvr.Channel,
                               IpAddress = nvr.IpAddress,
                               Password = nvr.Password,
                               Port = nvr.Port,
                               Username = nvr.Username
                           })?.ToList() ?? new List<CloudNvrCameraBindingInfo>(), token: token);
                        EventAggregator.Instance.Publish(new CloudVideoUploadMessage {
                            Barcode = packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                            IsSuccessful = cloudUploadResponse.IsSuccessful,
                            PanoramaImageCount = packageInfoModel.ImageInfos?.Count(c => c.Type == 1) ?? 0,
                            ScanImageCount = packageInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0,
                            ScanTime = packageInfoModel.PackageCreateTime
                        });

                        if (cloudUploadResponse.IsSuccessful) {
                            var cloudVideoUploadInfoModel = await _cloudVideoUploadRepository.FirstOrDefault(f =>
                                f.PackageId.Equals(packageInfoModel.Id), token);
                            if (cloudVideoUploadInfoModel is not null) {
                                //更新
                                cloudVideoUploadInfoModel.ResponseContent = cloudUploadResponse.ResponseContent;
                                cloudVideoUploadInfoModel.TargetAddress = cloudUploadResponse.TargetAddress;
                                cloudVideoUploadInfoModel.UploadTime = cloudUploadResponse.UploadTime;
                                cloudVideoUploadInfoModel.UploadContent = cloudUploadResponse.UploadContent;
                                cloudVideoUploadInfoModel.UploadDuration = cloudUploadResponse.UploadDuration;
                                cloudVideoUploadInfoModel.ScanImageCount = packageInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0;
                                cloudVideoUploadInfoModel.PanoramaImageCount =
                                    packageInfoModel.ImageInfos?.Count(c => c.Type == 1) ?? 0;

                                return await _cloudVideoUploadRepository.Update(cloudVideoUploadInfoModel, token);
                            }
                            else {
                                return await _cloudVideoUploadRepository.Insert(new CloudVideoUploadInfoModel() {
                                    PackageId = packageInfoModel.Id,
                                    ResponseContent = cloudUploadResponse.ResponseContent,
                                    TargetAddress = cloudUploadResponse.TargetAddress,
                                    UploadTime = cloudUploadResponse.UploadTime,
                                    UploadContent = cloudUploadResponse.UploadContent,
                                    UploadDuration = cloudUploadResponse.UploadDuration,
                                    ScanImageCount = packageInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0,
                                    PanoramaImageCount =
                                        packageInfoModel.ImageInfos?.Count(c => c.Type == 1) ?? 0
                                }, token);
                            }
                        }
                        return false;
                    }
                    else {
                        return false;
                    }
                });
            }
            finally {
                _cloudVideoUpLoadSlim.Release();
            }
        }

        //重试方法
    }
}