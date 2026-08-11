using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;

namespace JayTom.Dws.Domain.Dto {

    public class WeightSettingsDto {

        /// <summary>
        /// 称重模式
        /// </summary>
        public WeightMode Mode { get; set; } = WeightMode.None;

        /// <summary>
        /// 连接模式
        /// </summary>

        public ScaleCommunicationMode ScaleCommunicationMode { get; set; } = ScaleCommunicationMode.SerialPort;

        /// <summary>
        /// 串口连接参数
        /// </summary>
        public SerialPortSettingsInfo Connection { get; set; } = new();

        /// <summary>
        /// Tcp连接参数
        /// </summary>
        public TcpSettingsInfo TcpSettingsInfo { get; set; } = new();

        /// <summary>
        /// 公共参数
        /// </summary>
        public CommonWeightParams CommonWeight { get; set; } = new();

        /// <summary>
        /// 静态称参数
        /// </summary>
        public StaticWeightParams StaticWeight { get; set; } = new();

        /// <summary>
        /// 动态称参数
        /// </summary>
        public DynamicWeightParams DynamicWeight { get; set; } = new();

        /// <summary>
        /// 重量附加属性
        /// </summary>
        public AdditionalWeightProperties AdditionalWeight { get; set; } = new();
    }

    public enum WeightMode {

        /// <summary>
        /// 静态称重
        /// </summary>
        Static,

        /// <summary>
        /// 动态称重
        /// </summary>
        Dynamic,

        /// <summary>
        /// 不使用称重
        /// </summary>
        None
    }

    public class CommonWeightParams {

        /// <summary>
        /// 最小重量
        /// </summary>
        public decimal MinWeight { get; set; }

        /// <summary>
        /// 最大重量
        /// </summary>
        public decimal MaxWeight { get; set; }
    }

    /// <summary>
    /// 静态称参数
    /// </summary>
    public class StaticWeightParams {

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
        public DataFormatType SendingFormat { get; set; } = DataFormatType.Ascii;
    }

    public enum WeightAccessMode {

        /// <summary>
        /// 只读式
        /// </summary>
        Readonly,

        /// <summary>
        /// 问答式
        /// </summary>
        QuestionAnswer
    }

    /// <summary>
    /// 动态称参数
    /// </summary>
    public class DynamicWeightParams {

        /// <summary>
        /// 保留位数
        /// </summary>
        public int DecimalPrecision { get; set; } = 3;
    }

    public class AdditionalWeightProperties {

        /// <summary>
        /// 是否使用实际重量转换率
        /// </summary>
        public bool IsUseActualWeightConversionRate { get; set; }

        /// <summary>
        /// 重量转换率
        /// </summary>
        public decimal WeightConversionRate { get; set; }

        /// <summary>
        /// 是否使用追加重量
        /// </summary>
        public bool IsUseAppendedWeight { get; set; }

        /// <summary>
        /// 追加重量的值
        /// </summary>
        public decimal AppendedWeightValue { get; set; }

        /// <summary>
        /// 是否使用固定重量
        /// </summary>
        public bool IsUseFixedWeight { get; set; }

        /// <summary>
        /// 固定重量的值
        /// </summary>
        public decimal FixedWeightValue { get; set; }

        /// <summary>
        /// 是否使用融合重量超时
        /// </summary>
        public bool IsUseMergedWeightTimeout { get; set; }

        /// <summary>
        /// 融合重量超时时间
        /// </summary>
        public int MergedWeightTimeout { get; set; } = 300;
    }
}
