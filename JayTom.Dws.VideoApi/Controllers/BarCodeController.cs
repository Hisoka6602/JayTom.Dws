using Newtonsoft.Json;
using FluentFTP.Helpers;
using Newtonsoft.Json.Linq;
using JayTom.Dws.VideoApi.Vo;
using JayTom.Dws.VideoApi.Do;
using Microsoft.AspNetCore.Mvc;
using JayTom.Dws.VideoApi.Utils;
using JayTom.Dws.Application.Dto;
using Org.BouncyCastle.Asn1.Ocsp;
using Microsoft.Extensions.Hosting;
using JayTom.Dws.Data.VideoApiData;
using JayTom.Dws.Domain.Dto.VideoApi;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using JayTom.Dws.Application.Service.VideoApi;
using JayTom.Dws.Domain.Repository.VideoApiData;
using JayTom.Dws.Infrastructure.Repository.VideoApiData;
using JayTom.Dws.Infrastructure.SignalR.VideoApi.SignalRMessageHub;

namespace JayTom.Dws.VideoApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class BarCodeController : ControllerBase {
        private readonly ILogger<BarCodeController> _logger;
        private readonly IVideoBarCodeAppService _videoBarCodeAppService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMessageHub _messageHub;
        private readonly IVideoBarCodeRepository _videoBarCodeRepository;

        private static string? _saveImagePath;

        public BarCodeController(ILogger<BarCodeController> logger,
            IVideoBarCodeAppService videoBarCodeAppService,
            IWebHostEnvironment hostEnvironment,
            IHttpContextAccessor httpContextAccessor,
            IMessageHub messageHub,
            IVideoBarCodeRepository videoBarCodeRepository) {
            _logger = logger;
            _videoBarCodeAppService = videoBarCodeAppService;
            _hostEnvironment = hostEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _messageHub = messageHub;
            _videoBarCodeRepository = videoBarCodeRepository;
        }

