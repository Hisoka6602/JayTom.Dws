using System;
using System.Linq;
using System.Text;
using JayTom.Dws.License;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Collections.Generic;
using JayTom.Dws.Domain.Service.LicenseApi;

namespace JayTom.Dws.Application.Service.LicenseApi {

    public class LicenseCodeAppService : ILicenseCodeAppService {
        private readonly ILicenseCodeService _licenseCodeService;

        public LicenseCodeAppService(ILicenseCodeService licenseCodeService) {
            _licenseCodeService = licenseCodeService;
        }

        public Task<KeyValuePair<bool, object>> CreateLicenseCode(long templateInfoId, string userCode, int maxClientCount, DateTime expirationDate, string clientName,
            bool isSuperAdminCreated,
            CancellationToken token) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random((int)DateTime.Now.Ticks);
            var licenseCode = new string(Enumerable.Repeat(chars, 32)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return _licenseCodeService.CreateLicenseCode(templateInfoId, userCode, licenseCode, maxClientCount, expirationDate,
                 clientName, isSuperAdminCreated, token);
        }

        public Task<KeyValuePair<bool, object>> BulkCreateLicenseCode(long templateInfoId, string userCode, DateTime expirationDate, string clientName,
            int licenseCodeCount, bool isSuperAdminCreated = false, CancellationToken token = default) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random((int)DateTime.Now.Ticks);
            var licenseCodes = Enumerable.Range(0, licenseCodeCount)
                .Select(_ => new string(Enumerable.Repeat(chars, 32)
                    .Select(s => s[random.Next(s.Length)]).ToArray()))
                .ToList();
            return _licenseCodeService.BulkCreateLicenseCode(templateInfoId, userCode,
                licenseCodes, expirationDate, clientName, isSuperAdminCreated, token);
        }

        public Task<KeyValuePair<bool, object>> UpdateLicenseCode(long templateInfoId, string userCode, string licenseCode, int maxClientCount,
            DateTime expirationDate, string clientName, CancellationToken token) {
            return _licenseCodeService.UpdateLicenseCode(templateInfoId, userCode, licenseCode, maxClientCount, expirationDate,
                clientName, token);
        }

        public Task<KeyValuePair<bool, object>> LicenseCodeData(string userCode, CancellationToken token) {
            return _licenseCodeService.LicenseCodeData(userCode, token);
        }

        public Task<KeyValuePair<bool, object>> ExtendLicenseCodeValidity(string userCode, string licenseCode, DateTime expirationDate, CancellationToken token) {
            return _licenseCodeService.ExtendLicenseCodeValidity(userCode, licenseCode, expirationDate, token);
        }

        public Task<KeyValuePair<bool, object>> FreezeLicenseCode(string userCode, string licenseCode, bool isFreeze, CancellationToken token) {
            return _licenseCodeService.FreezeLicenseCode(userCode, licenseCode, isFreeze, token);
        }

        public Task<KeyValuePair<bool, object>> BulkExtendLicenseCodeValidity(string userCode, List<string> licenseCodes, DateTime expirationDate,
            CancellationToken token) {
            return _licenseCodeService.BulkExtendLicenseCodeValidity(userCode, licenseCodes, expirationDate, token);
        }

        public async Task<KeyValuePair<bool, object>> GetLicenseFileUrl(string userCode, string licenseCode, string machineCode, string remarks, CancellationToken token) {
            var (key, value) = await _licenseCodeService.LicenseCodeData(userCode, token);
            if (key && value is List<LicenseCodeInfo> infos) {
                var licenseCodeInfo = infos?.FirstOrDefault(f => f.LicenseCode.Equals(licenseCode));
                if (licenseCodeInfo != null) {
                    if (licenseCodeInfo.MaxClientCount <= licenseCodeInfo.ActivatedClientCount &&
                        licenseCodeInfo.LicenseClientBindingInfo?.Any(a => a.MachineCode.Equals(machineCode)) != true) {
                        return new KeyValuePair<bool, object>(false, "无可激活数量");
                    }

                    /*if (!licenseCodeInfo.IsAvailable) {
                        return new KeyValuePair<bool, object>(false, "授权码不可用");
                    }

                    if (licenseCodeInfo.MaxClientCount <= licenseCodeInfo.ActivatedClientCount) {
                        return new KeyValuePair<bool, object>(false, "无可激活数量");
                    }

                    if (DateTime.Now.CompareTo(licenseCodeInfo.ExpirationDate) >= 0) {
                        return new KeyValuePair<bool, object>(false, "授权码已到期");
                    }*/
                    var path = $"{Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")}\\LicenseFile";
                    if (!Directory.Exists(path)) {
                        Directory.CreateDirectory(path);
                    }

                    var unixTimeMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    JayTom.Dws.License.LicenseManager.GenerateKeyPair(out var publicKeyXml, out var privateKeyXml);
                    var authorizationFile = LicenseManager.GenerateAuthorizationFile(new LicenseData() {
                        ExpirationDate = licenseCodeInfo.ExpirationDate,
                        MachineCode = machineCode,
                        LicenseCode = licenseCode,
                        UserName = licenseCodeInfo.ClientName,
                        CreationTime = DateTime.Now,
                        IsAvailable = licenseCodeInfo.IsAvailable,
                        Remarks = remarks
                    }, publicKeyXml, privateKeyXml, $"{path}\\{unixTimeMilliseconds}.key");

                    return new KeyValuePair<bool, object>(authorizationFile.Key, authorizationFile.Key ? $"{unixTimeMilliseconds}.key" : authorizationFile.Value);
                }
                else {
                    return new KeyValuePair<bool, object>(false, "未获取到授权码");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "未获取到授权码");
            }
            //
        }

        public Task<KeyValuePair<bool, object>> UnbindMachineCode(string userCode, string licenseCode, string machineCode, CancellationToken token) {
            return _licenseCodeService.UnbindMachineCode(userCode, licenseCode, machineCode, token);
        }

        public Task<KeyValuePair<bool, object>> GetUserCode(string licenseCode, CancellationToken token) {
            return _licenseCodeService.GetUserCode(licenseCode, token);
        }

        public Task<KeyValuePair<bool, object>> ActivateAuthorization(string licenseCode, string machineCode, string remarks, CancellationToken token) {
            return _licenseCodeService.ActivateAuthorization(licenseCode, machineCode, remarks, token);
        }
    }
}