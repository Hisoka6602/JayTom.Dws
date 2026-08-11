using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Scale.StaticScale;

namespace JayTom.Dws.Plugin.Scale.ScaleValueParameters {
    public class DefaultStaticScaleValueParameters : BaseScaleValueParameters {

        /// <summary>
        /// 每条数据间隔时间(采样频率)
        /// </summary>
        public TimeSpan DataInterval { get; set; } = TimeSpan.FromMilliseconds(20);

        /// <summary>
        /// 是否反转
        /// </summary>
        public bool IsReversed { get; set; }

        /// <summary>
        /// 获取方式
        /// </summary>
        public WeightAccessMode AccessMode { get; set; } = WeightAccessMode.Readonly;

        /// <summary>
        /// 稳定个数
        /// </summary>
        public int BalanceCount { get; set; } = 10;

        /// <summary>
        /// 稳定精度(误差范围)
        /// </summary>
        public decimal BalanceQty { get; set; } = 0.002m;

        /// <summary>
        /// 标识符
        /// </summary>
        public string Identifier { get; set; } = "=";

        /// <summary>
        /// 字符长度
        /// </summary>
        public int CharacterLength { get; set; } = 8;

        /// <summary>
        /// 标识符位置
        /// </summary>
        public int IdentifierPosition { get; set; } = 0;

        /// <summary>
        /// 整数起始位置
        /// </summary>
        public int IntegerStartPosition { get; set; }

        /// <summary>
        /// 整数结束位置
        /// </summary>
        public int IntegerEndPosition { get; set; }

        /// <summary>
        /// 小数起始位置
        /// </summary>
        public int DecimalStartPosition { get; set; }

        /// <summary>
        /// 小数结束位置
        /// </summary>
        public int DecimalEndPosition { get; set; }

        /// <summary>
        /// 发送内容
        /// </summary>
        public string SendingContent { get; set; } = string.Empty;

        /// <summary>
        /// 发送格式
        /// </summary>
        public ScaleWeightFormat SendingFormat { get; set; }
    }
}
