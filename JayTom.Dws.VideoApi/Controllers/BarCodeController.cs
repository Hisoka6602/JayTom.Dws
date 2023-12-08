using JayTom.Dws.VideoApi.Vo;
using Microsoft.AspNetCore.Mvc;

namespace JayTom.Dws.VideoApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class BarCodeController : ControllerBase {
        private readonly ILogger<BarCodeController> _logger;

        public BarCodeController(ILogger<BarCodeController> logger) {
            _logger = logger;
        }

        [HttpPost("UploadBarcodeData")]
        public async Task<JsonResult> UploadBarcodeData([FromForm] IFormFile barcodeImage, [FromForm] List<IFormFile> panoramicImages, [FromForm] string jsonData, CancellationToken cancellationToken) {
            //解析Json
            //判断barcodeImage和panoramicImages是否存在数据

            return JsonResultVo.Success("aa");
        }
    }
}