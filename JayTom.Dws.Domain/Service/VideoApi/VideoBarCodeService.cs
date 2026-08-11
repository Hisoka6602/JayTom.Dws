using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Data.VideoApiData;
using JayTom.Dws.Domain.Dto.VideoApi;
using JayTom.Dws.Domain.Dto.CloudApiDto;
using JayTom.Dws.Domain.Repository.VideoApi;
using JayTom.Dws.Domain.Repository.VideoApiData;
using JayTom.Dws.Domain.Service.VideoApi;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JayTom.Dws.Infrastructure.Service.VideoApi {

    public class VideoBarCodeService : IVideoBarCodeService {
        private readonly IVideoPackageRepository _videoPackageRepository;

        /*private readonly IVideoBarCodeRepository _videoBarCodeRepository;
        private readonly IVideoNodeImageRepository _videoNodeImageRepository;
        private readonly IVideoScanNodeRepository _videoScanNodeRepository;

        public VideoBarCodeService(IVideoBarCodeRepository videoBarCodeRepository,
            IVideoNodeImageRepository videoNodeImageRepository,
            IVideoScanNodeRepository videoScanNodeRepository) {
            _videoBarCodeRepository = videoBarCodeRepository;
            _videoNodeImageRepository = videoNodeImageRepository;
            _videoScanNodeRepository = videoScanNodeRepository;
        }*/

        public VideoBarCodeService(IVideoPackageRepository videoPackageRepository) {
            _videoPackageRepository = videoPackageRepository;
        }

        public async Task<KeyValuePair<bool, object>> AddOrUpdateBarcodeInfo(BarcodeImageDto barcodeImageInfo, List<BarcodeImageDto> panoramaImageInfos, PackageDto packageInfo, string rootImagePath) {
            try {
                var imageInfoModels = new List<ImageInfoModel>();
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
                    barcodeImageInfo.Image?.As<Bitmap>().Save(barcodeImagePath, ImageFormat.Jpeg);
                    barcodeImageInfo.Image?.Dispose();

                    imageInfoModels?.Add(new ImageInfoModel() {
                        CameraName = barcodeImageInfo.CameraName,
                        CameraSerialNumber = barcodeImageInfo.CameraSerialNumber,
                        LocalPath = barcodeImagePath,
                        Type = 0,
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
                            s.Image?.As<Bitmap>().Save(panoramaImagePath, ImageFormat.Jpeg);
                        s.Image?.Dispose();
                        num++;
                        return new ImageInfoModel {
                            CameraName = s.CameraName,
                            CameraSerialNumber = s.CameraSerialNumber,
                            Type = 1,
                            LocalPath = panoramaImagePath,
                        };
                    })?.ToList() ?? new List<ImageInfoModel>();
                    imageInfoModels?.AddRange(videoNodeImageInfoModels);
                }
                //保存图片
                if (packageInfo.BarCodeInfo is null) {
                    return new KeyValuePair<bool, object>(false, "保存失败,条码信息不能为空!");
                }
                if (packageInfo.DeviceInfo is null) {
                    return new KeyValuePair<bool, object>(false, "保存失败,设备信息不能为空!");
                }

                var packageInfoModel = new PackageInfoModel() {
                    PackageCreateTime = packageInfo.PackageCreateTime,
                    PackageTimestamped = packageInfo.PackageTimestamped,
                    NvrInfos = packageInfo.CloudNvrCameraBindingInfos?.Select(s =>
                        new NvrInfoModel {
                            Channel = s.Channel,
                            IpAddress = s.IpAddress,
                            Password = s.Password,
                            Port = s.Port,
                            Username = s.Username,
                        })?.ToList() ?? new List<NvrInfoModel>(),
                    ImageInfos = imageInfoModels,
                    DeviceInfo = new DeviceInfoModel() {
                        DeviceName = packageInfo.DeviceInfo?.DeviceName ?? string.Empty,
                        MachineCode = packageInfo.DeviceInfo?.MachineCode ?? string.Empty,
                        NodeName = packageInfo.DeviceInfo?.NodeName ?? string.Empty,
                    },
                    BarCodeInfo = new BarCodeInfoModel() {
                        Barcode = packageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                        SerialNumber = packageInfo.BarCodeInfo?.SerialNumber ?? string.Empty,
                        DisplayIdentifier = packageInfo.BarCodeInfo?.DisplayIdentifier ?? string.Empty,
                        ScanTime = packageInfo.BarCodeInfo?.ScanTime ?? DateTime.MinValue,
                        Source = packageInfo.BarCodeInfo?.Source ?? SourceType.None,
                    }
                };

                var total = await _videoPackageRepository.Total(t =>
                    t.PackageTimestamped.
                        Equals(packageInfoModel.PackageTimestamped));
                bool insertOrUpdate;
                if (total > 0) {
                    insertOrUpdate = await _videoPackageRepository.Update(packageInfoModel);
                }
                else {
                    insertOrUpdate = await _videoPackageRepository.Insert(packageInfoModel);
                }

                return new KeyValuePair<bool, object>(insertOrUpdate, $"保存{(insertOrUpdate ? "成功" : "失败")}");
            }
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, $"保存失败:{e.Message}");
            }
        }

        public async Task<KeyValuePair<bool, object>> GroupedNodeNames(CancellationToken token = default) {
            //获取节点的分组返回
            var (key, value) = await _videoPackageRepository.SelectNodeInfos(token);
            return new KeyValuePair<bool, object>(key, value);
        }

        public async Task<KeyValuePair<bool, object>> GetBarcodeInfos(string barCode, DateTime? nodeStartDateTime, DateTime? nodeEndDateTime, string? nodeName,
            string? cameraSerialNumber, string? cameraName, int pageIndex = 0, int pageSize = 1000,
            CancellationToken token = default) {
            var (key, value) = await _videoPackageRepository.SelectPackageOrderByDescending(w =>
                    w.BarCodeInfo != null &&
                    w.DeviceInfo != null &&
                    (string.IsNullOrEmpty(barCode) || w.BarCodeInfo.Barcode.Contains(barCode)) &&
                    (nodeStartDateTime == null || w.BarCodeInfo.ScanTime >= nodeStartDateTime) &&
                    (nodeEndDateTime == null || w.BarCodeInfo.ScanTime <= nodeEndDateTime) &&
                    (nodeName == null || w.DeviceInfo.NodeName.Equals(nodeName)), o => o.PackageCreateTime,
                pageIndex, pageSize, token);

            return new KeyValuePair<bool, object>(key, value);
        }

        public async Task<KeyValuePair<bool, object>> GetBarcodeTotal(string barCode, DateTime? nodeStartDateTime, DateTime? nodeEndDateTime, string? nodeName,
            string? cameraSerialNumber, string? cameraName, CancellationToken token = default) {
            //获取条数

            var total = await _videoPackageRepository.Total(w =>
                w.BarCodeInfo != null &&
                w.DeviceInfo != null &&
                (string.IsNullOrEmpty(barCode) || w.BarCodeInfo.Barcode.Contains(barCode)) &&
                (nodeStartDateTime == null || w.BarCodeInfo.ScanTime >= nodeStartDateTime) &&
                (nodeStartDateTime == null || w.BarCodeInfo.ScanTime <= nodeEndDateTime) &&
                (nodeName == null || w.DeviceInfo.NodeName.Equals(nodeName)), token);
            return new KeyValuePair<bool, object>(true, total);
        }

        public async Task<KeyValuePair<bool, object>> BarcodeTotalForDate(DateTime date) {
            var total = await _videoPackageRepository.Total(w =>
                w.PackageCreateTime >= date &&
                w.PackageCreateTime <= date.Date.AddDays(1).AddSeconds(-1));

            return new KeyValuePair<bool, object>(true, total);
        }

        public async Task<KeyValuePair<bool, object>> BarcodeTotalForDateBetween(DateTime startDate, DateTime endDate) {
            var total = await _videoPackageRepository.Total(w =>
                w.PackageCreateTime >= startDate &&
                w.PackageCreateTime <= endDate);

            return new KeyValuePair<bool, object>(true, total);
        }

        public async Task<KeyValuePair<bool, object>> CleanupDataDaysAgo(int days, CancellationToken token = default) {
            try {
                do {
                    var (key, value) = await _videoPackageRepository.SelectPackageOrderByDescending(s =>
                            s.PackageCreateTime <= DateTime.Today.AddDays(0 - days), o => o.PackageCreateTime,
                        0, 1000, token);
                    if (key && value.Any()) {
                        foreach (var packageInfoModel in value) {
                            if (packageInfoModel.ImageInfos?.Any() == true) {
                                foreach (var imageInfoModel in packageInfoModel.ImageInfos) {
                                    //删除图片
                                    File.Delete(imageInfoModel.LocalPath);
                                }
                            }
                        }

                        await _videoPackageRepository.DeleteRange(value, token);
                    }
                    else {
                        break;
                    }
                } while (true);
                return new KeyValuePair<bool, object>(true, "删除成功");
            }
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, e.Message);
            }
        }

        //保存图片方法
    }
}
