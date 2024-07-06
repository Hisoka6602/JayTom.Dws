using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using JayTom.Dws.Data.VideoApiData;
using JayTom.Dws.Domain.Dto.VideoApi;
using JayTom.Dws.Domain.Dto.CloudApiDto;
using JayTom.Dws.Domain.Repository.VideoApiData;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JayTom.Dws.Domain.Service.VideoApi {

    public class VideoBarCodeService : IVideoBarCodeService {
        private readonly IVideoBarCodeRepository _videoBarCodeRepository;
        private readonly IVideoNodeImageRepository _videoNodeImageRepository;
        private readonly IVideoScanNodeRepository _videoScanNodeRepository;

        public VideoBarCodeService(IVideoBarCodeRepository videoBarCodeRepository,
            IVideoNodeImageRepository videoNodeImageRepository,
            IVideoScanNodeRepository videoScanNodeRepository) {
            _videoBarCodeRepository = videoBarCodeRepository;
            _videoNodeImageRepository = videoNodeImageRepository;
            _videoScanNodeRepository = videoScanNodeRepository;
        }

        public async Task<KeyValuePair<bool, object>> AddOrUpdateBarcodeInfo(BarcodeImageDto barcodeImageInfo, List<BarcodeImageDto> panoramaImageInfos, PackageDto scanNodeInfo, string rootImagePath) {
            try {
                var imageInfoModels = new List<VideoNodeImageInfoModel>();
                await Task.Yield();
                if (string.IsNullOrEmpty(rootImagePath)) {
                    rootImagePath = $"{System.AppDomain.CurrentDomain.BaseDirectory}Images";
                }
                if (barcodeImageInfo?.Image is not null) {
                    var barcodeImageRootPath = $"{rootImagePath}\\barcodeImages\\{DateTime.Now:yyyy}\\{DateTime.Now:MM}\\{DateTime.Now:dd}\\{DateTime.Now:HH}";

                    if (!Directory.Exists(barcodeImageRootPath)) {
                        Directory.CreateDirectory(barcodeImageRootPath);
                    }
                    var barcodeImagePath = $"{barcodeImageRootPath}\\{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.jpg";
                    barcodeImageInfo.Image?.Save(barcodeImagePath, ImageFormat.Jpeg);
                    barcodeImageInfo.Image?.Dispose();

                    imageInfoModels?.Add(new VideoNodeImageInfoModel() {
                        CameraName = barcodeImageInfo.CameraName,
                        CameraSerialNumber = barcodeImageInfo.CameraSerialNumber,
                        ImageType = 0,
                        Name = barcodeImageInfo.Name,
                        Path = barcodeImagePath,
                    });
                }
                if (panoramaImageInfos?.All(a => a.Image != null) == true) {
                    var panoramaRootImage = $"{rootImagePath}\\panoramaImages\\{DateTime.Now:yyyy}\\{DateTime.Now:MM}\\{DateTime.Now:dd}\\{DateTime.Now:HH}";

                    if (!Directory.Exists(panoramaRootImage)) {
                        Directory.CreateDirectory(panoramaRootImage);
                    }
                    var num = 0;
                    var videoNodeImageInfoModels = panoramaImageInfos.Select(s => {
                        var panoramaImagePath = $"{panoramaRootImage}\\{DateTimeOffset.Now.ToUnixTimeMilliseconds()}-{num}.jpg";
                        s.Image?.Save(panoramaImagePath, ImageFormat.Jpeg);
                        s.Image?.Dispose();
                        num++;
                        return new VideoNodeImageInfoModel {
                            CameraName = s.CameraName,
                            CameraSerialNumber = s.CameraSerialNumber,
                            ImageType = 1,
                            Name = s.Name,
                            Path = panoramaImagePath,
                        };
                    })?.ToList() ?? new List<VideoNodeImageInfoModel>();
                    imageInfoModels?.AddRange(videoNodeImageInfoModels);
                }
                //保存图片
                if (scanNodeInfo.BarCodeInfo is null) {
                    return new KeyValuePair<bool, object>(false, "保存失败,条码信息不能为空!");
                }
                if (scanNodeInfo.DeviceInfos is null) {
                    return new KeyValuePair<bool, object>(false, "保存失败,设备信息不能为空!");
                }
                var model = await _videoBarCodeRepository.FirstOrDefault(f => f.Barcode.Equals(scanNodeInfo.BarCodeInfo.Barcode));
                if (model is null) {
                    var videoBarCodeInfoModel = new VideoBarCodeInfoModel() {
                        Barcode = scanNodeInfo.BarCodeInfo.Barcode,
                        ScanTime = scanNodeInfo.BarCodeInfo.ScanTime,
                        TimestampedGuid = new DateTimeOffset(scanNodeInfo.BarCodeInfo.ScanTime).ToUnixTimeMilliseconds(),
                        VideoScanNodeInfos = new List<VideoScanNodeInfoModel>()
                        {
                            new()
                            {
                                Name = scanNodeInfo.DeviceInfos.NodeName,
                                ScanTime = scanNodeInfo.BarCodeInfo.ScanTime,
                                VideoNodeImageInfos = imageInfoModels,
                                VideoNvrCameraBindingInfos =scanNodeInfo?.CloudNvrCameraBindingInfos?
                            .Select(s => new VideoNvrCameraBindingInfoModel {
                                BarcodeScannerSerialNumber = s.BarcodeScannerSerialNumber,
                                Channel = s.Channel,
                                IpAddress = s.IpAddress,
                                Password = s.Password,
                                Port = s.Port,
                                Username = s.Username
                            })?.ToList() ?? new List<VideoNvrCameraBindingInfoModel>()
                            }
                        },
                    };

                    var insert = await _videoBarCodeRepository.Insert(videoBarCodeInfoModel);
                    if (insert) {
                        return new KeyValuePair<bool, object>(true, videoBarCodeInfoModel);
                    }
                }
                else {
                    var videoScanNodeInfoModel = new VideoScanNodeInfoModel() {
                        BarcodeId = model.Id,
                        Name = scanNodeInfo.DeviceInfos.NodeName,
                        ScanTime = scanNodeInfo.BarCodeInfo.ScanTime,
                        VideoNodeImageInfos = imageInfoModels,
                        VideoNvrCameraBindingInfos = scanNodeInfo?.CloudNvrCameraBindingInfos?
                            .Select(s => new VideoNvrCameraBindingInfoModel {
                                BarcodeScannerSerialNumber = s.BarcodeScannerSerialNumber,
                                Channel = s.Channel,
                                IpAddress = s.IpAddress,
                                Password = s.Password,
                                Port = s.Port,
                                Username = s.Username
                            })?.ToList() ?? new List<VideoNvrCameraBindingInfoModel>()
                    };
                    var update = await _videoScanNodeRepository.Update(videoScanNodeInfoModel);
                    if (update) {
                        return new KeyValuePair<bool, object>(true, videoScanNodeInfoModel);
                    }
                }

                return new KeyValuePair<bool, object>(false, "保存失败");
            }
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, $"保存失败:{e.Message}");
            }
        }

        public async Task<KeyValuePair<bool, object>> GroupedNodeNames(CancellationToken token = default) {
            //获取节点的分组返回
            var (key, value) = await _videoScanNodeRepository.GroupedNodeNames(token);
            return new KeyValuePair<bool, object>(key, value);
        }

        public async Task<KeyValuePair<bool, object>> GetBarcodeInfos(string barCode, DateTime? nodeStartDateTime, DateTime? nodeEndDateTime, string? nodeName,
            string? cameraSerialNumber, string? cameraName, int pageIndex = 0, int pageSize = 1000,
            CancellationToken token = default) {
            return await _videoBarCodeRepository.GetBarcodeInfos(barCode,
                nodeStartDateTime,
                nodeEndDateTime,
                nodeName,
                cameraSerialNumber,
                cameraName,
                pageIndex, pageSize, token);
        }

        public async Task<KeyValuePair<bool, object>> GetBarcodeTotal(string barCode, DateTime? nodeStartDateTime, DateTime? nodeEndDateTime, string? nodeName,
            string? cameraSerialNumber, string? cameraName, CancellationToken token = default) {
            //获取条数
            try {
                var (key, value) = await _videoBarCodeRepository.GetBarcodeTotal(barCode,
                    nodeStartDateTime,
                    nodeEndDateTime, nodeName, cameraSerialNumber,
                    cameraName, token);
                return new KeyValuePair<bool, object>(key, value);
            }
            catch (Exception e) {
                Console.WriteLine(e);

                return new KeyValuePair<bool, object>(false, e.Message);
            }
        }

        public async Task<KeyValuePair<bool, object>> BarcodeTotalForDate(DateTime date) {
            try {
                var total = await _videoBarCodeRepository.
                    Total(w => w.ScanTime >= date.Date &&
                               w.ScanTime <= date.Date.AddDays(1).AddSeconds(-1));

                return new KeyValuePair<bool, object>(true, total);
            }
            catch (Exception e) {
                Console.WriteLine(e);

                return new KeyValuePair<bool, object>(false, e.Message);
            }
        }

        public async Task<KeyValuePair<bool, object>> BarcodeTotalForDateBetween(DateTime startDate, DateTime endDate) {
            try {
                var total = await _videoBarCodeRepository.
                Total(w => w.ScanTime >= startDate.Date &&
                               w.ScanTime <= endDate.Date.AddDays(1).AddSeconds(-1));

                return new KeyValuePair<bool, object>(true, total);
            }
            catch (Exception e) {
                Console.WriteLine(e);

                return new KeyValuePair<bool, object>(false, e.Message);
            }
        }

        public async Task<KeyValuePair<bool, object>> CleanupDataDaysAgo(int days, CancellationToken token = default) {
            try {
                var scanNodeInfoModels = await _videoScanNodeRepository.GetScanNodeInfos(s =>
                    s.ScanTime <= DateTime.Today.AddDays(0 - days), token);

                if (scanNodeInfoModels?.Any() == true) {
                    foreach (var videoScanNodeInfoModel in scanNodeInfoModels) {
                        if (videoScanNodeInfoModel.VideoNodeImageInfos?.Any() == true) {
                            foreach (var videoNodeImageInfoModel in videoScanNodeInfoModel.VideoNodeImageInfos) {
                                //删除图片
                                File.Delete(videoNodeImageInfoModel.Path);
                            }
                        }
                    }
                }

                var videoBarCodeInfoModels = await _videoBarCodeRepository.Select(s =>
                    s.ScanTime <= DateTime.Today.AddDays(0 - days), o => o.Id, token);
                if (videoBarCodeInfoModels?.Any() == true) {
                    await _videoBarCodeRepository.DeleteRange(videoBarCodeInfoModels, token);
                }

                return new KeyValuePair<bool, object>(true, "删除成功");
            }
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, e.Message);
            }
        }

        //保存图片方法
    }
}