        [HttpPost("UploadBarcodeData")]
        public async Task<JsonResult> UploadBarcodeData([FromForm][NotNull] IFormFile barcodeImage,
            [FromForm] List<IFormFile> panoramaImages,
            [FromForm][NotNull] string jsonData,
            CancellationToken cancellationToken) {
            _saveImagePath ??= _hostEnvironment.WebRootPath;
            try {
                var scanNodeDto = JsonConvert.DeserializeObject<ScanNodeDto>(jsonData);
                if (scanNodeDto is not null) {
                    var strings = Path.GetFileNameWithoutExtension(barcodeImage.FileName)?.Split("_");
                    //判断是否已存在
                    var (key, value) = await _videoBarCodeAppService.AddOrUpdateBarcodeInfo(new BarcodeImageDto() {
                        CameraSerialNumber = strings?.Length > 0 ? strings[0] : string.Empty,
                        CameraName = strings?.Length > 1 ? strings[1] : string.Empty,
                        Image = FileUtils.ConvertIFormFileToBitmap(barcodeImage),
                        Name = barcodeImage.FileName
                    }, panoramaImages.Select(s => {
                        var split = Path.GetFileNameWithoutExtension(s.FileName)?.Split("_");
                        return new BarcodeImageDto {
                            CameraSerialNumber = split?.Length > 0 ? split[0] :
                                                                     string.Empty,
                            CameraName = split?.Length > 1 ? split[1] :
                                         string.Empty,
                            Image = FileUtils.ConvertIFormFileToBitmap(s),
                            Name = s.FileName
                        };
                    })?.ToList() ?? new List<BarcodeImageDto>(),
                        scanNodeDto, _saveImagePath);
                    if (key) {
                        if (value is VideoBarCodeInfoModel videoBarCodeInfoModel) {
                            //添加
                            var request = _httpContextAccessor.HttpContext?.Request;
                            _messageHub.UpDateNodes();
                            _messageHub.DataStatistics();
                            _messageHub.MessageItem(new MessageBarCodeItemInfo() {
                                BarCode = videoBarCodeInfoModel.Barcode,
                                CameraCustomName = videoBarCodeInfoModel.VideoScanNodeInfos?.
                                    FirstOrDefault()?.VideoNodeImageInfos?.
                                    Where(w => w.ImageType == 0)?.
                                    FirstOrDefault()?.CameraName ?? string.Empty,
                                CameraSerialNumber = videoBarCodeInfoModel.VideoScanNodeInfos?.
                                    FirstOrDefault()?.VideoNodeImageInfos?.
                                    Where(w => w.ImageType == 0)?.
                                    FirstOrDefault()?.CameraSerialNumber ?? string.Empty,
                                NodeName = videoBarCodeInfoModel.VideoScanNodeInfos?.FirstOrDefault()?.Name,
                                PanoramaImageItems = videoBarCodeInfoModel.VideoScanNodeInfos?.
                                    FirstOrDefault()?.VideoNodeImageInfos?.
                                    Where(w => w.ImageType == 1)?.
                                    Select(s => $"{request?.Scheme}://{request?.Host}/scr{s.Path.Replace(_hostEnvironment.WebRootPath, string.Empty).Replace("\\", "/")}")?.ToList() ?? new List<string>(),
                                ScanImageUrl = videoBarCodeInfoModel.VideoScanNodeInfos?.
                                    FirstOrDefault()?.VideoNodeImageInfos?.
                                    Where(w => w.ImageType == 0)?.
                                    Select(s => $"{request?.Scheme}://{request?.Host}/scr{s.Path.Replace(_hostEnvironment.WebRootPath, string.Empty).Replace("\\", "/")}")
                                    ?.ToList()?.FirstOrDefault() ?? string.Empty,
                                ScanTime = videoBarCodeInfoModel.VideoScanNodeInfos?.
                                    FirstOrDefault()?.ScanTime ?? DateTime.Now,
                                NvrCameraBindingItem = videoBarCodeInfoModel.VideoScanNodeInfos?.FirstOrDefault()?.
                                    VideoNvrCameraBindingInfos?.Select(s => new MessageNvrCameraBindingItemInfo {
                                        BarcodeScannerSerialNumber = s?.BarcodeScannerSerialNumber ?? string.Empty,
                                        Channel = s?.Channel ?? 0,
                                        IpAddress = s?.IpAddress ?? string.Empty,
                                        Password = s?.Password ?? string.Empty,
                                        Port = s?.Port ?? 0,
                                        Username = s?.Username ?? string.Empty
                                    })?.ToList() ?? new List<MessageNvrCameraBindingItemInfo>()
                            });
                        }
                        else if (value is VideoScanNodeInfoModel videoScanNodeInfoModel) {
                            //更新
                            var request = _httpContextAccessor.HttpContext?.Request;
                            var barCodeInfoModel = await _videoBarCodeRepository.
                                FirstOrDefault(f =>
                                        f.Id.Equals(videoScanNodeInfoModel.BarcodeId),
                                    cancellationToken);
                            _messageHub.MessageItem(new MessageBarCodeItemInfo() {
                                BarCode = barCodeInfoModel?.Barcode ?? string.Empty,

                                CameraCustomName = videoScanNodeInfoModel?.VideoNodeImageInfos?.
                                   Where(w => w.ImageType == 0)?.
                                   FirstOrDefault()?.CameraName ?? string.Empty,
                                CameraSerialNumber = videoScanNodeInfoModel?.VideoNodeImageInfos?.
                                   Where(w => w.ImageType == 0)?.
                                   FirstOrDefault()?.CameraSerialNumber ?? string.Empty,
                                NodeName = videoScanNodeInfoModel?.Name,
                                PanoramaImageItems = videoScanNodeInfoModel?.VideoNodeImageInfos?.
                                   Where(w => w.ImageType == 1)?.
                                   Select(s => $"{request?.Scheme}://{request?.Host}/scr{s.Path.Replace(_hostEnvironment.WebRootPath, string.Empty).Replace("\\", "/")}")?.ToList() ?? new List<string>(),
                                ScanImageUrl = videoScanNodeInfoModel?.VideoNodeImageInfos?.
                                   Where(w => w.ImageType == 0)?.
                                   Select(s => $"{request?.Scheme}://{request?.Host}/scr{s.Path.Replace(_hostEnvironment.WebRootPath, string.Empty).Replace("\\", "/")}")
                                   ?.ToList()?.FirstOrDefault() ?? string.Empty,
                                ScanTime = videoScanNodeInfoModel?.ScanTime ?? DateTime.Now,
                                NvrCameraBindingItem = videoScanNodeInfoModel?.VideoNvrCameraBindingInfos?.Select(s => new MessageNvrCameraBindingItemInfo {
                                    BarcodeScannerSerialNumber = s?.BarcodeScannerSerialNumber ?? string.Empty,
                                    Channel = s?.Channel ?? 0,
                                    IpAddress = s?.IpAddress ?? string.Empty,
                                    Password = s?.Password ?? string.Empty,
                                    Port = s?.Port ?? 0,
                                    Username = s?.Username ?? string.Empty
                                })?.ToList() ?? new List<MessageNvrCameraBindingItemInfo>()
                            });
                        }
                    }
                    return key ? JsonResultVo.Success("保存成功") : JsonResultVo.Fail("保存失败");
                }
                else {
                    return JsonResultVo.Fail("提交信息格式错误!");
                }
            }
            catch (Exception e) {
                return JsonResultVo.Fail(e.Message);
            }
        }

