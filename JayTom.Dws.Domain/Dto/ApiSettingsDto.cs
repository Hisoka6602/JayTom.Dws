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

        /// <summary>
        /// 络道科技Api
        /// </summary>
        RoutDataApi = 9,

        /// <summary>
        /// Geek+
        /// </summary>
        GeekPlusApi = 10,

        /// <summary>
        /// 菜鸟Api
        /// </summary>
        CaiNiaoApi = 11,

        /// <summary>
        /// 菜鸟Api
        /// </summary>
        EshippingitApi = 12,

        /// <summary>
        /// 邮政Api
        /// </summary>
        PostApi = 13,

        /// <summary>
        /// 邮政揽投Api
        /// </summary>
        PostInApi = 14,

        /// <summary>
        /// 长沙拙燕Api
        /// </summary>
        ZhuoYanScm = 15,

        /// <summary>
        /// 通天晓Api
        /// </summary>
        TtxApi = 16,

        /// <summary>
        /// 旺店通Wms+通天晓
        /// </summary>
        WdtWmsApiAndTtxApi = 17
    }
}