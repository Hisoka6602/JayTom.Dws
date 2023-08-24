namespace JayTom.Dws.Plugin.Scale.ScaleValueParameters {

    public class BaseScaleValueParameters {

        /// <summary>
        /// 最小可用重量
        /// </summary>
        public float MinWeight { get; set; } = (float)0.002;

        /// <summary>
        /// 最大可用重量
        /// </summary>
        public float MaxWeight { get; set; } = 30;
    }
}