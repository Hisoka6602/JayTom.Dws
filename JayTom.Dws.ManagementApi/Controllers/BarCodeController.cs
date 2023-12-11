using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace JayTom.Dws.ManagementApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class BarCodeController : ControllerBase {
        private readonly ILogger<BarCodeController> _logger;

        public BarCodeController(ILogger<BarCodeController> logger) {
            _logger = logger;
        }

        [HttpPost("UploadBarcodeData")]
        public async Task<JsonResult> UploadBarcodeData(IFormFile barcodeImage, List<IFormFile> panoramaImages, [FromBody] string jsonData, CancellationToken cancellationToken) {
        }
    }
}