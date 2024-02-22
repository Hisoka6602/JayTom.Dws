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

        public async Task<KeyValuePair<bool, object>> GetUserCode(string licenseCode, CancellationToken token) {
            if (!string.IsNullOrEmpty(licenseCode)) {
                return await _licenseCodeRepository.FirstDetails(f => f.LicenseCode.Equals(licenseCode), token);
            }
            else {
                return new KeyValuePair<bool, object>(false, "授权码不能为空");
            }
        }

        public async Task<KeyValuePair<bool, object>> UnbindMachineCode(string userCode, string licenseCode, string machineCode, CancellationToken token) {
            //获取授权码

            var (key, value) = await _licenseCodeRepository.FirstDetails(f => f.UserInfo != null &&
                f.LicenseClientBindingInfo != null &&
                (f.UserInfo.Role == UserRole.SuperAdmin ||
                 f.UserInfo.UserCode.Equals(userCode)) &&
                f.LicenseCode.Equals(licenseCode) &&
                f.LicenseClientBindingInfo.Any(a =>
                    a.MachineCode.Equals(machineCode)), token);

            if (key && value is LicenseCodeInfo info) {
                if (!info.IsAvailable) {
                    return new KeyValuePair<bool, object>(false, "授权码不可用");
                }

                var unbindMachineCode = await _licenseCodeRepository.UnbindMachineCode(machineCode, token);
                return new KeyValuePair<bool, object>(unbindMachineCode, $"解绑{(unbindMachineCode ? "成功" : "失败")}");
            }
            else {
                return new KeyValuePair<bool, object>(false, "解绑失败");
            }
        }

        public async Task<KeyValuePair<bool, object>> ActivateAuthorization(string licenseCode, string machineCode, string remarks, CancellationToken token) {
            var (key, value) = await GetUserCode(licenseCode, token);
            if (key && value is LicenseCodeInfo { UserInfo: not null } info) {
                if (!info.IsAvailable) {
                    return new KeyValuePair<bool, object>(false, "授权码不可用");
                }
                if (info.MaxClientCount <= info.ActivatedClientCount) {
                    return new KeyValuePair<bool, object>(false, "无可激活数量");
                }
                if (DateTime.Now.CompareTo(info.ExpirationDate) >= 0) {
                    return new KeyValuePair<bool, object>(false, "授权码已到期");
                }
                if (info.LicenseClientBindingInfo?.Any(a => a.MachineCode.Equals(machineCode)) == true) {
                    var licenseClientBindingInfo = await _licenseClientBindingRepository.FirstOrDefault(f => f.MachineCode.Equals(machineCode) &&
                        f.LicenseCodeId.Equals(info.Id), token);
                    if (licenseClientBindingInfo is not null) {
                        licenseClientBindingInfo.LastVerifiedDate = DateTime.Now;
                        _licenseClientBindingRepository.Update(licenseClientBindingInfo);
                    }

                    return new KeyValuePair<bool, object>(true, "该机器码已激活过");
                }
                //插入绑定机器码
                //增加已激活数量
                var clientBindingInfo = new LicenseClientBindingInfo() {
                    CreateTime = DateTime.Now,
                    FirstActivatedDate = DateTime.Now,
                    LastVerifiedDate = DateTime.Now,
                    LicenseCodeId = info.Id,
                    MachineCode = machineCode,
                    Remarks = remarks
                };
                var insert = await _licenseClientBindingRepository.Insert(clientBindingInfo, token);

                if (insert) {
                    info.ActivatedClientCount += 1;
                    var update = await _licenseCodeRepository.Update(info, token);
                    return update ? new KeyValuePair<bool, object>(true, "激活成功") : new KeyValuePair<bool, object>(false, "激活失败");
                }
                else {
                    //return new KeyValuePair<bool, object>(false, "设置机器码失败");
                    return new KeyValuePair<bool, object>(false, $"设置机器码失败:UserId:{info.UserInfo.Id},LicenseCodeId:{info.Id}");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, value.ToString() ?? string.Empty);
            }
        }
    }
}