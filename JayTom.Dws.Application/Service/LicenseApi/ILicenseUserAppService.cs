using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.VideoApi;

namespace JayTom.Dws.Application.Service.LicenseApi {

    public interface ILicenseUserAppService {

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="phone"></param>
        /// <param name="ipAddress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> Register(
            string userCode,
            string userName,
            string password,
            string phone,
            string ipAddress,
            CancellationToken token);

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="loginCode"></param>
        /// <param name="password"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> Login(
            string loginCode,
            string password,
            CancellationToken token);

        /// <summary>
        /// 修改资料
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="businessLicenseFilePath"></param>
        /// <param name="token"></param>
        /// <param name="userName"></param>
        /// <param name="phone"></param>
        /// <param name="companyName"></param>
        /// <param name="companyAddress"></param>
        /// <param name="contactEmail"></param>
        /// <param name="description"></param>
        /// <param name="contractFilePath"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> UpdateProfile(
            string userCode,
            string? userName,
            string? phone,
            string companyName,
            string companyAddress,
            string contactEmail,
            string description,
            string contractFilePath,
            string businessLicenseFilePath,
            CancellationToken token);

        /// <summary>
        /// 修改密码
        /// </summary>
        public Task<KeyValuePair<bool, object>> ChangePassword(
            string userCode,
            string oldPassWord,
            string newPassWord,
            CancellationToken token);

        /// <summary>
        /// 个人信息
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> Info(
            string userCode,
            CancellationToken token);

        /// <summary>
        /// 冻结用户
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="isFreeze"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> FreezeUser(
            string userCode,
            bool isFreeze,
            CancellationToken token);

        /// <summary>
        /// 设置用户头像
        /// </summary>
        /// <param name="userCode"></param>
        /// <param name="iconUrlPath"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> SetUserIcon(
            string userCode,
            string iconUrlPath,
            CancellationToken token);

        /// <summary>
        /// 获取所有租户信息
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> TenantInfos(CancellationToken token);
    }
}