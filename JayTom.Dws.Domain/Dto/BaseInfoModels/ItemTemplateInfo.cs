using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.BaseInfoModels {
    public class ItemTemplateInfo {

        /// <summary>
        /// 类型(0=编辑框、1=按钮、2=分隔符)
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 实际内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 应用类型
        /// </summary>
        public ItemApplicationType ApplicationType { get; set; }
    }

    public enum ItemApplicationType {

        /// <summary>
        /// 水印
        /// </summary>
        Watermark = 0,

        /// <summary>
        /// 子路径
        /// </summary>
        SubDirectory = 1,

        /// <summary>
        /// 图片命名
        /// </summary>
        ImageNaming = 2,

        /// <summary>
        /// 结果数据
        /// </summary>
        ResultData = 3,

        /// <summary>
        /// 体积输入
        /// </summary>
        VolumeInput = 4,

        /// <summary>
        /// Api数据
        /// </summary>
        ApiData = 5,

        /// <summary>
        /// 逻辑公式
        /// </summary>
        Formula = 6,
        /// <summary>
        /// 数据输入
        /// </summary>
        DataInput,
    }
}