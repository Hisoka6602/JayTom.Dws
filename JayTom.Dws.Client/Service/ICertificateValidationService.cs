using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace JayTom.Dws.Client.Service {

    public interface ICertificateValidationService {

        /// <summary>
        /// 时间验证
        /// </summary>
        /// <returns></returns>
        Task<bool> ValidateTime();

        /// <summary>
        /// 证书验证
        /// </summary>
        /// <returns></returns>
        Task<bool> ValidateCertificate();

        /// <summary>
        /// 机器码效验
        /// </summary>
        /// <returns></returns>
        Task<bool> ValidateMachineCode();
    }
}