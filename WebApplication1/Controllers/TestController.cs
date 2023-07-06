using System.Drawing;
using System.Net.Mime;
using JayTom.Dws.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase {

        [HttpPost("Image")]
        public Task<Image> Image([FromBody] TestModel param) {
            var image = param.imagebase64.ConvertBase64ToImage();

            return Task.FromResult(image);
        }
    }
}