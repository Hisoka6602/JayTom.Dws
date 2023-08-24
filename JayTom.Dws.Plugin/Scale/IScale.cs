using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Scale.ScaleValueParameters;

namespace JayTom.Dws.Plugin.Scale {
    public interface IScale : IDisposable {

        /// <summary>
        /// 重量附加属性
        /// </summary>
        public WeightAdditionalProperties WeightAdditionalProperties { get; set; }

        /// <summary>
        /// 重量内容格式
        /// </summary>
        public ScaleWeightFormat WeightFormat { get; set; }

        /// <summary>
        /// 稳定重量
        /// </summary>
        event EventHandler<float> StabledWeight;

        /// <summary>
        /// 接收内容
        /// </summary>
        event EventHandler<string> Received;

        /// <summary>
        /// 连接事件
        /// </summary>
        event EventHandler<IScale> Connected;

        /// <summary>
        /// 断开事件
        /// </summary>
        event EventHandler<IScale> Disconnected;

        /// <summary>
        /// 异常事件
        /// </summary>
        event EventHandler<Exception> Excepted;

        /// <summary>
        /// 设置称重参数
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        bool SetWeightCalculationParameters(BaseScaleValueParameters param);

        /// <summary>
        /// 连接状态
        /// </summary>
        ScaleStatus Status { get; }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        bool Connect(BaseScaleConnectParam connectParam);
    }

    public enum ScaleStatus {

        /// <summary>
        /// 未连接
        /// </summary>
        NotConnected,

        /// <summary>
        /// 已断开
        /// </summary>
        Disconnected,

        /// <summary>
        /// 运行中
        /// </summary>
        Running
    }

    public enum ScaleWeightFormat {

        /// <summary>
        /// 十六进制
        /// </summary>
        Hex,

        /// <summary>
        /// ASCII码
        /// </summary>
        Ascii
    }

    public class WeightAdditionalProperties {

        /// <summary>
        /// 是否使用实际重量转换率
        /// </summary>
        public bool IsUseActualWeightConversionRate { get; set; }

        /// <summary>
        /// 重量转换率
        /// </summary>
        public double WeightConversionRate { get; set; }

        /// <summary>
        /// 是否使用追加重量
        /// </summary>
        public bool IsUseAppendedWeight { get; set; }

        /// <summary>
        /// 追加重量的值
        /// </summary>
        public double AppendedWeightValue { get; set; }

        /// <summary>
        /// 是否使用固定重量
        /// </summary>
        public bool IsUseFixedWeight { get; set; }

        /// <summary>
        /// 固定重量的值
        /// </summary>
        public double FixedWeightValue { get; set; }

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