using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.CloudApi.Do {

    public class BasePageDo {

        /// <summary>
        /// 页码
        /// </summary>
        [MinLength(0, ErrorMessage = "最小值不能小于0")]
        public int PageIndex { get; set; }

        /// <summary>
        /// 页尺寸
        /// </summary>
        [MinLength(1, ErrorMessage = "最小值不能小于0"),
        MaxLength(1000, ErrorMessage = "最大不能大于1000")]
        public int PageCount { get; set; }
    }
}