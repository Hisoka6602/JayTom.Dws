using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Interface.License {

    public interface IClientLicenseApi {

        /// <summary>
        /// 远程授权
        /// </summary>
        /// <param name="machineCode"></param>
        /// <param name="remarks"></param>
        /// <param name="token"></param>
        /// <param name="licenseCode"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> CreateAuthorization(string licenseCode, string machineCode, string remarks, CancellationToken token = default);

        /// <summary>
        /// 激活授权
        /// </summary>
        /// <param name="machineCode"></param>
        /// <param name="remarks"></param>
        /// <param name="token"></param>
        /// <param name="licenseCode"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> ActivateAuthorization(string licenseCode, string machineCode, string remarks, CancellationToken token = default);

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileUrl"></param>
        /// <param name="savePath"></param>
        /// <returns></returns>
        Task<bool> DownloadFileAsync(string fileUrl, string savePath);
    }

    public class ApiResult {
        public bool Result { get; set; }
        public object? Data { get; set; }
        public string Msg { get; set; } = string.Empty;
        public int Total { get; set; }
    }
}