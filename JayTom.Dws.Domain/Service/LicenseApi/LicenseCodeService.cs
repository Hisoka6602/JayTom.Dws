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

                var licenseCodeInfo = await _licenseCodeRepository.FirstOrDefault(w =>
                    (userId == 0 || w.UserId.Equals(userId)) && w.LicenseCode.Equals(licenseCode), token);
                if (licenseCodeInfo is not null) {
                    licenseCodeInfo.ExpirationDate = expirationDate;
                    var update = await _licenseCodeRepository.Update(licenseCodeInfo, token);
                    return new KeyValuePair<bool, object>(update, $"操作{(update ? "成功" : "失败")}");
                }

                return new KeyValuePair<bool, object>(false, "找不到该授权码!");
            }
            else {
                return new KeyValuePair<bool, object>(false, "您无权限访问");
            }
        }

        public async Task<KeyValuePair<bool, object>> FreezeLicenseCode(string userCode, string licenseCode, bool isFreeze, CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.
                FirstOrDefault(f => f.UserCode.Equals(userCode), token);
            if (licenseUserInfo is not null) {
                long userId = 0;
                if (licenseUserInfo.Role != UserRole.SuperAdmin) {
                    userId = licenseUserInfo.Id;
                }

                var licenseCodeInfo = await _licenseCodeRepository.FirstOrDefault(w =>
                    (userId == 0 || w.UserId.Equals(userId)) && w.LicenseCode.Equals(licenseCode), token);
                if (licenseCodeInfo is not null) {
                    licenseCodeInfo.IsAvailable = !isFreeze;
                    var update = await _licenseCodeRepository.Update(licenseCodeInfo, token);
                    return new KeyValuePair<bool, object>(update, $"操作{(update ? "成功" : "失败")}");
                }

                return new KeyValuePair<bool, object>(false, "找不到该授权码!");
            }
            else {
                return new KeyValuePair<bool, object>(false, "您无权限访问");
            }
        }

        public async Task<KeyValuePair<bool, object>> BulkExtendLicenseCodeValidity(string userCode, List<string> licenseCodes, DateTime expirationDate,
            CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.
                FirstOrDefault(f => f.UserCode.Equals(userCode), token);
            if (licenseUserInfo is not null) {
                long userId = 0;
                if (licenseUserInfo.Role != UserRole.SuperAdmin) {
                    userId = licenseUserInfo.Id;
                }

                var licenseCodeInfos = await _licenseCodeRepository
                    .Select(s =>
                            (userId == 0 || s.UserId.Equals(userId)) && licenseCodes.Contains(s.LicenseCode),
                        o => o.Id, token);

                var list = licenseCodeInfos?.Select(s => s.LicenseCode)?.ToList();
                var excepts = licenseCodes?.Except(list ?? new List<string>())?.ToList();

                if (excepts?.Any() == true) {
                    return new KeyValuePair<bool, object>(false, $"找不到授权码:{string.Join(",", excepts)}");
                }
                foreach (var licenseCodeInfo in licenseCodeInfos ?? new List<LicenseCodeInfo>()) {
                    licenseCodeInfo.ExpirationDate = expirationDate;
                }

                var updateRange = await _licenseCodeRepository.UpdateRange(licenseCodeInfos ?? new List<LicenseCodeInfo>(), token);

                return new KeyValuePair<bool, object>(updateRange, $"操作{(updateRange ? "成功" : "失败")}");
            }
            else {
                return new KeyValuePair<bool, object>(false, "您无权限访问");
            }
        }
    }
}