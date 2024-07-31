using NPOI.Util;
using Newtonsoft.Json;
using JayTom.Dws.CloudApi.Vo;
using JayTom.Dws.CloudApi.Do;
using JayTom.Dws.Data.Package;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JayTom.Dws.CloudApi.Utils;
using JayTom.Dws.Application.Dto;
using Microsoft.Extensions.Hosting;
using JayTom.Dws.CloudApi.Attributes;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Domain.Dto.CloudApiDto;
using JayTom.Dws.Application.Service.CloudApi;

namespace JayTom.Dws.CloudApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class PackageController : ControllerBase {
        private readonly ICloudAppService _cloudAppService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static string? _saveImagePath;

        public PackageController(ICloudAppService cloudAppService,
            IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor) {
            _cloudAppService = cloudAppService;
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 上传数据
        /// </summary>
        /// <param name="barcodeImage"></param>
        /// <param name="panoramaImages"></param>
        /// <param name="packageInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("UploadPackageInfo")]
        public async Task<JsonResult> UploadPackageInfo([FromForm] IFormFile? barcodeImage,
            [FromForm] List<IFormFile>? panoramaImages,
            [FromForm][JsonValidation<PackageDto>(
                ErrorMessage = "packageInfo类型不正确无法解析成Json")]
            [PackageInfoFidleNotNull]
            string packageInfo,
            CancellationToken cancellationToken) {
            _saveImagePath ??= _webHostEnvironment.WebRootPath;
            var packageDto = JsonConvert.DeserializeObject<PackageDto>(packageInfo);
            if (packageDto is not null) {
                /*//临时测试
                var random = new Random();
                var randomType = Enum.GetValues(typeof(AbnormalSortingType))
                    .OfType<AbnormalSortingType>()
                    .OrderBy(x => random.Next())
                    .FirstOrDefault();
                packageDto.SortingInfo ??= new SortingInfoDto();
                packageDto.SortingInfo.IsAbnormalSorting = true;
                packageDto.SortingInfo.AbnormalSortingType = randomType;
                packageDto.SortingInfo.IsSortingUsed = true;*/
                //扫码图
                if (barcodeImage is not null) {
                    var barcodeImageInfo = Path.GetFileNameWithoutExtension(barcodeImage.FileName)?.Split("_");
                    if (barcodeImageInfo?.Any() == true) {
                        var imageInfoDto = packageDto.ImageInfos?.FirstOrDefault(f => f.CameraSerialNumber.Equals(barcodeImageInfo[0]) && f.Type == 0);
                        if (imageInfoDto is not null) {
                            imageInfoDto.Image = FileUtils.ConvertIFormFileToBitmap(barcodeImage);
                        }
                    }
                }
                //全景图
                if (panoramaImages?.Any() == true) {
                    foreach (var panoramaImage in panoramaImages) {
                        var split = Path.GetFileNameWithoutExtension(panoramaImage.FileName)?.Split("_");

                        if (split?.Any() == true) {
                            var imageInfoDto = packageDto.ImageInfos?.FirstOrDefault(f => f.CameraSerialNumber.Equals(split[0]) && f.Type == 1);
                            if (imageInfoDto is not null) {
                                imageInfoDto.Image = FileUtils.ConvertIFormFileToBitmap(panoramaImage);
                            }
                        }
                    }
                }

                var request = _httpContextAccessor.HttpContext?.Request;
                var webImagePath = $"{request?.Scheme}://{request?.Host}/scr";
                var (key, value) = await _cloudAppService.SavePackageInfo(packageDto, _saveImagePath,
                    webImagePath,
                    cancellationToken);
                return key ? JsonResultVo.Success("保存成功") : JsonResultVo.Fail("保存失败");
            }
            return JsonResultVo.Fail("packageInfo信息为空");
        }

        /// <summary>
        /// 数据-查询详细列表(条件、分页)
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("Packages")]
        public async Task<JsonResult> GetPackages([FromBody] PackagesDo param,
            CancellationToken cancellationToken) {
            // 查询数据库，返回符合条件的 PackageDto 列表

            var (key, value) = await _cloudAppService.GetPackages(param.Barcode,
                param.StartScanTime,
                param.EndScanTime,
                param.CameraSerialNumber,
                param.MinWeight,
                param.MaxWeight,
                param.RequestStatus,
                param.PhysicalExit,
                param.SentInstruction,
                param.LogisticsName,
                param.ThreeSegmentCode,
                param.NodeName,
                param.DeviceName,
                param.PageIndex,
                param.PageSize, cancellationToken);
            if (key && value is PackageListInfoDto dto) {
                return JsonResultVo.Success($"查询成功,返回{dto.PackageInfos.Count}条数据", dto.Total, dto.PackageInfos.Select(s => new PackageDto() {
                    PackageCreateTime = s.PackageCreateTime,
                    PackageTimestamped = s.PackageTimestamped,
                    Other = s.Other,
                    BarCodeInfo = new BarCodeInfoDto() {
                        Barcode = s.BarCodeInfo?.Barcode ?? string.Empty,
                        CameraSerialNumber = s.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
                        Source = s.BarCodeInfo?.Source ?? SourceType.None,
                        ScanTime = s.BarCodeInfo?.ScanTime ?? DateTime.MinValue,
                    },
                    WeightInfo = new WeightInfoDto() {
                        CreateTime = s.WeightInfo?.CreateTime ?? DateTime.MinValue,
                        FormattedWeight = s.WeightInfo?.FormattedWeight ?? 0,
                        OriginalText = s.WeightInfo?.OriginalText ?? string.Empty,
                        SourceType = s.WeightInfo?.SourceType ?? SourceType.None,
                        WeighingMode = s.WeightInfo?.WeighingMode ?? WeighingMode.None,
                    },
                    VolumeInfo = new VolumeInfoDto() {
                        CreateTime = s.VolumeInfo?.CreateTime ?? DateTime.MinValue,
                        FormattedHeight = s.VolumeInfo?.FormattedHeight ?? 0,
                        FormattedWidth = s.VolumeInfo?.FormattedWidth ?? 0,
                        FormattedVolume = s.VolumeInfo?.FormattedVolume ?? 0,
                        FormattedLength = s.VolumeInfo?.FormattedLength ?? 0,
                        OriginalText = s.VolumeInfo?.OriginalText ?? string.Empty,
                        SourceType = s.VolumeInfo?.SourceType ?? SourceType.None,
                    },
                    UploadInfo = new UploadInfoDto() {
                        ApiExceptionType = s.UploadInfo?.ApiExceptionType ?? ApiExceptionType.None,
                        DurationInSeconds = s.UploadInfo?.DurationInSeconds ?? 0,
                        ExceptionMessage = s.UploadInfo?.ExceptionMessage ?? string.Empty,
                        InterfaceParameters = s.UploadInfo?.InterfaceParameters ?? string.Empty,
                        RequestContent = s.UploadInfo?.RequestContent ?? string.Empty,
                        RequestStatus = s.UploadInfo?.RequestStatus ?? UploadStatus.NotUploaded,
                        RequestTime = s.UploadInfo?.RequestTime ?? DateTime.MinValue,
                        RequestUrl = s.UploadInfo?.RequestUrl ?? string.Empty,
                        ResponseContent = s.UploadInfo?.ResponseContent ?? string.Empty,
                        ResponseTime = s.UploadInfo?.ResponseTime ?? DateTime.MinValue,
                    },
                    ExitInfo = new ExitInfoDto() {
                        PhysicalExit = s.ExitInfo?.PhysicalExit ?? string.Empty,
                        PhysicalExitId = s.ExitInfo?.PhysicalExitId ?? 0,
                        TheoreticalExit = s.ExitInfo?.TheoreticalExit ?? string.Empty,
                    },
                    SortingInfo = new SortingInfoDto() {
                        ConnectionName = s.SortingInfo?.ConnectionName ?? string.Empty,
                        ChecksumProtocolName = s.SortingInfo?.ChecksumProtocolName ?? string.Empty,
                        CommunicationMethod = s.SortingInfo?.CommunicationMethod ?? CommunicationsType.None,
                        IsCreatedByLowerMachine = s.SortingInfo?.IsCreatedByLowerMachine ?? false,
                        IsSortingUsed = s.SortingInfo?.IsSortingUsed ?? false,
                        InstructionInfos = s.SortingInfo?.InstructionInfos?.Select(s1 => new InstructionInfoDto {
                            InstructionType = s1.InstructionType,
                            InstructionContent = s1.InstructionContent,
                            InstructionGeneratedTime = s1.InstructionGeneratedTime
                        })?.ToList(),
                        SortingMode = s.SortingInfo?.SortingMode ?? SortMode.None,
                    },
                    LogisticsInfo = new LogisticsInfoDto() {
                        LogisticsName = s.LogisticsInfo?.LogisticsName ?? string.Empty,
                        LogisticsCode = s.LogisticsInfo?.LogisticsCode ?? string.Empty,
                    },
                    OcrInfo = new OcrInfoDto() {
                        CameraSerialNumber = s.OcrInfo?.CameraSerialNumber ?? string.Empty,
                        ThreeSegmentCode = s.OcrInfo?.ThreeSegmentCode ?? string.Empty,
                        RecognizeTime = s.OcrInfo?.RecognizeTime ?? DateTime.MinValue,
                        ElapsedMilliseconds = s.OcrInfo?.ElapsedMilliseconds ?? 0,
                        SubmitTimestamp = s.OcrInfo?.SubmitTimestamp ?? 0,
                        IsUseOcr = s.OcrInfo?.IsUseOcr ?? false,
                        OriginalContent = s?.OcrInfo?.OriginalContent ?? string.Empty,
                        VirtualNumberLast4 = s?.OcrInfo?.VirtualNumberLast4 ?? string.Empty,
                        OcrDetailedInfos = s?.OcrInfo?.OcrDetailedInfos?.Select(s1 =>
                            new OcrDetailedInfoDto {
                                Address = s1.Address,
                                InformationType = s1.InformationType,
                                Name = s1.Name,
                                Phone = s1.Phone
                            })?.ToList(),
                    },
                    ImageInfos = s.ImageInfos?.Select(s1 => new ImageInfoDto {
                        CameraSerialNumber = s1.CameraSerialNumber,
                        CameraName = s1.CameraName,
                        CustomCameraName = s1.CustomCameraName,
                        ImageUrl = s1.ImageUrl,
                        Type = s1.Type,
                    })?.ToList(),
                    DeviceInfo = new DeviceInfoDto() {
                        DeviceName = s.DeviceInfo?.DeviceName ?? string.Empty,
                        MachineCode = s.DeviceInfo?.MachineCode ?? string.Empty,
                        NodeName = s.DeviceInfo?.NodeName ?? string.Empty,
                    }
                }));
            }
            else {
                return JsonResultVo.Fail(value?.ToString() ?? string.Empty);
            }
        }

        /// <summary>
        /// 统计-查询统计数据
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("Statistics")]
        public async Task<JsonResult> GetStatistics([FromBody] StatisticsDo param, CancellationToken cancellationToken) {
            var (key, value) = await _cloudAppService.GetStatistics(param.StartDateTime,
                param.EndDateTime,
                param.DeviceName,
                cancellationToken);
            if (key && value is PackageStatisticsDto info) {
                return JsonResultVo.Success("查询成功", data: info);
            }
            return JsonResultVo.Fail(value?.ToString() ?? string.Empty);
        }
    }
}