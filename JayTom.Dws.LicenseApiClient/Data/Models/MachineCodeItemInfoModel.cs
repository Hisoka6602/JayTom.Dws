namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class MachineCodeItemInfoModel : BaseItemInfoModel {

        /// <summary>
        /// 机器码
        /// </summary>
        public string MachineCode { get; set; } = string.Empty;

        /// <summary>
        /// 首次激活时间
        /// </summary>
        public DateTime FirstActivatedDate { get; set; }

        /// <summary>
        /// 最后效验时间
        /// </summary>
        public DateTime LastVerifiedDate { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }
}