namespace JayTom.Dws.Plugin.Scale.ScaleValueParameters {

    public class BaseScaleValueParameters {

        /// <summary>
        /// 最小可用重量
        /// </summary>
        public decimal MinWeight { get; set; } = 0.002m;

        /// <summary>
        /// 最大可用重量
        /// </summary>
        public decimal MaxWeight { get; set; } = 30;
    }
}
