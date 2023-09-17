using System;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Client.Service.ImageStorage;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;
using static JayTom.Dws.Client.Service.BackgroundService.ScanProcessBackgroundService;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// 数据处理器
    /// </summary>
    public class DataProcessingBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IBarCodeRepository _barCodeRepository;
        private readonly IPanoramaImageRepository _panoramaImageRepository;
        private readonly IImageStorageService _imageStorageService;
        private ConcurrentQueue<BarCodeInfoModel> _insertItems = new();
        private ConcurrentQueue<ApiResponseReceived> _updateResponseItems = new();
        private ConcurrentQueue<SavedImageInfo> _savedImageItems = new();

        public DataProcessingBackgroundService(IBarCodeRepository barCodeRepository,
            IPanoramaImageRepository panoramaImageRepository,
            IImageStorageService imageStorageService) {
            _barCodeRepository = barCodeRepository;
            _panoramaImageRepository = panoramaImageRepository;
            _imageStorageService = imageStorageService;
            _imageStorageService.ImageSaved += delegate (object? sender, ImageSavedEventArgs args) {
                //保存后触发
                _savedImageItems.Enqueue(new SavedImageInfo() {
                    BarCode = args.BarCode,
                    FilePath = args.FilePath,
                    ImageType = args.ImageType,
                    CameraSerialNumber = args.CameraSerialNumber ?? string.Empty,
                    ScanTime = args.ScanTime,
                });
            };
            EventAggregator.Instance.Subscribe<ScanBarCodeInfo>(item => {
                if (item is ScanBarCodeInfo model) {
                    //添加
                    _insertItems.Enqueue(new BarCodeInfoModel() {
                        Barcode = model.BarCode,
                        Weight = (float)(model.Weight ?? 0),
                        ScanTime = model.ScanTime,
                        Volume = (float)(model.Volume ?? 0),
                        Length = (float)(model.Length ?? 0),
                        Width = (float)(model.Width ?? 0),
                        Height = (float)(model.Height ?? 0),
                        TimestampedGuid = new DateTimeOffset(model.ScanTime).ToUnixTimeMilliseconds()
                    });
                }
            });
            EventAggregator.Instance.Subscribe<ApiResponseReceived>(item => {
                if (item is ApiResponseReceived model) {
                    _updateResponseItems.Enqueue(model);
                }
            });
        }

        //保存队列
        //更新队列
        //ScanBarCodeInfo(扫码结果)
        //ApiResponseReceived(提交结果)
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                var tryDequeue = _insertItems.TryDequeue(out var insertModel);
                if (tryDequeue && insertModel is not null) {
                    _barCodeRepository.InsertAsync(insertModel, stoppingToken);
                }

                var dequeue = _updateResponseItems.TryDequeue(out var responseModel);
                if (dequeue && responseModel is not null) {
                    //更新
                    var barCodeInfoModel = await _barCodeRepository.FirstOrDefault(f =>
                        f.Barcode.Equals(responseModel.Barcode)
                        && f.ScanTime.Equals(responseModel.ScanTime), stoppingToken);

                    if (barCodeInfoModel is not null) {
                        barCodeInfoModel.RequestContent = responseModel.UploadResponse?.RequestContent ?? string.Empty;
                        barCodeInfoModel.RequestStatus = responseModel.UploadResponse?.IsSuccess == true ? UploadStatus.Succeeded : UploadStatus.Failed;
                        barCodeInfoModel.RequestTime = responseModel.UploadResponse?.RequestTime ?? DateTime.Today;
                        barCodeInfoModel.ResponseContent = responseModel.UploadResponse?.ResponseContent ?? string.Empty;
                        barCodeInfoModel.ResponseTime = responseModel.UploadResponse?.ResponseTime ?? DateTime.Today;
                        var update = await _barCodeRepository.Update(barCodeInfoModel, stoppingToken);
                        if (!update) {
                            _updateResponseItems.Enqueue(responseModel);
                        }
                    }
                    else {
                        _updateResponseItems.Enqueue(responseModel);
                    }
                }
                //更新图片路径

                var isSaved = _savedImageItems.TryDequeue(out var savedImageInfo);
                if (isSaved && savedImageInfo is not null) {
                    if (savedImageInfo.ImageType == SaveImageType.BarcodeImage) {
                        //扫码图
                        var barCodeInfoModel = await _barCodeRepository.FirstOrDefault(f =>
                            f.Barcode.Equals(savedImageInfo.BarCode)
                            && f.ScanTime.Equals(savedImageInfo.ScanTime), stoppingToken);
                        if (barCodeInfoModel is not null) {
                            barCodeInfoModel.BarcodeImagePath = savedImageInfo.FilePath;
                            var update = await _barCodeRepository.Update(barCodeInfoModel, stoppingToken);
                            if (!update) {
                                _savedImageItems.Enqueue(savedImageInfo);
                            }
                        }
                        else {
                            _savedImageItems.Enqueue(savedImageInfo);
                        }
                    }
                    else if (savedImageInfo.ImageType == SaveImageType.PanoramaImage) {
                        //全景图
                        var barCodeInfoModel = await _barCodeRepository.FirstOrDefault(f =>
                            f.Barcode.Equals(savedImageInfo.BarCode)
                            && f.ScanTime.Equals(savedImageInfo.ScanTime), stoppingToken);
                        if (barCodeInfoModel is not null) {
                            var insert = await _panoramaImageRepository.Insert(new PanoramaImageInfoModel() {
                                PanoramaImagePath = savedImageInfo.FilePath,
                                BarcodeInfoId = barCodeInfoModel.Id
                            }, stoppingToken);
                            if (!insert) {
                                _savedImageItems.Enqueue(savedImageInfo);
                            }
                        }
                        else {
                            _savedImageItems.Enqueue(savedImageInfo);
                        }
                    }
                }
            }
        }
    }
}