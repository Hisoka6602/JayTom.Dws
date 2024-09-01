using JayTom.Dws.VideoApi.Do;
using JayTom.Dws.VideoApi.Vo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JayTom.Dws.Domain.Dto.CloudApiDto;
using JayTom.Dws.Application.Service.VideoApi;

namespace JayTom.Dws.VideoApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase {
        private readonly IVideoConfigAppService _videoConfigAppService;

        public ConfigController(ILogger<BarCodeController> logger,
            IVideoConfigAppService videoConfigAppService) {
            _videoConfigAppService = videoConfigAppService;
        }

        [Produces("application/json")]
        [HttpPost("SaveConfig")]
        public async Task<JsonResult> SaveConfig([FromBody] ConfigDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _videoConfigAppService.SetVideoConfig(param.SettingsName,
                param.ConfigJson, cancellationToken);
            return key ? JsonResultVo.Success(value?.ToString() ?? string.Empty) : JsonResultVo.Fail(value?.ToString() ?? string.Empty);
        }

        [Produces("application/json")]
        [HttpGet("GetConfig")]
        public async Task<JsonResult> GetConfig([FromQuery] string settingsName,
            CancellationToken cancellationToken) {
            var (b, o) = await _videoConfigAppService.GetVideoConfig(settingsName, cancellationToken);

            return JsonResultVo.Success(b ? "查询成功" : "查询失败", o);
        }
    }
}