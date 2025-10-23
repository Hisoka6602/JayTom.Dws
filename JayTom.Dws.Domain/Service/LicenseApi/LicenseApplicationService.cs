using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.License;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.LicenseApi;
using JayTom.Dws.Domain.Repository.License;

namespace JayTom.Dws.Domain.Service.LicenseApi {

    public class LicenseApplicationService : ILicenseApplicationService {
        private readonly ILicenseApplicationRepository _licenseApplicationRepository;
        private readonly ILicensePermissionTemplateRepository _licensePermissionTemplateRepository;
        private readonly ILicenseUserRepository _licenseUserRepository;
        private readonly ILicenseFeatureRepository _licenseFeatureRepository;

        public LicenseApplicationService(ILicenseApplicationRepository
            licenseApplicationRepository,
            ILicensePermissionTemplateRepository licensePermissionTemplateRepository,
            ILicenseUserRepository licenseUserRepository,
            ILicenseFeatureRepository licenseFeatureRepository) {
            _licenseApplicationRepository = licenseApplicationRepository;
            _licensePermissionTemplateRepository = licensePermissionTemplateRepository;
            _licenseUserRepository = licenseUserRepository;
            _licenseFeatureRepository = licenseFeatureRepository;
        }

        public async Task<KeyValuePair<bool, object>> CreateApplication(string applicationName, string description, string ipAddress, List<LicenseFeatureDto> licenseFeatures,
            CancellationToken token) {
            var insert = await _licenseApplicationRepository.Insert(new LicenseApplicationInfo() {
                ApplicationName = applicationName,
                Description = description,
                ModifyIp = ipAddress,
                CreateTime = DateTime.Now,
                LicenseFeatureInfos = licenseFeatures.Select(s => new LicenseFeatureInfo() {
                    CreateTime = DateTime.Now,
                    Description = s.Description,
                    FeatureGuid = s.FeatureGuid,
                    FeatureName = s.FeatureName,
                    IsActive = s.IsActive
                }).ToList()
            }, token);
            return new KeyValuePair<bool, object>(insert, $"创建{(insert ? "成功" : "失败")}");
        }

