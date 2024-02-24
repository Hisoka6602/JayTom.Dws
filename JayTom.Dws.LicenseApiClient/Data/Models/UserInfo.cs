namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class UserInfo {

        /// <summary>
        /// pid
        /// </summary>
        public long Pid { get; set; }

        /// <summary>
        /// 用户代码
        /// </summary>
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 手机号
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 角色
        /// </summary>
        public int Role { get; set; }

        /// <summary>
        /// 用户状态
        /// </summary>
        public UserStatus Status { get; set; } = UserStatus.Active;

        /// <summary>
        /// 用户图片
        /// </summary>
        public string? UserIcon { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime RegisterTime { get; set; }

        /// <summary>
        /// 详细信息
        /// </summary>
        public virtual UserDetailsInfo UserDetailsInfo { get; set; } = new();

        /// <summary>
        /// 授权码
        /// </summary>
        public List<LicenseCodeItemInfoModel> LicenseCodeInfos { get; set; } = new();

        /// <summary>
        /// 授权码拓展信息
        /// </summary>
        public List<LicenseAppLicenseInfoModel> AppLicenseInfos { get; set; } = new();
    }
}