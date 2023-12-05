using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.License {

    public class LicenseData {

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 序列号
        /// </summary>
        public string Signature { get; set; } = string.Empty;

        /// <summary>
        /// 到期时间
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// 机器码
        /// </summary>
        public string MachineCode { get; set; } = string.Empty;
    }
}