using JayTom.Dws.CloudApi.Vo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.CloudApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class PackageController : ControllerBase {

        /// <summary>
        /// 上传数据
        /// </summary>
        /// <param name="barcodeImage"></param>
        /// <param name="panoramaImages"></param>
        /// <param name="packageInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("UploadPackageInfo")]
        public async Task<JsonResult> UploadPackageInfo([FromForm][NotNull] IFormFile barcodeImage,
            [FromForm] List<IFormFile> panoramaImages,
            [FromForm][NotNull] string packageInfo,
            CancellationToken cancellationToken) {
            //处理图片
            //添加到数据库
            //PackageDto

            return JsonResultVo.Success("测试回调");
        }

        /// <summary>
        /// 数据-查询详细列表(条件、分页)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("Packages")]
        public async Task<JsonResult> GetPackages(/*[FromQuery] PackageFilter filter,
            [FromQuery] Pagination pagination,*/
            CancellationToken cancellationToken) {
            // 查询数据库，返回符合条件的 PackageDto 列表

            //PackageDto

            return JsonResultVo.Success("测试回调");
        }

        /// <summary>
        /// 数据-查询详细列表(条件、分页)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("Package")]
        public async Task<JsonResult> GetPackage(string id, CancellationToken cancellationToken) {
            // 查询数据库，返回指定id的 PackageDto
            //PackageDto
            return JsonResultVo.Success("测试回调");
        }

        /// <summary>
        /// 统计-查询统计数据
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("Statistics")]
        public async Task<JsonResult> GetStatistics(/*[FromQuery] StatisticsFilter filter,*/ CancellationToken cancellationToken) {
            // 查询数据库，返回符合条件的统计数据
            //PackageStatisticsDto
            return JsonResultVo.Success("测试回调");
        }

        /// <summary>
        /// 统计-查询(时间、入参分类:无物理格口、网络超时、条码无识别)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("ErrorStatistics")]
        public async Task<JsonResult> GetErrorStatistics(/*[FromQuery] ErrorStatisticsFilter filter, */CancellationToken cancellationToken) {
            // 查询数据库，返回符合条件的错误统计数据
            // List< StatisticsDto >
            return JsonResultVo.Success("测试回调");
        }
    }
}