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
using JayTom.Dws.Domain.Dto.VideoApi;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using JayTom.Dws.Application.Service.VideoApi;

namespace JayTom.Dws.VideoApi.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class BarCodeController : ControllerBase {
        private readonly ILogger<BarCodeController> _logger;
        private readonly IVideoBarCodeAppService _videoBarCodeAppService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static string? _saveImagePath;

        public BarCodeController(ILogger<BarCodeController> logger,
            IVideoBarCodeAppService videoBarCodeAppService,
            IWebHostEnvironment hostEnvironment, IHttpContextAccessor httpContextAccessor) {
            _logger = logger;
            _videoBarCodeAppService = videoBarCodeAppService;
            _hostEnvironment = hostEnvironment;
            _httpContextAccessor = httpContextAccessor;
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
                    var (key, value) = await _videoBarCodeAppService.AddOrUpdateBarcodeInfo(new BarcodeImageDto() {
                        CameraSerialNumber = Path.GetFileNameWithoutExtension(barcodeImage.FileName)?.Split("_")?[0] ?? string.Empty,
                        //CameraName = barcodeImage.FileName.Split("_")?[1] ?? string.Empty,
                        Image = FileUtils.ConvertIFormFileToBitmap(barcodeImage),
                        Name = barcodeImage.FileName
                    }, panoramaImages.Select(s =>
                        new BarcodeImageDto {
                            CameraSerialNumber = Path.GetFileNameWithoutExtension(s.FileName)?.Split("_")?[0] ?? string.Empty,
                            //CameraName = s.FileName.Split("_")?[1] ?? string.Empty,
                            Image = FileUtils.ConvertIFormFileToBitmap(s),
                            Name = s.FileName
                        })?.ToList() ?? new List<BarcodeImageDto>(),
                        scanNodeDto, _saveImagePath);
                    return key ? JsonResultVo.Success(value) : JsonResultVo.Fail(value);
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
    }
}