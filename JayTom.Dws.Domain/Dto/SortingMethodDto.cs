using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto {

    public class SortingMethodDto {

        /// <summary>
        /// 选中的分拣模式
        /// </summary>
        public SortMode SortMode { get; set; }
    }

    public enum SortMode {

        /// <summary>
        /// 无
        /// </summary>
        None,

        /// <summary>
        /// 条码分拣
        /// </summary>
        BarcodeSorting,

        /// <summary>
        /// 重量分拣
        /// </summary>
        WeightSorting,

        /// <summary>
        /// 体积分拣
        /// </summary>
        VolumeSorting,

        /// <summary>
        /// 物流分拣
        /// </summary>
        LogisticsSorting,

        /// <summary>
        /// Ocr分拣
        /// </summary>
        OcrSorting,

        /// <summary>
        /// Api分拣
        /// </summary>
        ApiResponseSorting,

        /// <summary>
        /// 组合工作流分拣
        /// </summary>
        CombinedWorkflowSorting
    }
}