using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.LicenseApi;

namespace JayTom.Dws.Application.Service.LicenseApi {

    public interface ILicenseApplicationAppService {

        /// <summary>
        /// 创建应用
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="ipAddress"></param>
        /// <param name="licenseFeatures"></param>
        /// <param name="token"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> CreateApplication(string applicationName,
            string description,
            string ipAddress,
            List<LicenseFeatureDto> licenseFeatures, CancellationToken token);

        /// <summary>
        /// 更新应用信息
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="description"></param>
        /// <param name="ipAddress"></param>
        /// <param name="licenseFeatures"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> UpdateApplication(long applicationId,
            string description,
            string ipAddress,
            List<LicenseFeatureDto> licenseFeatures, CancellationToken token);

        /// <summary>
        /// 创建应用模板
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="createBy"></param>
        /// <param name="token"></param>
        /// <param name="licenseApplicationInfoId"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> CreateApplicationTemplate(
            long licenseApplicationInfoId,
            string templateName,
            string createBy,
            CancellationToken token);

        /// <summary>
        /// 设置模板权限
        /// </summary>
        /// <param name="licenseFeatures"></param>
        /// <param name="token"></param>
        /// <param name="userCode"></param>
        /// <param name="templateId"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> SetTemplatePermissions(string userCode, long templateId, List<LicenseFeatureDto> licenseFeatures, CancellationToken token);

        /// <summary>
        /// 获取应用列表
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> ApplicationData(CancellationToken token);

        /// <summary>
        /// 获取模板列表
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> TemplateData(string userCode, CancellationToken token);

        /// <summary>
        /// 删除应用
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> DeleteApplication(long applicationId, CancellationToken token);

        /// <summary>
        /// 删除模板
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="token"></param>
        /// <param name="userCode"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> DeleteTemplate(string userCode, long templateId, CancellationToken token);
    }
}