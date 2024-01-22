using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.CloudApi.Do {

    public class BasePageDo {
        /// <summary>
        /// 页码
        /// </summary>

        [Range(0, int.MaxValue, ErrorMessage = "值不能小于0,或大于最大整数")]
        public int PageIndex { get; set; }

        /// <summary>
        /// 页尺寸
        /// </summary>
        [Range(1, 1000, ErrorMessage = "值不能小于1,或大于1000"), DefaultValue(1)]
        public int PageSize { get; set; }
    }
}