        [Produces("application/json")]
        [HttpPost("BarcodeInfos")]
        public async Task<JsonResult> BarcodeInfos([FromBody] BarcodeDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _videoBarCodeAppService.GetBarcodeInfos(param.BarCode,
                param.NodeStartDateTime,
                param.NodeEndDateTime,
                param.NodeName,
                param.CameraSerialNumber,
                param.CameraName,
                param.PageIndex,
                param.PageSize, cancellationToken);
            if (key && value is BarcodesDto dto) {
                dto.BarCodes.ForEach(b =>
                    b.ScanNodeInfos?.ForEach(s =>
                        s.BarcodeImageInfos?.ForEach(f => {
                            var request = _httpContextAccessor.HttpContext?.Request;
                            f.Path = $"{request?.Scheme}://{request?.Host}/scr{f.Path.Replace(_hostEnvironment.WebRootPath, string.Empty).Replace("\\", "/")}";
                        })
                    )
                );
                return JsonResultVo.Success("查询成功", dto.Total, dto.BarCodes);
            }
            else {
                return JsonResultVo.Fail(value?.ToString() ?? string.Empty);
            }
        }

        [HttpGet("GroupedNodeNames")]
        public async Task<JsonResult> GroupedNodeNames() {
            var (key, value) = await _videoBarCodeAppService.GroupedNodeNames();
            if (key && value is List<string> nodeNames) {
                return JsonResultVo.Success("查询成功", nodeNames.Count, nodeNames);
            }
            else {
                return JsonResultVo.Fail(value?.ToString() ?? string.Empty);
            }
        }

        [HttpGet("BarcodeTotalForDate")]
        public async Task<JsonResult> BarcodeTotalForDate(DateTime date) {
            var (key, value) = await _videoBarCodeAppService.BarcodeTotalForDate(date);
            if (key && value is int total) {
                return JsonResultVo.Success("查询成功", total);
            }
            else {
                return JsonResultVo.Fail(value?.ToString() ?? string.Empty);
            }
        }

        [HttpGet("BarcodeTotalForDateBetween")]
        public async Task<JsonResult> BarcodeTotalForDateBetween(DateTime startDate, DateTime endDate) {
            var (key, value) = await _videoBarCodeAppService.BarcodeTotalForDateBetween(startDate, endDate);
            if (key && value is int total) {
                return JsonResultVo.Success("查询成功", total);
            }
            else {
                return JsonResultVo.Fail(value?.ToString() ?? string.Empty);
            }
        }
    }
}