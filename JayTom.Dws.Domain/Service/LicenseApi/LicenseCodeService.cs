using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Collections.Generic;
using JayTom.Dws.Domain.Repository.License;

namespace JayTom.Dws.Domain.Service.LicenseApi {

    public class LicenseCodeService : ILicenseCodeService {
        private readonly ILicenseCodeRepository _licenseCodeRepository;
        private readonly ILicenseClientBindingRepository _licenseClientBindingRepository;
        private readonly ILicenseUserRepository _licenseUserRepository;

        public LicenseCodeService(ILicenseCodeRepository licenseCodeRepository,
            ILicenseClientBindingRepository licenseClientBindingRepository,
            ILicenseUserRepository licenseUserRepository) {
            _licenseCodeRepository = licenseCodeRepository;
            _licenseClientBindingRepository = licenseClientBindingRepository;
            _licenseUserRepository = licenseUserRepository;
        }

        public async Task<KeyValuePair<bool, object>> CreateLicenseCode(long templateInfoId, string userCode, string licenseCode, int maxClientCount,
            DateTime expirationDate, string clientName, CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.FirstOrDefault(f =>
                f.UserCode.Equals(userCode), token);
            if (licenseUserInfo is not null) {
                var insert = await _licenseCodeRepository.Insert(new LicenseCodeInfo() {
                    LicensePermissionTemplateInfoId = templateInfoId,
                    MaxClientCount = maxClientCount,
                    ExpirationDate = expirationDate,
                    ClientName = clientName,
                    LicenseCode = licenseCode,
                    UserId = licenseUserInfo.Id,
                    CreateTime = DateTime.Now,
                    LicenseClientBindingInfo = new List<LicenseClientBindingInfo>()
                }, token);
                if (!insert) {
                    return new KeyValuePair<bool, object>(false, "创建失败!");
                }
                return new KeyValuePair<bool, object>(true, licenseCode);
            }
            else {
                return new KeyValuePair<bool, object>(false, "您无访问权限");
            }
        }

        public async Task<KeyValuePair<bool, object>> LicenseCodeData(string userCode, CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.
                FirstOrDefault(f => f.UserCode.Equals(userCode), token);
            if (licenseUserInfo is not null) {
                long userId = 0;
                if (licenseUserInfo.Role != UserRole.SuperAdmin) {
                    userId = licenseUserInfo.Id;
                }

                var (key, value) = await _licenseCodeRepository.Details(w =>
                    userId == 0 || w.UserId.Equals(userId), token);

                if (key && value is List<LicenseCodeInfo> infos) {
                    if (infos?.Any() == true) {
                        return new KeyValuePair<bool, object>(true, infos);
                    }
                }

                return new KeyValuePair<bool, object>(false, "未获取到任何数据");
            }
            else {
                return new KeyValuePair<bool, object>(false, "您无权限访问");
            }
        }

        public async Task<KeyValuePair<bool, object>> ExtendLicenseCodeValidity(string userCode, string licenseCode, DateTime expirationDate, CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.
                FirstOrDefault(f => f.UserCode.Equals(userCode), token);
            if (licenseUserInfo is not null) {
                long userId = 0;
                if (licenseUserInfo.Role != UserRole.SuperAdmin) {
                    userId = licenseUserInfo.Id;
                }

                var (key, value) = await _licenseCodeRepository.Details(w =>
                    userId == 0 || w.UserId.Equals(userId), token);

                if (key && value is List<LicenseCodeInfo> infos) {
                    if (infos?.Any() == true) {
                        return new KeyValuePair<bool, object>(true, infos);
                    }
                }

                return new KeyValuePair<bool, object>(false, "未获取到任何数据");
            }
            else {
                return new KeyValuePair<bool, object>(false, "您无权限访问");
            }
        }
    }
}