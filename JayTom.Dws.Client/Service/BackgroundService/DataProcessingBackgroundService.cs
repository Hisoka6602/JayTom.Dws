using System;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Windows.Documents;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// 数据处理器
    /// </summary>
    public class DataProcessingBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IBarCodeRepository _barCodeRepository;

        //private readonly IPanoramaImageRepository _panoramaImageRepository;
        private readonly IImageStorageService _imageStorageService;

        private readonly ISortingRepository _sortingRepository;
        private readonly IUploadRepository _uploadRepository;
        private readonly IImageRepository _imageRepository;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private ConcurrentQueue<BarCodeInfoModel> _insertItems = new();
        private ConcurrentQueue<ApiResponseReceived> _updateResponseItems = new();
        private ConcurrentQueue<SavedImageInfo> _savedImageItems = new();
        private ConcurrentQueue<InstructionReceived> _instructionItems = new();

        public DataProcessingBackgroundService(IBarCodeRepository barCodeRepository,
            //IPanoramaImageRepository panoramaImageRepository,
            IImageStorageService imageStorageService, ISortingRepository sortingRepository,
            IUploadRepository uploadRepository,
            IImageRepository imageRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository) {
            _barCodeRepository = barCodeRepository;
            // _panoramaImageRepository = panoramaImageRepository;
            _imageStorageService = imageStorageService;
            _sortingRepository = sortingRepository;
            _uploadRepository = uploadRepository;
            _imageRepository = imageRepository;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
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
            EventAggregator.Instance.Subscribe<PackageInfo>(item => {
                if (item is PackageInfo model) {
                    //添加
                    _insertItems.Enqueue(new BarCodeInfoModel() {
                        Barcode = model.BarCode ?? string.Empty,
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
            EventAggregator.Instance.Subscribe<InstructionReceived>(item => {
                if (item is InstructionReceived model) {
                    _instructionItems.Enqueue(model);
                }
            });
        }

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

                    if (barCodeInfoModel is not null && responseModel.UploadResponse is not null) {
                        barCodeInfoModel.RequestStatus = responseModel.UploadResponse.IsSuccess ? UploadStatus.Succeeded : UploadStatus.Failed;
                        var update = await _barCodeRepository.Update(barCodeInfoModel, stoppingToken);
                        var insert = await _uploadRepository.Insert(new UploadInfoModel() {
                            BarcodeId = barCodeInfoModel.Id,
                            IsSuccess = responseModel.UploadResponse.IsSuccess,
                            RequestContent = responseModel.UploadResponse.RequestContent,
                            ResponseContent = responseModel.UploadResponse.ResponseContent,
                            RequestTime = responseModel.UploadResponse.RequestTime,
                            ResponseTime = responseModel.UploadResponse.ResponseTime,
                            DurationInSeconds = responseModel.UploadResponse.Duration,
                            InterfaceParameters = responseModel.UploadResponse.ApiParameters,
                            RequestUrl = responseModel.UploadResponse.RequestUrl,
                            ExceptionMessage = responseModel.UploadResponse.ExceptionMsg,
                        }, stoppingToken);

                        if (!update || !insert) {
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
                            //获取相机信息
                            var cameraConfigInfoModel = await _barcodeScannerCameraConfigRepository.FirstOrDefault(f =>
                                f.SerialNumber.Equals(savedImageInfo.CameraSerialNumber), stoppingToken);

                            var insert = await _imageRepository.Insert(new ImageInfoModel() {
                                BarcodeId = barCodeInfoModel.Id,
                                CameraName = cameraConfigInfoModel?.Name ?? string.Empty,
                                CameraSerialNumber = savedImageInfo.CameraSerialNumber,
                                CustomCameraName = cameraConfigInfoModel?.CustomName ?? string.Empty,
                                LocalPath = savedImageInfo.FilePath ?? string.Empty,
                                Type = 0
                            }, stoppingToken);

                            if (!insert) {
                                _savedImageItems.Enqueue(savedImageInfo);
                            }
                            /*barCodeInfoModel.BarcodeImagePath = savedImageInfo.FilePath;
                            var update = await _barCodeRepository.Update(barCodeInfoModel, stoppingToken);
                            if (!insert) {
                                _savedImageItems.Enqueue(savedImageInfo);
                            }*/
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
                            /*var insert = await _panoramaImageRepository.Insert(new PanoramaImageInfoModel() {
                                PanoramaImagePath = savedImageInfo.FilePath,
                                BarcodeId = barCodeInfoModel.Id
                            }, stoppingToken);*/
                            //获取相机信息
                            var cameraConfigInfoModel = await _barcodeScannerCameraConfigRepository.FirstOrDefault(f =>
                                f.SerialNumber.Equals(savedImageInfo.CameraSerialNumber), stoppingToken);

                            var insert = await _imageRepository.Insert(new ImageInfoModel() {
                                BarcodeId = barCodeInfoModel.Id,
                                CameraName = cameraConfigInfoModel?.Name ?? string.Empty,
                                CameraSerialNumber = savedImageInfo.CameraSerialNumber,
                                CustomCameraName = cameraConfigInfoModel?.CustomName ?? string.Empty,
                                LocalPath = savedImageInfo.FilePath ?? string.Empty,
                                Type = 1
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

                var isSorting = _instructionItems.TryDequeue(out var sortingModel);
                if (isSorting && sortingModel is not null) {
                    //取出对应条码id(根据条码、扫码时间)
                    var barCodeInfoModel = await _barCodeRepository.FirstOrDefault(f => f.Barcode.Equals(sortingModel.BarCode) &&
                        f.ScanTime.Equals(sortingModel.ScanTime), stoppingToken);
                    if (barCodeInfoModel is not null) {
                        //存到表
                        var insert = await _sortingRepository.Insert(new SortingInfoModel() {
                            BarcodeId = barCodeInfoModel.Id,
                            ChecksumProtocolName = sortingModel.ChecksumProtocolName,
                            CommandTarget = sortingModel.CommandTarget,
                            CommunicationMethod = sortingModel.CommunicationMethod,
                            ExitId = sortingModel.ExitId,
                            IsCreatedByLowerMachine = sortingModel.IsCreatedByLowerMachine,
                            IsSortingUsed = true,
                            LogisticsId = sortingModel.LogisticsId,
                            SentInstruction = sortingModel.SentInstruction,
                            SortingMode = sortingModel.SortingMode,
                            PackageCreationInstruction = sortingModel.PackageCreationInstruction,
                            PackageCreationTime = sortingModel.PackageCreationTime,
                        }, stoppingToken);
                        if (!insert) {
                            _instructionItems.Enqueue(sortingModel);
                        }
                    }
                    else {
                        _instructionItems.Enqueue(sortingModel);
                    }
                }
                await Task.Delay(50, stoppingToken);
            }
        }
    }
}