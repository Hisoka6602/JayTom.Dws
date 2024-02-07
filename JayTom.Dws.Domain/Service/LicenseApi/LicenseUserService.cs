using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Collections.Generic;
using JayTom.Dws.Domain.Repository.License;

namespace JayTom.Dws.Domain.Service.LicenseApi {

    public class LicenseUserService : ILicenseUserService {
        private readonly ILicenseUserRepository _licenseUserRepository;
        private readonly ILicenseUserDetailsRepository _licenseUserDetailsRepository;

        public LicenseUserService(ILicenseUserRepository licenseUserRepository,
            ILicenseUserDetailsRepository licenseUserDetailsRepository) {
            _licenseUserRepository = licenseUserRepository;
            _licenseUserDetailsRepository = licenseUserDetailsRepository;
        }

        public async Task<KeyValuePair<bool, object>> Register(string userCode,
            string userName, string password, string phone, string ipAddress, CancellationToken token) {
            var insert = await _licenseUserRepository.Insert(new LicenseUserInfo() {
                CreateTime = DateTime.Now,
                UserCode = userCode,
                UserName = userName,
                Phone = phone,
                PassWord = password,
                Role = UserRole.Tenant,
                Status = UserStatus.Active,
                ModifyIp = ipAddress
            }, token);
            return new KeyValuePair<bool, object>(insert, insert ? "注册成功" : "注册失败");
        }

        public async Task<KeyValuePair<bool, object>> Login(string loginCode, string password, CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.FirstOrDefault(f =>
                (f.Phone.Equals(loginCode) ||
                 f.UserCode.Equals(loginCode) ||
                 f.UserName.Equals(loginCode)) &&
                f.PassWord.Equals(password), token);
            if (licenseUserInfo is not null) {
                return new KeyValuePair<bool, object>(true, licenseUserInfo);
            }
            return new KeyValuePair<bool, object>(false, "密码错误!");
        }

        public async Task<KeyValuePair<bool, object>> UpdateProfile(string userCode, string? userName, string? phone, string companyName, string companyAddress,
            string contactEmail, string description, string contractFilePath, string businessLicenseFilePath,
            CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.FirstOrDefault(f =>
                f.UserCode.Equals(userCode), token);
            if (licenseUserInfo is not null) {
                if (!string.IsNullOrEmpty(userName) || !string.IsNullOrEmpty(phone)) {
                    licenseUserInfo.UserName = string.IsNullOrEmpty(userName) ? licenseUserInfo.UserName : userName;
                    licenseUserInfo.Phone = string.IsNullOrEmpty(phone) ? licenseUserInfo.Phone : phone;
                    var update = await _licenseUserRepository.Update(licenseUserInfo, token);
                }

                var licenseUserDetailsInfo = await _licenseUserDetailsRepository.
                    FirstOrDefault(f =>
                        f.UserId.Equals(licenseUserInfo.Id), token);
                if (licenseUserDetailsInfo is not null) {
                    licenseUserDetailsInfo.CompanyAddress = companyAddress;
                    licenseUserDetailsInfo.CompanyName = companyName;
                    licenseUserDetailsInfo.ContactEmail = contactEmail;
                    licenseUserDetailsInfo.ContractFilePath = contractFilePath;
                    licenseUserDetailsInfo.BusinessLicenseFilePath = businessLicenseFilePath;
                    licenseUserDetailsInfo.Description = description;
                    var update = await _licenseUserDetailsRepository.Update(licenseUserDetailsInfo, token);
                    return new KeyValuePair<bool, object>(update, $"修改{(update ? "成功" : "失败")}");
                }
                else {
                    var insert = await _licenseUserDetailsRepository.Insert(new LicenseUserDetailsInfo() {
                        CreateTime = DateTime.Now,
                        CompanyAddress = companyAddress,
                        CompanyName = companyName,
                        ContactEmail = contactEmail,
                        ContractFilePath = contractFilePath,
                        BusinessLicenseFilePath = businessLicenseFilePath,
                        Description = description,
                        UserId = licenseUserInfo.Id
                    }, token);
                    return new KeyValuePair<bool, object>(insert, $"修改{(insert ? "成功" : "失败")}");
                }
            }
            return new KeyValuePair<bool, object>(false, "账号不存在!");
        }

        public Task<KeyValuePair<bool, object>> UpdateProfile(string userCode, string password, CancellationToken token) {
            throw new NotImplementedException();
        }

        public async Task<KeyValuePair<bool, object>> ChangePassword(string userCode, string oldPassWord, string newPassWord, CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.FirstOrDefault(f =>
                f.UserCode.Equals(userCode) &&
                f.PassWord.Equals(oldPassWord), token);
            if (licenseUserInfo is not null) {
                licenseUserInfo.PassWord = newPassWord;
                var update = await _licenseUserRepository.Update(licenseUserInfo, token);

                return new KeyValuePair<bool, object>(update, update ? "修改密码成功" : "修改密码失败!");
            }
            return new KeyValuePair<bool, object>(false, "密码错误!");
        }

        public Task<KeyValuePair<bool, object>> Info(string userCode, CancellationToken token) {
            return _licenseUserRepository.DetailsInfo(userCode, token);
        }

        public async Task<KeyValuePair<bool, object>> FreezeUser(string userCode, bool isFreeze, CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.FirstOrDefault(f =>
                f.UserCode.Equals(userCode), token);
            if (licenseUserInfo is not null) {
                licenseUserInfo.Status = isFreeze ? UserStatus.Frozen : UserStatus.Active;
                var update = await _licenseUserRepository.Update(licenseUserInfo, token);

                return new KeyValuePair<bool, object>(update, $"{(isFreeze ? "冻结" : "解冻")}{(update ? "成功" : "失败")}");
            }
            return new KeyValuePair<bool, object>(false, "账号不存在!");
        }

        public async Task<KeyValuePair<bool, object>> SetUserIcon(string userCode, string iconUrlPath, CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.FirstOrDefault(f =>
                f.UserCode.Equals(userCode), token);
            if (licenseUserInfo is not null) {
                licenseUserInfo.UserIcon = iconUrlPath;
                var update = await _licenseUserRepository.Update(licenseUserInfo, token);

                return new KeyValuePair<bool, object>(update, $"设置{(update ? "成功" : "失败")}");
            }
            return new KeyValuePair<bool, object>(false, "账号不存在!");
        }

        public Task<KeyValuePair<bool, object>> TenantInfos(CancellationToken token) {
            return _licenseUserRepository.SelectOrderByDescending(s => s.Role == UserRole.Tenant, o => o.Id, token);
        }
    }
}