using System.Drawing;
using JayTom.Dws.LicenseApiClient.Data.Models;

namespace JayTom.Dws.LicenseApiClient.Api {

    public class LicenseApiRequest : ILicenseApiRequest {
        public static string? Token { get; private set; }

        public LicenseApiRequest() {
            //读配置文件(获取域名)
        }

        public Task<KeyValuePair<bool, object>> Register(string userCode, string userName, string passWord, string phone, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> Login(string loginCode, string passWord, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> UpdateProfile(string? userName, string? phone, string companyName, string companyAddress, string contactEmail,
            string description, string contractFilePath, string businessLicenseFilePath, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> ChangePassword(string oldPassWord, string newPassWord, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> Info(CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> FreezeUser(string userCode, bool isFreeze, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> ChangeUserIcon(Image iconImage, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> CreateApplication(string applicationName, string description, List<FeatureItemModel>? featureInfos, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> CreateApplicationTemplate(long licenseApplicationInfoId, string templateName, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> SetTemplatePermissions(long templateId, List<FeatureItemModel>? featureInfos, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> ApplicationData(CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> TemplateData(CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> DeleteApplication(long deleteApplicationId, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> DeleteTemplate(long deleteTemplateId, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> CreateLicenseCode(long templateInfoId, int maxClientCount, DateTime expirationDate, string clientName,
            CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> LicenseCodeData(CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> ExtendLicenseCodeValidity(string licenseCode, DateTime expirationDate, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> FreezeLicenseCode(string licenseCode, bool isFreeze, CancellationToken token) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, object>> DownloadLicenseFile(string licenseCode, string machineCode, CancellationToken token) {
            throw new NotImplementedException();
        }
    }
}