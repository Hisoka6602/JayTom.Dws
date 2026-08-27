using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Abstractions.Results;
using Newtonsoft.Json;

namespace JayTom.Dws.Integrations.License {

    public interface IClientLicenseApi {

        /// <summary>
        /// 远程授权
        /// </summary>
        /// <param name="machineCode"></param>
        /// <param name="remarks"></param>
        /// <param name="token"></param>
        /// <param name="licenseCode"></param>
        /// <returns></returns>
        Task<OperationResult<ApiResult>> CreateAuthorization(string licenseCode, string machineCode, string remarks, CancellationToken token = default);

        /// <summary>
        /// 激活授权
        /// </summary>
        /// <param name="machineCode"></param>
        /// <param name="remarks"></param>
        /// <param name="token"></param>
        /// <param name="licenseCode"></param>
        /// <returns></returns>
        Task<OperationResult<ApiResult>> ActivateAuthorization(string licenseCode, string machineCode, string remarks, CancellationToken token = default);

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileUrl"></param>
        /// <param name="savePath"></param>
        /// <param name="token">取消令牌。</param>
        /// <returns></returns>
        Task<OperationResult<string>> DownloadFileAsync(string fileUrl, string savePath, CancellationToken token = default);
    }

    public class ApiResult {
        [JsonProperty("result")]
        public bool IsSuccess { get; set; }
        public string? Data { get; set; }
        [JsonProperty("msg")]
        public string Message { get; set; } = string.Empty;
        public long Total { get; set; }
    }
}
