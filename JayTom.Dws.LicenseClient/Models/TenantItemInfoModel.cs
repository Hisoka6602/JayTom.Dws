namespace JayTom.Dws.LicenseClient.Models {

    public class TenantItemInfoModel : BaseItemInfoModel {

        /// <summary>
        /// 用户代码
        /// </summary>
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 手机号码
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 状态
        /// </summary>
        public UserStatus Status { get; set; } = UserStatus.Active;

        /// <summary>
        /// 用户图标
        /// </summary>
        public string? UserIcon { get; set; }

        /// <summary>
        /// 授权码数量
        /// </summary>
        public int LicenseCodeCount { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime RegisterTime { get; set; }
    }

    public enum UserStatus {

        /// <summary>
        /// 激活
        /// </summary>
        Active = 0,

        /// <summary>
        /// 冻结
        /// </summary>
        Frozen = 1,

        /// <summary>
        /// 失效
        /// </summary>
        Invalid = 2
    }
}