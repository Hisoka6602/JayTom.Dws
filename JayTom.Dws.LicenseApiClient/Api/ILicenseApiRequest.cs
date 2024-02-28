using JayTom.Dws.LicenseApiClient.Data.Models;

namespace JayTom.Dws.LicenseApiClient.Api {

    public interface ILicenseApiRequest {

        /// <summary>
        /// 是否已登录
        /// </summary>
        /// <returns></returns>
        Task<bool> IsLoggedIn();

        /// <summary>
        /// 设置url
        /// </summary>
        /// <param name="url"></param>
        void SetBaseUrl(string url);

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="companyName"></param>
        /// <param name="token"></param>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> Register(string userCode, string userName, string passWord, string phone, string companyName = "", CancellationToken token = default);

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="passWord"></param>
        /// <param name="token"></param>
        /// <param name="loginCode"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> Login(string loginCode, string passWord, CancellationToken token = default);

        /// <summary>
        /// 修改资料
        /// </summary>
        /// <param name="businessLicenseFilePath"></param>
        /// <param name="token"></param>
        /// <param name="userName"></param>
        /// <param name="phone"></param>
        /// <param name="companyName"></param>
        /// <param name="companyAddress"></param>
        /// <param name="contactEmail"></param>
        /// <param name="description"></param>
        /// <param name="contractFilePath"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> UpdateProfile(string? userName, string? phone,
            string companyName,
            string companyAddress,
            string contactEmail,
            string description,
            string contractFilePath,
            string businessLicenseFilePath,
            CancellationToken token = default);

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="newPassWord"></param>
        /// <param name="token"></param>
        /// <param name="oldPassWord"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> ChangePassword(string oldPassWord,
            string newPassWord,
            CancellationToken token = default);

        /// <summary>
        /// 个人信息
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> Info(CancellationToken token = default);

        /// <summary>
        /// 冻结用户
        /// </summary>
        /// <param name="isFreeze"></param>
        /// <param name="token"></param>
        /// <param name="userCode"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> FreezeUser(string userCode,
            bool isFreeze,
            CancellationToken token = default);

        /// <summary>
        /// 修改用户头像
        /// </summary>
        /// <param name="iconImage"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> ChangeUserIcon(System.Drawing.Image iconImage, CancellationToken token = default);

        /// <summary>
        /// 租户信息
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> TenantInfos(CancellationToken token = default);

        //---------------------------------
        /// <summary>
        /// 创建应用
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> CreateApplication(string applicationName,
            string description,
            List<FeatureItemModel>? featureInfos,
            CancellationToken token = default);

        /// <summary>
        /// 创建应用模板
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="token"></param>
        /// <param name="licenseApplicationInfoId"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> CreateApplicationTemplate(
            long licenseApplicationInfoId,
            string templateName,
            CancellationToken token = default);

        /// <summary>
        /// 设置模板权限
        /// </summary>
        /// <param name="featureInfos"></param>
        /// <param name="token"></param>
        /// <param name="templateId"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> SetTemplatePermissions(long templateId,
            List<FeatureItemModel>? featureInfos,
            CancellationToken token = default);

        /// <summary>
        /// 获取应用列表
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> ApplicationData(CancellationToken token = default);

        /// <summary>
        /// 获取模板列表
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> TemplateData(CancellationToken token = default);

        /// <summary>
        /// 删除应用
        /// </summary>
        /// <param name="deleteApplicationId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> DeleteApplication(long deleteApplicationId, CancellationToken token = default);

        /// <summary>
        /// 删除模板
        /// </summary>
        /// <param name="deleteTemplateId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> DeleteTemplate(long deleteTemplateId, CancellationToken token = default);

        /// <summary>
        /// 创建授权码
        /// </summary>
        /// <param name="clientName"></param>
        /// <param name="userCode"></param>
        /// <param name="token"></param>
        /// <param name="templateInfoId"></param>
        /// <param name="maxClientCount"></param>
        /// <param name="expirationDate"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> CreateLicenseCode(long templateInfoId,
            int maxClientCount,
            DateTime expirationDate,
            string clientName,
            string userCode = "",
            CancellationToken token = default);

        /// <summary>
        /// 更新授权码信息
        /// </summary>
        /// <param name="templateInfoId"></param>
        /// <param name="userCode"></param>
        /// <param name="licenseCode"></param>
        /// <param name="maxClientCount"></param>
        /// <param name="expirationDate"></param>
        /// <param name="clientName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> UpdateLicenseCode(long templateInfoId,
           string licenseCode,
           int maxClientCount,
           DateTime expirationDate,
           string clientName,
           CancellationToken token = default);

        /// <summary>
        /// 授权码数据列表
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> LicenseCodeData(CancellationToken token = default);

        /// <summary>
        /// 延期授权码
        /// </summary>
        /// <param name="expirationDate"></param>
        /// <param name="token"></param>
        /// <param name="licenseCode"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> ExtendLicenseCodeValidity(string licenseCode,
            DateTime expirationDate,
            CancellationToken token = default);

        /// <summary>
        /// 冻结授权码
        /// </summary>
        /// <param name="isFreeze"></param>
        /// <param name="token"></param>
        /// <param name="licenseCode"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> FreezeLicenseCode(string licenseCode,
            bool isFreeze,
            CancellationToken token = default);

        /// <summary>
        /// 下载授权文件
        /// </summary>
        /// <param name="machineCode"></param>
        /// <param name="remarks"></param>
        /// <param name="token"></param>
        /// <param name="licenseCode"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> DownloadLicenseFile(string licenseCode,
            string machineCode,
            string remarks,
            CancellationToken token = default);

        /// <summary>
        /// 解绑机器码
        /// </summary>
        /// <param name="licenseCode"></param>
        /// <param name="machineCode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> UnbindMachineCode(string licenseCode, string machineCode, CancellationToken token);

        /// <summary>
        /// 修改租户授权上限
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="licensePermissionTemplateInfoId"></param>
        /// <param name="maxLicenseCodeCount"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> UpdateTenantLicenseMaxCount(string userCode, long licensePermissionTemplateInfoId, int maxLicenseCodeCount,
            CancellationToken cancellationToken = default);
    }

    public class ApiResult {
        public bool Result { get; set; }
        public object? Data { get; set; }
        public string Msg { get; set; } = string.Empty;
        public int Total { get; set; }
    }
}