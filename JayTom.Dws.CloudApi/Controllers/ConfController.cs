using JayTom.Dws.CloudApi.Do;
using JayTom.Dws.CloudApi.Vo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JayTom.Dws.CloudApi.Filter;
using JayTom.Dws.CloudApi.Do.Conf;
using JayTom.Dws.Domain.Dto.CloudApiDto;
using JayTom.Dws.Application.Service.CloudApi;

namespace JayTom.Dws.CloudApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class ConfController : ControllerBase {
        private readonly ICloudAppService _cloudAppService;

        public ConfController(ICloudAppService cloudAppService) {
            _cloudAppService = cloudAppService;
        }

        /// <summary>
        /// 添加异常分类
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("AddExceptionType")]
        [IpAddressFilter("127.0.0.1")]
        public async Task<JsonResult> AddExceptionType([FromBody] ExceptionInfoDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _cloudAppService.AddExceptionType(param.ExceptionName,
                param.ExceptionColor, cancellationToken);
            return key ? JsonResultVo.Success("添加成功") : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 修改异常分类
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("UpdateExceptionType")]
        [IpAddressFilter("127.0.0.1")]
        public async Task<JsonResult> UpdateExceptionType([FromBody] ExceptionInfoDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _cloudAppService.UpdateExceptionType(param.ExceptionTypeId,
                param.ExceptionName, param.ExceptionColor,
                cancellationToken);
            return key ? JsonResultVo.Success("修改成功") : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 删除异常分类
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("DeleteExceptionType")]
        [IpAddressFilter("127.0.0.1")]
        public async Task<JsonResult> DeleteExceptionType([FromBody] ExceptionInfoDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _cloudAppService.DeleteExceptionType(param.ExceptionTypeId, cancellationToken);
            return key ? JsonResultVo.Success("删除成功") : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 获取异常类型
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpGet("ExceptionTypes")]
        public async Task<JsonResult> ExceptionTypes(
            CancellationToken cancellationToken) {
            var (key, value) = await _cloudAppService.ExceptionTypes(cancellationToken);
            if (key && value is List<ExceptionTypeDto> dto) {
                return JsonResultVo.Success("查询成功",
                    dto.Count, dto);
            }
            else {
                return JsonResultVo.Fail(value?.ToString() ?? string.Empty);
            }
        }

        /// <summary>
        /// 添加异常匹配规则
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("AddExceptionRule")]
        [IpAddressFilter("127.0.0.1")]
        public async Task<JsonResult> AddExceptionRule([FromBody] ExceptionMatchInfoDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _cloudAppService.AddExceptionRule(param.Keywords,
                param.CustomRegex, param.DataSource, param.ExceptionTypeName,
                param.ExceptionTypeId, param.Priority, cancellationToken);
            return key ? JsonResultVo.Success("添加成功") : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 更新异常匹配规则
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("UpdateExceptionRule")]
        [IpAddressFilter("127.0.0.1")]
        public async Task<JsonResult> UpdateExceptionRule([FromBody] ExceptionMatchInfoDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _cloudAppService.AddExceptionRule(param.Keywords,
                param.CustomRegex, param.DataSource, param.ExceptionTypeName,
                param.ExceptionTypeId, param.Priority, cancellationToken);
            return key ? JsonResultVo.Success("添加成功") : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 删除异常匹配规则
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("DeleteExceptionRule")]
        [IpAddressFilter("127.0.0.1")]
        public async Task<JsonResult> DeleteExceptionRule([FromBody] ExceptionMatchInfoDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _cloudAppService.DeleteExceptionRule(param.ExceptionRuleId, cancellationToken);
            return key ? JsonResultVo.Success("删除成功") : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 获取异常匹配规则
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpGet("ExceptionRules")]
        public async Task<JsonResult> ExceptionRules(
            CancellationToken cancellationToken) {
            var (key, value) = await _cloudAppService.ExceptionRule(cancellationToken);
            if (key && value is List<ExceptionRuleDto> dto) {
                return JsonResultVo.Success("查询成功",
                    dto.Count, dto);
            }
            else {
                return JsonResultVo.Fail(value?.ToString() ?? string.Empty);
            }
        }
    }
}