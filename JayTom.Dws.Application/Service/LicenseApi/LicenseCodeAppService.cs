using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Service.LicenseApi;

namespace JayTom.Dws.Application.Service.LicenseApi {

    public class LicenseCodeAppService : ILicenseCodeAppService {
        private readonly ILicenseCodeService _licenseCodeService;

        public LicenseCodeAppService(ILicenseCodeService licenseCodeService) {
            _licenseCodeService = licenseCodeService;
        }

        public Task<KeyValuePair<bool, object>> CreateLicenseCode(long templateInfoId, string userCode, int maxClientCount, DateTime expirationDate, string clientName,
            CancellationToken token) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var licenseCode = new string(Enumerable.Repeat(chars, 32)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return _licenseCodeService.CreateLicenseCode(templateInfoId, userCode, licenseCode, maxClientCount, expirationDate,
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
    }
}