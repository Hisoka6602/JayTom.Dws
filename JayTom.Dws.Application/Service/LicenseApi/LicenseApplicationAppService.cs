using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.LicenseApi;
using JayTom.Dws.Domain.Service.LicenseApi;

namespace JayTom.Dws.Application.Service.LicenseApi {

    public class LicenseApplicationAppService : ILicenseApplicationAppService {
        private readonly ILicenseApplicationService _licenseApplicationService;

        public LicenseApplicationAppService(ILicenseApplicationService licenseApplicationService) {
            _licenseApplicationService = licenseApplicationService;
        }

        public Task<KeyValuePair<bool, object>> CreateApplication(string applicationName, string description, string ipAddress, List<LicenseFeatureDto> licenseFeatures,
            CancellationToken token) {
            return _licenseApplicationService.CreateApplication(applicationName, description, ipAddress, licenseFeatures,
                 token);
        }

        public Task<KeyValuePair<bool, object>> UpdateApplication(long applicationId, string description, string ipAddress, List<LicenseFeatureDto> licenseFeatures,
            CancellationToken token) {
            return _licenseApplicationService.UpdateApplication(applicationId, description, ipAddress, licenseFeatures,
                token);
        }

        public Task<KeyValuePair<bool, object>> CreateApplicationTemplate(long licenseApplicationInfoId, string templateName, string createBy, CancellationToken token) {
            return _licenseApplicationService.CreateApplicationTemplate(licenseApplicationInfoId, templateName, createBy,
                token);
        }

        public Task<KeyValuePair<bool, object>> SetTemplatePermissions(string userCode, long templateId, List<LicenseFeatureDto> licenseFeatures, CancellationToken token) {
            return _licenseApplicationService.SetTemplatePermissions(userCode, templateId, licenseFeatures, token);
        }

        public Task<KeyValuePair<bool, object>> ApplicationData(CancellationToken token) {
            return _licenseApplicationService.ApplicationData(token);
        }

        public Task<KeyValuePair<bool, object>> TemplateData(string userCode, CancellationToken token) {
            return _licenseApplicationService.TemplateData(userCode, token);
        }

        public Task<KeyValuePair<bool, object>> DeleteApplication(long applicationId, CancellationToken token) {
            return _licenseApplicationService.DeleteApplication(applicationId, token);
        }

        public Task<KeyValuePair<bool, object>> DeleteTemplate(string userCode, long templateId, CancellationToken token) {
            return _licenseApplicationService.DeleteTemplate(userCode, templateId, token);
        }
    }
}