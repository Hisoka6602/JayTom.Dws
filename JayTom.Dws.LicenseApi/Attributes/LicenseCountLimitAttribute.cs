using RTools_NTS.Util;
using Newtonsoft.Json.Linq;
using JayTom.Dws.Data.License;
using JayTom.Dws.LicenseApi.Do;
using JayTom.Dws.Domain.Repository.License;
using System.ComponentModel.DataAnnotations;
using JayTom.Dws.Application.Service.LicenseApi;
using JayTom.Dws.Infrastructure.Repository.License;

namespace JayTom.Dws.LicenseApi.Attributes {

    /// <summary>
    /// 授权是否超上限
    /// </summary>
    public class LicenseCountLimitAttribute : ValidationAttribute {
        public bool IsEdit { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            var licenseUserAppService = validationContext.GetService<ILicenseUserAppService>();
            var code = string.Empty;
            var isSuperAdminCreated = false;
            var licenseCode = string.Empty;
            long templateInfoId = 0;

            if (value is int codeCount) {
                var httpContextAccessor = validationContext.GetService<IHttpContextAccessor>();
                if (httpContextAccessor is { HttpContext: not null }) {
                    code = httpContextAccessor.HttpContext.Response.HttpContext.User.Identity?.Name ?? string.Empty;
                    var result = licenseUserAppService?.Info(code, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (result is { Key: true, Value: LicenseUserInfo { Role: UserRole.SuperAdmin } }) {
                        //重新获取
                        isSuperAdminCreated = true;

                    }
                    switch (validationContext.ObjectInstance) {
                        case BulkCreateLicenseCodeDo model:
                            if (isSuperAdminCreated) {
                                code = model.UserCode;
                            }

                            templateInfoId = model.TemplateInfoId;
                            break;

                        case CreateLicenseCodeDo createModel:
                            if (isSuperAdminCreated) {
                                code = createModel.UserCode;
                            }

                            templateInfoId = createModel.TemplateInfoId;
                            break;

                        case UpdateLicenseCodeDo updateModel:
                            if (isSuperAdminCreated) {
                                code = updateModel.UserCode;
                            }

                            templateInfoId = updateModel.TemplateInfoId;
                            licenseCode = updateModel.LicenseCode;
                            break;
                    }

                }

                var licenseUserRepository = validationContext.GetService<ILicenseUserRepository>();
                if (licenseUserRepository is not null) {
                    var (key, o) = licenseUserRepository.DetailsInfo(code ?? string.Empty, CancellationToken.None).
                        ConfigureAwait(false).GetAwaiter().GetResult();
                    if (key && o is LicenseUserInfo licenseUserInfo) {
                        var licenseAppLicenseInfo = licenseUserInfo.AppLicenseInfos
                            ?.FirstOrDefault(f => f.LicensePermissionTemplateInfoId.
                                Equals(templateInfoId));
                        int maxLicenseCodeCount;
                        if (!isSuperAdminCreated && licenseAppLicenseInfo is null) {
                            maxLicenseCodeCount = 1;
                        }
                        else {
                            maxLicenseCodeCount = licenseAppLicenseInfo?.MaxLicenseCodeCount ?? codeCount;
                        }
                        //获取已使用的授权码数量
                        var sum = licenseUserInfo.LicenseCodeInfos?.Where(w => w.LicensePermissionTemplateInfoId.Equals(templateInfoId))
                            ?.Sum(s => s.MaxClientCount) ?? 0;

                        //获取授权码上限
                        if (IsEdit) {
                            sum = licenseUserInfo.LicenseCodeInfos?.Where(w => w.LicensePermissionTemplateInfoId.Equals(templateInfoId) &&
                                                                               !w.LicenseCode.Equals(licenseCode))
                                ?.Sum(s => s.MaxClientCount) ?? 0;
                        }
                        if (sum + codeCount > maxLicenseCodeCount) {
                            return new ValidationResult(ErrorMessage);
                        }
                    }
                }
            }

            return ValidationResult.Success;
        }
    }
}