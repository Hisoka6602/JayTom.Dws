using System;
using System.Linq;
using System.Text;
using System.Reflection;
using Newtonsoft.Json.Linq;
using JayTom.Dws.Domain.Jwt;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Collections.Generic;
using JayTom.Dws.Domain.Repository.License;
using JayTom.Dws.Domain.Service.LicenseApi;

namespace JayTom.Dws.Application.Service.LicenseApi {

    public class LicenseUserAppService : ILicenseUserAppService {
        private readonly ILicenseUserService _licenseUserService;
        private readonly IAuthenticateService _authenticateService;

        public LicenseUserAppService(ILicenseUserService licenseUserService,
            IAuthenticateService authenticateService) {
            _licenseUserService = licenseUserService;
            _authenticateService = authenticateService;
        }

        public Task<KeyValuePair<bool, object>> Register(string userCode, string userName,
            string password, string phone, string ipAddress, string companyName, CancellationToken token) {
            return _licenseUserService.Register(userCode, userName, password, phone, ipAddress, companyName, token);
        }

        public async Task<KeyValuePair<bool, object>> Login(string loginCode, string password, CancellationToken token) {
            var (key, value) = await _licenseUserService.Login(loginCode, password, token);
            if (key && value is LicenseUserInfo info) {
                var isAuthenticated = _authenticateService.IsAuthenticated(new LoginRequestDto() {
                    PassWord = info.PassWord,
                    UserCode = info.UserCode
                }, out var logInToken);
                return !isAuthenticated ? new KeyValuePair<bool, object>(false, "生成Token失败!") : new KeyValuePair<bool, object>(true, logInToken);
            }
            return new KeyValuePair<bool, object>(false, value.ToString() ?? string.Empty);
        }

        public Task<KeyValuePair<bool, object>> UpdateProfile(string userCode, string? userName, string? phone, string companyName, string companyAddress,
            string contactEmail, string description, string contractFilePath, string businessLicenseFilePath,
            CancellationToken token) {
            return _licenseUserService.UpdateProfile(userCode, userName, phone, companyName, companyAddress, contactEmail,
                 description, contractFilePath, businessLicenseFilePath, token);
        }

        public Task<KeyValuePair<bool, object>> ChangePassword(string userCode, string oldPassWord, string newPassWord, CancellationToken token) {
            return _licenseUserService.ChangePassword(userCode, oldPassWord, newPassWord, token);
        }

        public Task<KeyValuePair<bool, object>> Info(string userCode, CancellationToken token) {
            return _licenseUserService.Info(userCode, token);
        }

        public async Task<KeyValuePair<bool, object>> FreezeUser(string userCode, bool isFreeze, CancellationToken token) {
            var (key, value) = await _licenseUserService.Info(userCode, token);
            if (key && value is LicenseUserInfo info) {
                if (info.Status == UserStatus.Active != isFreeze) {
                    return new KeyValuePair<bool, object>(false, $"用户当前已经是:{(isFreeze ? "激活" : "冻结")}状态,无需重复操作");
                }
                return await _licenseUserService.FreezeUser(userCode, isFreeze, token);
            }
            return new KeyValuePair<bool, object>(false, $"用户不存在");
        }

        public Task<KeyValuePair<bool, object>> SetUserIcon(string userCode, string iconUrlPath, CancellationToken token) {
            return _licenseUserService.SetUserIcon(userCode, iconUrlPath, token);
        }

        public Task<KeyValuePair<bool, object>> TenantInfos(CancellationToken token) {
            return _licenseUserService.TenantInfos(token);
        }

        public Task<KeyValuePair<bool, object>> UpdateTenantLicenseMaxCount(string userCode, long licensePermissionTemplateInfoId, int maxLicenseCodeCount,
            CancellationToken cancellationToken) {
            return _licenseUserService.UpdateTenantLicenseMaxCount(userCode, licensePermissionTemplateInfoId, maxLicenseCodeCount, cancellationToken);
        }
    }
}