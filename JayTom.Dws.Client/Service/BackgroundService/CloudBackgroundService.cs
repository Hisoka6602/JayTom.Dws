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
        private readonly IBarCodeRepository _barCodeRepository;
        private readonly ICloudVideoUploadRepository _cloudVideoUploadRepository;
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private CloudVideoSettingsDto _cloudVideoSettingsDto = new();
        private DateTime _startTime = DateTime.Now;
        private SemaphoreSlim _cloudVideoUpLoadSlim = new(2);
        private List<NvrCameraBindingInfoModel> _nvrCameraBindingInfoModels = new();

        public CloudBackgroundService(IConfigRepository configRepository,
            ICloud cloud, IBarCodeRepository barCodeRepository,
            ICloudVideoUploadRepository cloudVideoUploadRepository,
            INvrCameraBindingRepository nvrCameraBindingRepository) {
            _configRepository = configRepository;
            _cloud = cloud;
            _barCodeRepository = barCodeRepository;
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
                        var (key, value) = await _barCodeRepository.SelectBarCode(s =>
                                s.ScanTime.CompareTo(_startTime) > 0 &&
                                s.ScanTime.CompareTo(DateTime.Now.AddSeconds(-20)) <= 0 &&
                                (s.CloudVideoUploadInfo == null || s.CloudVideoUploadInfo.UploadTime == null),
                            o => o.ScanTime, 0,
                            _cloudVideoSettingsDto.Concurrency, stoppingToken);
                        if (key && value is { } barCodeInfoModels) {
                            if (barCodeInfoModels?.Any() == true) {
                                foreach (var barCodeInfoModel in barCodeInfoModels) {
                                    PolicyCloudVideoUpLoad(barCodeInfoModel, stoppingToken);
                                }

                                _startTime = barCodeInfoModels.Max(m => m.ScanTime);
                            }
                        }
                    }
                }
                await Task.Delay(50, stoppingToken);
            }
        }

        private async void PolicyCloudVideoUpLoad(BarCodeInfoModel barCodeInfoModel, CancellationToken token) {
            try {
                await _cloudVideoUpLoadSlim.WaitAsync(token);
                var retryPolicy = Policy.HandleResult<bool>(result => !result)
               .Or<Exception>().RetryAsync(_cloudVideoSettingsDto.RetryAttempts, (a, b) => {
                   EventAggregator.Instance.Publish(new CloudVideoUploadRetryMessage {
                       Barcode = barCodeInfoModel.Barcode,
                       RetryCount = b
                   });
               });
                await retryPolicy.ExecuteAsync(async () => {
                    //获取数据
                    //创建多线程

                    //位置输出*/
                    var (key, value) = await _cloud.SetParameters(new Dictionary<string, object>() {
                    { "Url", _cloudVideoSettingsDto.Url },
                    { "Timeout", _cloudVideoSettingsDto.RequestTimeout },
                    });
                    if (key) {
                        var cameraSerialNumber = barCodeInfoModel.ImageInfos?.FirstOrDefault(f => f.Type == 0)?.CameraSerialNumber;
                        //取出绑定信息
                        var nvrCameraBindingInfoModel = _nvrCameraBindingInfoModels.FirstOrDefault(f => !string.IsNullOrEmpty(cameraSerialNumber)
                            && f.BarcodeScannerSerialNumber.Equals(
                                cameraSerialNumber)) ?? new NvrCameraBindingInfoModel();
                        var cloudUploadResponse = await _cloud.UploadData(barCodeInfoModel.Barcode,
                            barCodeInfoModel.ScanTime, barCodeInfoModel.Weight,
                            _cloudVideoSettingsDto.NodeName,
                            null, barCodeInfoModel.ImageInfos?.Select(s =>
                                new CloudUploadImageInfo {
                                    CameraSerialNumber = s.CameraSerialNumber,
                                    CameraName = s.CameraName,
                                    CustomCameraName = s.CustomCameraName,
                                    Type = s.Type,
                                    Image = File.Exists(s.LocalPath) ? Image.FromFile(s.LocalPath) : null
                                })?.ToList(), nvrCameraBindingInfo: new CloudNvrCameraBindingInfo() {
                                    BarcodeScannerSerialNumber = nvrCameraBindingInfoModel.BarcodeScannerSerialNumber,
                                    Channel = nvrCameraBindingInfoModel.Channel,
                                    IpAddress = nvrCameraBindingInfoModel.IpAddress,
                                    Password = nvrCameraBindingInfoModel.Password,
                                    Port = nvrCameraBindingInfoModel.Port,
                                    Username = nvrCameraBindingInfoModel.Username
                                }, token: token);
                        EventAggregator.Instance.Publish(new CloudVideoUploadMessage {
                            Barcode = barCodeInfoModel.Barcode,
                            IsSuccessful = cloudUploadResponse.IsSuccessful,
                            PanoramaImageCount = barCodeInfoModel.ImageInfos?.Count(c => c.Type == 1) ?? 0,
                            ScanImageCount = barCodeInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0,
                        });

                        if (cloudUploadResponse.IsSuccessful) {
                            var cloudVideoUploadInfoModel = await _cloudVideoUploadRepository.FirstOrDefault(f =>
                                f.BarcodeId.Equals(barCodeInfoModel.Id), token);
                            if (cloudVideoUploadInfoModel is not null) {
                                //更新
                                cloudVideoUploadInfoModel.ResponseContent = cloudUploadResponse.ResponseContent;
                                cloudVideoUploadInfoModel.TargetAddress = cloudUploadResponse.TargetAddress;
                                cloudVideoUploadInfoModel.UploadTime = cloudUploadResponse.UploadTime;
                                cloudVideoUploadInfoModel.UploadContent = cloudUploadResponse.UploadContent;
                                cloudVideoUploadInfoModel.UploadDuration = cloudUploadResponse.UploadDuration;
                                cloudVideoUploadInfoModel.ScanImageCount = barCodeInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0;
                                cloudVideoUploadInfoModel.PanoramaImageCount =
                                    barCodeInfoModel.ImageInfos?.Count(c => c.Type == 1) ?? 0;

                                return await _cloudVideoUploadRepository.Update(cloudVideoUploadInfoModel, token);
                            }
                            else {
                                return await _cloudVideoUploadRepository.Insert(new CloudVideoUploadInfoModel() {
                                    BarcodeId = barCodeInfoModel.Id,
                                    ResponseContent = cloudUploadResponse.ResponseContent,
                                    TargetAddress = cloudUploadResponse.TargetAddress,
                                    UploadTime = cloudUploadResponse.UploadTime,
                                    UploadContent = cloudUploadResponse.UploadContent,
                                    UploadDuration = cloudUploadResponse.UploadDuration,
                                    ScanImageCount = barCodeInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0,
                                    PanoramaImageCount =
                                        barCodeInfoModel.ImageInfos?.Count(c => c.Type == 1) ?? 0
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