namespace JayTom.Dws.Plugin.WeighingScale {

    /// <summary>
    /// 称重器
    /// </summary>
    public interface IWeighingScale : IDevice {

        /// <summary>
        /// 稳定重量
        /// </summary>
        event EventHandler<decimal> StabledWeight;

        /// <summary>
        /// 实时重量
        /// </summary>
        event EventHandler<decimal> CurrentWeight;

        /// <summary>
        /// 接收内容
        /// </summary>
        event EventHandler<string> Received;

        /// <summary>
        /// 设置称重参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        bool SetWeightCalculationParameters<T>(T param);
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
}