        public async Task<KeyValuePair<bool, object>> UpdateApplication(long applicationId, string description, string ipAddress, List<LicenseFeatureDto> licenseFeatures,
            CancellationToken token) {
            var licenseApplicationInfo = await _licenseApplicationRepository.FirstOrDefault(f => f.Id.Equals(applicationId),
                token);

            if (licenseApplicationInfo is not null) {
                licenseApplicationInfo.Description = description;
                licenseApplicationInfo.ModifyIp = ipAddress;
                licenseApplicationInfo.ModifyTime = DateTime.Now;
                //相同模板的也需要修改

                var licenseFeatureInfos = await _licenseFeatureRepository.Select(s =>
                        s.LicenseApplicationInfoId.Equals(applicationId),
                    o => o.Id);
                //删除

                var deleteRange = await _licenseFeatureRepository.DeleteRange(licenseFeatureInfos, token);
                if (deleteRange) {
                    var insertRange = await _licenseFeatureRepository.InsertRange(licenseFeatures.Select(s => new LicenseFeatureInfo {
                        CreateTime = DateTime.Now,
                        Description = s.Description,
                        FeatureGuid = s.FeatureGuid,
                        FeatureName = s.FeatureName,
                        IsActive = s.IsActive,
                        ModifyIp = ipAddress,
                        ModifyTime = DateTime.Now,
                        LicenseApplicationInfoId = licenseApplicationInfo.Id,
                    })?.ToList() ?? new List<LicenseFeatureInfo>(), token);
                    if (insertRange) {
                        var update = await _licenseApplicationRepository.Update(licenseApplicationInfo, token);
                        return new KeyValuePair<bool, object>(update, update ? "更新成功" : "更新失败");
                    }
                    else {
                        return new KeyValuePair<bool, object>(false, "模块更新失败!");
                    }
                }
                else {
                    return new KeyValuePair<bool, object>(false, "程序更新失败!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "应用不存在!");
            }
        }

        public async Task<KeyValuePair<bool, object>> ApplicationData(CancellationToken token) {
            var (key, value) = await _licenseApplicationRepository.Details(s => s.Id > 0 &&
                s.LicensePermissionTemplateId == null, token);
            if (key && value is List<LicenseApplicationInfo> infos) {
                if (infos?.Any() == true) {
                    return new KeyValuePair<bool, object>(true, infos);
                }
            }

            return new KeyValuePair<bool, object>(false, "未获取到任何数据");
        }

        public async Task<KeyValuePair<bool, object>> CreateApplicationTemplate(long licenseApplicationInfoId, string templateName, string createBy, CancellationToken token) {
            var (key, value) = await _licenseApplicationRepository.FirstDetails(f =>
                f.Id.Equals(licenseApplicationInfoId), token);
            if (value is LicenseApplicationInfo info) {
                info.Id = 0;
                var insert = await _licensePermissionTemplateRepository.Insert(new LicensePermissionTemplateInfo() {
                    TemplateName = templateName,
                    CreateTime = DateTime.Now,
                    CreateBy = createBy,
                    LicenseApplicationInfo = new LicenseApplicationInfo() {
                        ApplicationName = info.ApplicationName,
                        CreateTime = DateTime.Now,
                        Description = info.Description,
                        LicenseFeatureInfos = info.LicenseFeatureInfos?.Select(s => new LicenseFeatureInfo {
                            CreateTime = DateTime.Now,
                            Description = s.Description,
                            FeatureGuid = s.FeatureGuid,
                            FeatureName = s.FeatureName,
                            IsActive = s.IsActive,
                        })?.ToList(),
                    }
                }, token);
                return new KeyValuePair<bool, object>(insert, $"创建{(insert ? "成功" : "失败")}");
            }
            else {
                return new KeyValuePair<bool, object>(false, "不存在该应用程序");
            }
        }

        public async Task<KeyValuePair<bool, object>> TemplateData(string userCode, CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.
                FirstOrDefault(f => f.UserCode.Equals(userCode), token);
            if (licenseUserInfo is not null) {
                /*var code = userCode;
                if (licenseUserInfo.Role == UserRole.SuperAdmin) {
                    code = null;
                }*/
                //暂时先所有人可以见
                string? code = null;
                var (key, value) = await _licensePermissionTemplateRepository.Details(w =>
                    code == null || w.CreateBy.Equals(code), token);

                if (key && value is List<LicensePermissionTemplateInfo> infos) {
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

        public async Task<KeyValuePair<bool, object>> DeleteTemplate(string userCode, long templateId, CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.
                FirstOrDefault(f => f.UserCode.Equals(userCode), token);
            if (licenseUserInfo is not null) {
                var code = userCode;
                if (licenseUserInfo.Role == UserRole.SuperAdmin) {
                    code = null;
                }

                var templateInfo = await _licensePermissionTemplateRepository.FirstOrDefault(f =>
                   f.Id.Equals(templateId) && (code == null || f.CreateBy.Equals(code)), token);

                if (templateInfo is not null) {
                    var delete = await _licensePermissionTemplateRepository.Delete(templateInfo, token);
                    return new KeyValuePair<bool, object>(delete, $"删除{(delete ? "成功" : "失败")}");
                }
                else {
                    return new KeyValuePair<bool, object>(false, "查询不到该模板信息");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "您无权限访问");
            }
        }

        public async Task<KeyValuePair<bool, object>> DeleteApplication(long applicationId, CancellationToken token) {
            var licenseApplicationInfo = await _licenseApplicationRepository.
                FirstOrDefault(f =>
                    f.Id.Equals(applicationId), token);
            if (licenseApplicationInfo is not null) {
                var delete = await _licenseApplicationRepository.Delete(licenseApplicationInfo, token);
                return new KeyValuePair<bool, object>(delete, $"删除{(delete ? "成功" : "失败")}");
            }
            else {
                return new KeyValuePair<bool, object>(false, "查询不到该应用信息");
            }
        }

        public async Task<KeyValuePair<bool, object>> SetTemplatePermissions(string userCode, long templateId, List<LicenseFeatureDto> licenseFeatures, CancellationToken token) {
            var licenseUserInfo = await _licenseUserRepository.
                FirstOrDefault(f => f.UserCode.Equals(userCode), token);
            if (licenseUserInfo is not null) {
                var code = userCode;
                if (licenseUserInfo.Role == UserRole.SuperAdmin) {
                    code = null;
                }

                //获取模板
                var (key, value) = await _licensePermissionTemplateRepository.
                    FirstDetails(f =>
                        f.Id.Equals(templateId), token);
                if (key && value is LicensePermissionTemplateInfo info) {
                    var (b, o) = await _licenseApplicationRepository.FirstDetails(f =>
                        info.LicenseApplicationInfo != null &&
                        f.ApplicationName.Equals(info.LicenseApplicationInfo.ApplicationName) &&
                        f.LicensePermissionTemplateId == null, token);
                    if (b && o is LicenseApplicationInfo licenseApplicationInfo) {
                        var licenseFeatureInfos = licenseApplicationInfo.LicenseFeatureInfos?.Select(s =>
                            new LicenseFeatureInfo {
                                CreateTime = info.LicenseApplicationInfo
                                    ?.LicenseFeatureInfos
                                    ?.FirstOrDefault(f =>
                                        f.FeatureGuid.Equals(s.FeatureGuid))?.CreateTime ?? DateTime.Now,
                                Description = licenseFeatures
                                    ?.FirstOrDefault(f =>
                                        f.FeatureGuid.Equals(s.FeatureGuid))?.Description ?? string.Empty,
                                FeatureGuid = s.FeatureGuid,
                                FeatureName = s.FeatureName,

                                IsActive = licenseFeatures
                                    ?.FirstOrDefault(f =>
                                        f.FeatureGuid.Equals(s.FeatureGuid))?.IsActive ?? false,
                                LicenseApplicationInfoId = info.LicenseApplicationInfo
                                    ?.LicenseFeatureInfos
                                    ?.FirstOrDefault(f =>
                                        f.FeatureGuid.Equals(s.FeatureGuid))?.LicenseApplicationInfoId ?? 0,
                            })?.ToList();

                        var featureInfos = await _licenseFeatureRepository.Select(s =>
                            s.LicenseApplicationInfoId.Equals(info.LicenseApplicationInfo!.Id), o => o.Id, token);

                        var deleteRange = await _licenseFeatureRepository.DeleteRange(
                            featureInfos,
                            token);

                        info.LicenseApplicationInfo!.LicenseFeatureInfos = licenseFeatureInfos;

                        var update = await _licensePermissionTemplateRepository.Update(info, token);
                        return new KeyValuePair<bool, object>(update, $"修改{(update && deleteRange ? "成功" : "失败")}");
                    }
                    else {
                        return new KeyValuePair<bool, object>(false, "查询不到应用信息");
                    }
                }
                else {
                    return new KeyValuePair<bool, object>(false, "查询不到模板信息");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "您无权限访问");
            }
        }
    }
}