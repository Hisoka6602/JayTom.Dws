using JayTom.Dws.Data.License;
using Microsoft.AspNetCore.Mvc;
using JayTom.Dws.LicenseApi.Do;
using JayTom.Dws.LicenseApi.Vo;
using Microsoft.AspNetCore.Http;
using JayTom.Dws.LicenseApi.Dto;
using JayTom.Dws.LicenseApi.Utils;
using JayTom.Dws.LicenseApi.Filter;
using JayTom.Dws.LicenseApi.Attributes;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using JayTom.Dws.Application.Service.LicenseApi;

namespace JayTom.Dws.LicenseApi.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase {
        private readonly ILicenseUserAppService _licenseUserAppService;
        private readonly ILogger<UserController> _logger;

        public UserController(ILicenseUserAppService licenseUserAppService,
            ILogger<UserController> logger) {
            _licenseUserAppService = licenseUserAppService;
            _logger = logger;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("Register")]
        public async Task<JsonResult> Register([FromBody] RegisterDo param,
            CancellationToken cancellationToken) {
            var ipAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString();
            var keyValuePair = await _licenseUserAppService.Register(param.UserCode,
                param.UserName, param.PassWord,
                param.Phone, ipAddress ?? string.Empty, cancellationToken);

            return keyValuePair is { Key: true, Value: string msg } ? JsonResultVo.Success(msg) : JsonResultVo.Fail("注册失败!");
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("Login"), UserStatus(Status = UserStatus.Active)]
        public async Task<JsonResult> Login([FromBody] LoginDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _licenseUserAppService.
                Login(param.LoginCode, param.PassWord,
                    cancellationToken);
            return key ? JsonResultVo.Success("登录成功", data: value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 修改资料
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("UpdateProfile"), Authorize, UserStatus(Status = UserStatus.Active)]
        public async Task<JsonResult> UpdateProfile([FromBody] UpdateProfileDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseUserAppService.UpdateProfile(code,
                param.UserName,
                param.Phone,
                param.CompanyName,
                param.CompanyAddress,
                param.ContactEmail,
                param.Description,
                param.ContractFilePath,
                param.BusinessLicenseFilePath, cancellationToken);

            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("ChangePassword"), Authorize, UserStatus(Status = UserStatus.Active)]
        public async Task<JsonResult> ChangePassword([FromBody] ChangePasswordDo param,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseUserAppService.ChangePassword(code ?? string.Empty, param.OldPassWord,
                param.NewPassWord, cancellationToken);
            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 个人信息
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpGet("Info"), Authorize, UserStatus(Status = UserStatus.Active)]
        public async Task<JsonResult> Info(CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var (key, value) = await _licenseUserAppService.Info(code, cancellationToken);

            if (key && value is LicenseUserInfo info) {
                return JsonResultVo.Success("查询成功", data: new UserInfoDto() {
                    Pid = info.Pid,
                    UserName = info.UserName,
                    UserCode = info.UserCode,
                    Phone = DataUtils.MaskPhoneNumber(info.Phone),
                    Role = info.Role,
                    Status = info.Status,
                    UserIcon = info.UserIcon,
                    RegisterTime = info.CreateTime,
                    LicenseCodeInfos = info.LicenseCodeInfos?.Select(s => new LicenseCodeInfo {
                        LicenseCode = s.LicenseCode,
                        MaxClientCount = s.MaxClientCount,
                        ActivatedClientCount = s.ActivatedClientCount,
                        ExpirationDate = s.ExpirationDate,
                        ClientName = s.ClientName,
                        IsAvailable = s.IsAvailable
                    })?.ToList(),
                    UserDetailsInfo = new LicenseUserDetailsInfo() {
                        CompanyAddress = info.UserDetailsInfo?.CompanyAddress ?? string.Empty,
                        CompanyName = info.UserDetailsInfo?.CompanyName ?? string.Empty,
                        ContactEmail = info.UserDetailsInfo?.ContactEmail ?? string.Empty,
                        Description = info.UserDetailsInfo?.Description ?? string.Empty,
                        ContractFilePath = info.UserDetailsInfo?.ContractFilePath ?? string.Empty,
                        BusinessLicenseFilePath = info.UserDetailsInfo?.BusinessLicenseFilePath ?? string.Empty,
                    }
                });
            }

            return key ? JsonResultVo.Success("查询成功", data: value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 冻结用户
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("FreezeUser"), Authorize, UserRole(Role = (int)UserRole.SuperAdmin)]
        public async Task<JsonResult> FreezeUser([FromBody] FreezeUserDo param,
            CancellationToken cancellationToken) {
            var (key, value) = await _licenseUserAppService.FreezeUser(param.UserCode,
                param.IsFreeze, cancellationToken);
            return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }

        /// <summary>
        /// 修改用户头像
        /// </summary>
        /// <param name="imageFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpPost("ChangeUserIcon"), Authorize]
        public async Task<JsonResult> ChangeUserIcon([Required(ErrorMessage = "图片不能为空"), Image] IFormFile imageFile,
            CancellationToken cancellationToken) {
            var code = HttpContext.Response.HttpContext.User.Identity?.Name;
            var path = $"{Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")}\\Image";
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            var imagePath = $"{path}\\{imageFile.FileName}";
            try {
                await using (var stream = new FileStream(imagePath, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                    await imageFile.CopyToAsync(stream, cancellationToken);
                    await Task.Yield();
                }

                var iconPath = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/Scr/Image/{imageFile.FileName}";

                var (key, value) = await _licenseUserAppService.SetUserIcon(code ?? string.Empty, iconPath, cancellationToken);
                return key ? JsonResultVo.Success(value.ToString() ?? string.Empty) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
            }
            catch (Exception e) {
                _logger.LogError($"图片上传异常:{e}");
            }
            return JsonResultVo.Fail("图片上传失败!");
        }

        /// <summary>
        /// 租户信息
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Produces("application/json")]
        [HttpGet("TenantInfos"), Authorize, UserRole(Role = (int)UserRole.SuperAdmin), UserStatus(Status = UserStatus.Active)]
        public async Task<JsonResult> TenantInfos(CancellationToken cancellationToken) {
            var (key, value) = await _licenseUserAppService.TenantInfos(cancellationToken);
            if (key && value is List<LicenseUserInfo> infos) {
                var userInfoDtos = infos?.Select(info => new UserInfoDto() {
                    Pid = info.Pid,
                    UserName = info.UserName,
                    UserCode = info.UserCode,
                    Phone = DataUtils.MaskPhoneNumber(info.Phone),
                    Role = info.Role,
                    Status = info.Status,
                    UserIcon = info.UserIcon,
                    RegisterTime = info.CreateTime,
                    LicenseCodeInfos = info.LicenseCodeInfos?.Select(s => new LicenseCodeInfo {
                        LicenseCode = s.LicenseCode,
                        MaxClientCount = s.MaxClientCount,
                        ActivatedClientCount = s.ActivatedClientCount,
                        ExpirationDate = s.ExpirationDate,
                        ClientName = s.ClientName,
                        IsAvailable = s.IsAvailable
                    })?.ToList(),
                    UserDetailsInfo = new LicenseUserDetailsInfo() {
                        CompanyAddress = info.UserDetailsInfo?.CompanyAddress ?? string.Empty,
                        CompanyName = info.UserDetailsInfo?.CompanyName ?? string.Empty,
                        ContactEmail = info.UserDetailsInfo?.ContactEmail ?? string.Empty,
                        Description = info.UserDetailsInfo?.Description ?? string.Empty,
                        ContractFilePath = info.UserDetailsInfo?.ContractFilePath ?? string.Empty,
                        BusinessLicenseFilePath = info.UserDetailsInfo?.BusinessLicenseFilePath ?? string.Empty,
                    }
                })?.ToList() ?? new List<UserInfoDto>();
                return JsonResultVo.Success("查询成功", data: userInfoDtos);
            }

            return key ? JsonResultVo.Success("查询成功", data: value) : JsonResultVo.Fail(value.ToString() ?? string.Empty);
        }
    }
}