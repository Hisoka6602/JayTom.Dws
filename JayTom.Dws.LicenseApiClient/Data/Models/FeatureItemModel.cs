namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class FeatureItemModel {
        public int Num { get; set; }

        /// <summary>
        /// 功能名称
        /// </summary>
        public string FeatureName { get; set; } = string.Empty;

        /// <summary>
        /// Guid能为空
        /// </summary>
        public string FeatureGuid { get; set; } = string.Empty;

        /// <summary>
        /// 说明
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}