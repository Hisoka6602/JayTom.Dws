using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto {

    public class ApiSettingsDto {
        public ApiType Type { get; set; } = ApiType.None;
    }

    public enum ApiType {

        /// <summary>
        /// 不上传
        /// </summary>
        None = 0,

        /// <summary>
        /// 基础api
        /// </summary>
        DefaultApi = 1,
    }
}