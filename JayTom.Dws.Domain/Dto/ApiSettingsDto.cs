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

        /// <summary>
        /// 桑能Api(新加坡)
        /// </summary>
        SunnenApi = 2,

        /// <summary>
        /// 旺店通Wms
        /// </summary>
        WdtWmsApi = 3,

        /// <summary>
        /// 旺店通Erp
        /// </summary>
        WdtErpApi = 4,

        /// <summary>
        /// 旺店通Erp旗舰版
        /// </summary>
        WdtErpFlagShipApi = 5,

        /// <summary>
        /// 神州集运后台
        /// </summary>
        SzjyApi = 6,

        /// <summary>
        /// 筋斗云Wms
        /// </summary>
        JdyWms = 7,

        /// <summary>
        /// 极兔快递
        /// </summary>
        JtExpressApi = 8,
    }
}