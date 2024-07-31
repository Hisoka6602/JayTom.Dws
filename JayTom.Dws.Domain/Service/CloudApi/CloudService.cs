using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Data.CloudApiData;
using JayTom.Dws.Domain.Dto.CloudApiDto;
using JayTom.Dws.Domain.Repository.CloudApi;

namespace JayTom.Dws.Domain.Service.CloudApi {

    public class CloudService : ICloudService {
        private readonly ICloudPackageRepository _cloudPackageRepository;
        private readonly IExceptionTypeRepository _exceptionTypeRepository;
        private readonly IExceptionMatchRepository _exceptionMatchRepository;

        public CloudService(ICloudPackageRepository cloudPackageRepository,
            IExceptionTypeRepository exceptionTypeRepository,
            IExceptionMatchRepository exceptionMatchRepository) {
            _cloudPackageRepository = cloudPackageRepository;
            _exceptionTypeRepository = exceptionTypeRepository;
            _exceptionMatchRepository = exceptionMatchRepository;
        }

        public async Task<KeyValuePair<bool, object>> SavePackageInfo(PackageDto packageInfo, CancellationToken cancellationToken = default) {
            //判断更新还是添加(需要通知事件)
            try {
                var insert = await _cloudPackageRepository.Insert(new PackageInfoModel() {
                    PackageCreateTime = packageInfo.PackageCreateTime,
                    PackageTimestamped = packageInfo.PackageTimestamped,
                    BarCodeInfo = new BarCodeInfoModel() {
                        Barcode = packageInfo.BarCodeInfo?.Barcode ?? string.Empty,
                        CameraSerialNumber = packageInfo.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                        ScanTime = packageInfo.BarCodeInfo?.ScanTime ?? DateTime.Now,
                        Source = packageInfo.BarCodeInfo?.Source ?? SourceType.None,
                    },
                    WeightInfo = new WeightInfoModel() {
                        CreateTime = packageInfo.WeightInfo?.CreateTime ?? DateTime.MinValue,
                        FormattedWeight = packageInfo.WeightInfo?.FormattedWeight ?? 0,
                        OriginalText = packageInfo.WeightInfo?.OriginalText ?? string.Empty,
                        SourceType = packageInfo.WeightInfo?.SourceType ?? SourceType.None,
                        WeighingMode = packageInfo.WeightInfo?.WeighingMode ?? WeighingMode.None,
                    },
                    VolumeInfo = new VolumeInfoModel() {
                        CreateTime = packageInfo.VolumeInfo?.CreateTime ?? DateTime.MinValue,
                        FormattedHeight = packageInfo.VolumeInfo?.FormattedHeight ?? 0,
                        FormattedWidth = packageInfo.VolumeInfo?.FormattedWidth ?? 0,
                        FormattedVolume = packageInfo.VolumeInfo?.FormattedVolume ?? 0,
                        FormattedLength = packageInfo.VolumeInfo?.FormattedLength ?? 0,
                        OriginalText = packageInfo.VolumeInfo?.OriginalText ?? string.Empty,
                        SourceType = packageInfo.VolumeInfo?.SourceType ?? SourceType.None,
                    },
                    UploadInfo = new UploadInfoModel() {
                        ApiExceptionType = packageInfo.UploadInfo?.ApiExceptionType ?? ApiExceptionType.None,
                        DurationInSeconds = packageInfo.UploadInfo?.DurationInSeconds ?? 0,
                        ExceptionMessage = packageInfo.UploadInfo?.ExceptionMessage ?? string.Empty,
                        InterfaceParameters = packageInfo.UploadInfo?.InterfaceParameters ?? string.Empty,
                        RequestContent = packageInfo.UploadInfo?.RequestContent ?? string.Empty,
                        RequestStatus = packageInfo.UploadInfo?.RequestStatus ?? UploadStatus.NotUploaded,
                        RequestTime = packageInfo.UploadInfo?.RequestTime ?? DateTime.MinValue,
                        RequestUrl = packageInfo.UploadInfo?.RequestUrl ?? string.Empty,
                        ResponseContent = packageInfo.UploadInfo?.ResponseContent ?? string.Empty,
                        ResponseTime = packageInfo.UploadInfo?.ResponseTime ?? DateTime.MinValue,
                    },
                    ExitInfo = new ExitInfoModel() {
                        PhysicalExit = packageInfo.ExitInfo?.PhysicalExit ?? string.Empty,
                        TheoreticalExit = packageInfo.ExitInfo?.TheoreticalExit ?? string.Empty,
                        PhysicalExitId = packageInfo.ExitInfo?.PhysicalExitId ?? 0,
                    },
                    SortingInfo = new SortingInfoModel() {
                        ConnectionName = packageInfo.SortingInfo?.ConnectionName ?? string.Empty,
                        ChecksumProtocolName = packageInfo.SortingInfo?.ChecksumProtocolName ?? string.Empty,
                        CommunicationMethod = packageInfo.SortingInfo?.CommunicationMethod ?? CommunicationsType.None,
                        IsCreatedByLowerMachine = packageInfo.SortingInfo?.IsCreatedByLowerMachine ?? false,
                        InstructionInfos = packageInfo.SortingInfo?.InstructionInfos?
                            .Select(s => new InstructionInfoModel {
                                InstructionGeneratedTime = s.InstructionGeneratedTime,
                                InstructionType = s.InstructionType,
                                InstructionContent = s.InstructionContent,
                            }).ToList(),
                        SortingMode = packageInfo.SortingInfo?.SortingMode ?? SortMode.None,
                        IsSortingUsed = packageInfo.SortingInfo?.IsSortingUsed ?? false,
                        IsAbnormalSorting = packageInfo.SortingInfo?.IsAbnormalSorting ?? false,
                        AbnormalSortingType = packageInfo.SortingInfo?.AbnormalSortingType ?? AbnormalSortingType.None,
                    },
                    LogisticsInfo = new LogisticsInfoModel() {
                        LogisticsCode = packageInfo.LogisticsInfo?.LogisticsCode ?? string.Empty,
                        LogisticsName = packageInfo.LogisticsInfo?.LogisticsName ?? string.Empty,
                    },
                    OcrInfo = new OcrInfoModel() {
                        CameraSerialNumber = packageInfo.OcrInfo?.CameraSerialNumber ?? string.Empty,
                        ElapsedMilliseconds = packageInfo.OcrInfo?.ElapsedMilliseconds ?? 0,
                        SubmitTimestamp = packageInfo.OcrInfo?.SubmitTimestamp ?? 0,
                        ThreeSegmentCode = packageInfo.OcrInfo?.ThreeSegmentCode ?? string.Empty,
                        RecognizeTime = packageInfo.OcrInfo?.RecognizeTime ?? DateTime.MinValue,
                        VirtualNumberLast4 = packageInfo.OcrInfo?.VirtualNumberLast4 ?? string.Empty,
                        OriginalContent = packageInfo.OcrInfo?.OriginalContent ?? string.Empty,
                        OcrDetailedInfos = packageInfo.OcrInfo?.OcrDetailedInfos?.Select(s =>
                            new OcrDetailedInfoModel {
                                Address = s.Address,
                                InformationType = s.InformationType,
                                Name = s.Name,
                                Phone = s.Phone,
                            })?.ToList(),
                    },
                    ImageInfos = packageInfo.ImageInfos?.Select(s =>
                        new ImageInfoModel {
                            CameraSerialNumber = s.CameraSerialNumber,
                            CameraName = s.CameraName,
                            CustomCameraName = s.CustomCameraName,
                            ImageUrl = s.ImageUrl,
                            LocalPath = s.LocalPath,
                            Type = s.Type,
                        })?.ToList(),
                    DeviceInfo = new DeviceInfoModel() {
                        DeviceName = packageInfo.DeviceInfo?.DeviceName ?? string.Empty,
                        MachineCode = packageInfo.DeviceInfo?.MachineCode ?? string.Empty,
                        NodeName = packageInfo.DeviceInfo?.NodeName ?? string.Empty,
                    }
                }, cancellationToken);
                return new KeyValuePair<bool, object>(insert, insert ? "保存成功" : "保存失败");
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            return new KeyValuePair<bool, object>(false, "保存失败");
        }

        public async Task<KeyValuePair<bool, object>> GetPackages(string? barcode, DateTime? startScanTime, DateTime? endScanTime, string? cameraSerialNumber,
            double? minWeight, double? maxWeight, int? requestStatus, string? physicalExit, string? sentInstruction,
            string? logisticsName, string? threeSegmentCode, string? nodeName, string? deviceName, int pageIndex,
            int pageSize, CancellationToken cancellationToken) {
            var total = await _cloudPackageRepository.Total(w =>
                    w.BarCodeInfo != null &&
                    w.WeightInfo != null &&
                    (string.IsNullOrEmpty(barcode) || w.BarCodeInfo.Barcode.Contains(barcode)) &&
                    (startScanTime == null || w.BarCodeInfo.ScanTime >= startScanTime) &&
                    (endScanTime == null || w.BarCodeInfo.ScanTime <= endScanTime) &&
                    (string.IsNullOrEmpty(cameraSerialNumber) ||
                     w.BarCodeInfo.CameraSerialNumber.Contains(cameraSerialNumber)) &&
                    (minWeight == null || w.WeightInfo.FormattedWeight >= minWeight) &&
                    (maxWeight == null || w.WeightInfo.FormattedWeight <= maxWeight) &&
                    (requestStatus == null ||
                     (w.UploadInfo != null && w.UploadInfo.RequestStatus == (UploadStatus)requestStatus)) &&
                    (string.IsNullOrEmpty(physicalExit) ||
                     (w.ExitInfo != null && w.ExitInfo.PhysicalExit.Contains(physicalExit))) &&
                    (string.IsNullOrEmpty(sentInstruction) ||
                     (w.SortingInfo != null && w.SortingInfo.InstructionInfos != null &&
                      w.SortingInfo.InstructionInfos.Any(a => a.InstructionContent.Contains(sentInstruction)))) &&
                    (string.IsNullOrEmpty(logisticsName) ||
                     (w.LogisticsInfo != null && w.LogisticsInfo.LogisticsName.Contains(logisticsName))) &&
                    (string.IsNullOrEmpty(threeSegmentCode) ||
                     (w.OcrInfo != null && w.OcrInfo.ThreeSegmentCode.Contains(threeSegmentCode))) &&
                    (string.IsNullOrEmpty(nodeName) ||
                     (w.DeviceInfo != null && w.DeviceInfo.NodeName.Contains(nodeName))) &&
                    (string.IsNullOrEmpty(deviceName) ||
                     (w.DeviceInfo != null && w.DeviceInfo.DeviceName.Contains(deviceName)))
                , cancellationToken);
            if (total > 0) {
                var (key, value) = await _cloudPackageRepository.SelectPackageOrderByDescending(w =>
                        w.BarCodeInfo != null &&
                        w.WeightInfo != null &&
                        (string.IsNullOrEmpty(barcode) || w.BarCodeInfo.Barcode.Contains(barcode)) &&
                        (startScanTime == null || w.BarCodeInfo.ScanTime >= startScanTime) &&
                        (endScanTime == null || w.BarCodeInfo.ScanTime <= endScanTime) &&
                        (string.IsNullOrEmpty(cameraSerialNumber) ||
                         w.BarCodeInfo.CameraSerialNumber.Contains(cameraSerialNumber)) &&
                        (minWeight == null || w.WeightInfo.FormattedWeight >= minWeight) &&
                        (maxWeight == null || w.WeightInfo.FormattedWeight <= maxWeight) &&
                        (requestStatus == null ||
                         (w.UploadInfo != null && w.UploadInfo.RequestStatus == (UploadStatus)requestStatus)) &&
                        (string.IsNullOrEmpty(physicalExit) ||
                         (w.ExitInfo != null && w.ExitInfo.PhysicalExit.Contains(physicalExit))) &&
                        (string.IsNullOrEmpty(sentInstruction) ||
                         (w.SortingInfo != null && w.SortingInfo.InstructionInfos != null &&
                          w.SortingInfo.InstructionInfos.Any(a => a.InstructionContent.Contains(sentInstruction)))) &&
                        (string.IsNullOrEmpty(logisticsName) ||
                         (w.LogisticsInfo != null && w.LogisticsInfo.LogisticsName.Contains(logisticsName))) &&
                        (string.IsNullOrEmpty(threeSegmentCode) ||
                         (w.OcrInfo != null && w.OcrInfo.ThreeSegmentCode.Contains(threeSegmentCode))) &&
                        (string.IsNullOrEmpty(nodeName) ||
                         (w.DeviceInfo != null && w.DeviceInfo.NodeName.Contains(nodeName))) &&
                        (string.IsNullOrEmpty(deviceName) ||
                         (w.DeviceInfo != null && w.DeviceInfo.DeviceName.Contains(deviceName)))
                    , o => o.PackageCreateTime,
                    pageIndex, pageSize, cancellationToken);
                if (key) {
                    var packageListInfoDto = new PackageListInfoDto() {
                        PackageInfos = value,
                        Total = total,
                    };
                    return new KeyValuePair<bool, object>(true, packageListInfoDto);
                }
            }
            return new KeyValuePair<bool, object>(false, "未查询到数据");
        }

        public Task<KeyValuePair<bool, object>> GetStatistics(DateTime? startDateTime, DateTime? endDateTime, string? deviceName, CancellationToken cancellationToken) {
            return _cloudPackageRepository.GetStatistics(startDateTime, endDateTime, deviceName, cancellationToken);
        }

        public async Task<KeyValuePair<bool, object>> CleanupDataDaysAgo(int days, CancellationToken token = default) {
            var dateTime = DateTime.Now.AddDays(0 - days);
            var total = await _cloudPackageRepository.Total(w => w.BarCodeInfo != null &&
                                                                 w.BarCodeInfo.ScanTime < dateTime, token);
            if (total > 0) {
                var (key, value) = await _cloudPackageRepository.SelectPackage(w => w.BarCodeInfo != null &&
                        w.BarCodeInfo.ScanTime < dateTime,
                    o => o.Id, 0, total, token);
                if (key) {
                    await _cloudPackageRepository.DeleteRange(value, token);
                }

                return new KeyValuePair<bool, object>(true, "删除成功");
            }
            else {
                return new KeyValuePair<bool, object>(false, "未获取到相关数据");
            }
        }

        public async Task<KeyValuePair<bool, object>> AddExceptionType(string exceptionName, string exceptionColor, CancellationToken token = default) {
            var insertOrUpdate = await _exceptionTypeRepository.Insert(new ExceptionTypeInfoModel() {
                ExceptionColor = exceptionColor,
                ExceptionName = exceptionName
            }, token);
            return new KeyValuePair<bool, object>(insertOrUpdate, insertOrUpdate ? "操作成功" : "操作失败");
        }

        public async Task<KeyValuePair<bool, object>> UpdateExceptionType(long exceptionCategoryId, string exceptionName, string exceptionColor,
            CancellationToken token = default) {
            var insertOrUpdate = await _exceptionTypeRepository.Update(new ExceptionTypeInfoModel() {
                ExceptionColor = exceptionColor,
                ExceptionName = exceptionName,
                Id = exceptionCategoryId
            }, token);
            return new KeyValuePair<bool, object>(insertOrUpdate, insertOrUpdate ? "操作成功" : "操作失败");
        }

        public async Task<KeyValuePair<bool, object>> DeleteExceptionType(long exceptionCategoryId, CancellationToken token = default) {
            var model = await _exceptionTypeRepository.FirstOrDefault(f =>
                f.Id.Equals(exceptionCategoryId), token);
            if (model is not null) {
                var delete = await _exceptionTypeRepository.Delete(model, token);
                if (delete) {
                    return new KeyValuePair<bool, object>(true, "删除成功");
                }
            }
            return new KeyValuePair<bool, object>(false, "删除失败");
        }

        public async Task<KeyValuePair<bool, object>> AddExceptionRule(string keywords, string customRegex, int dataSource, string exceptionTypeName,
            long exceptionTypeId, int priority, CancellationToken token = default) {
            var insertOrUpdate = await _exceptionMatchRepository.Insert(new ExceptionMatchInfoModel() {
                Keywords = keywords,
                CustomRegex = customRegex,
                DataSource = dataSource,
                ExceptionTypeId = exceptionTypeId,
                Priority = priority,
            }, token);
            return new KeyValuePair<bool, object>(insertOrUpdate, insertOrUpdate ? "操作成功" : "操作失败");
        }

        public async Task<KeyValuePair<bool, object>> UpdateExceptionRule(long exceptionRuleId, string keywords, string customRegex, int dataSource,
            string exceptionTypeName, long exceptionTypeId, int priority, CancellationToken token = default) {
            var update = await _exceptionMatchRepository.Update(new ExceptionMatchInfoModel() {
                Id = exceptionRuleId,
                Keywords = keywords,
                CustomRegex = customRegex,
                DataSource = dataSource,
                ExceptionTypeId = exceptionTypeId,
                Priority = priority,
            }, token);
            return new KeyValuePair<bool, object>(update, update ? "操作成功" : "操作失败");
        }

        public async Task<KeyValuePair<bool, object>> DeleteExceptionRule(long exceptionRuleId, CancellationToken token = default) {
            var model = await _exceptionMatchRepository.FirstOrDefault(f =>
                f.Id.Equals(exceptionRuleId), token);
            if (model is not null) {
                var delete = await _exceptionMatchRepository.Delete(model, token);
                if (delete) {
                    return new KeyValuePair<bool, object>(true, "删除成功");
                }
            }

            return new KeyValuePair<bool, object>(false, "删除失败");
        }

        public async Task<KeyValuePair<bool, object>> ExceptionTypes(CancellationToken token = default) {
            var models = await _exceptionTypeRepository.MemoryCacheData();

            return new KeyValuePair<bool, object>(true, models?.Select(s => new ExceptionTypeDto {
                ExceptionColor = s.ExceptionColor,
                ExceptionName = s.ExceptionName,
                Id = s.Id
            })?.ToList() ?? new List<ExceptionTypeDto>());
        }

        public async Task<KeyValuePair<bool, object>> ExceptionRule(CancellationToken token = default) {
            var models = await _exceptionMatchRepository.MemoryCacheData();

            return new KeyValuePair<bool, object>(true, models.Select(s => new ExceptionRuleDto {
                Id = s.Id,
                CustomRegex = s.CustomRegex,
                DataSource = s.DataSource,
                ExceptionTypeId = s.ExceptionTypeId,
                Keywords = s.Keywords,
                Priority = s.Priority,
            })?.ToList() ?? new List<ExceptionRuleDto>());
        }
    }
}