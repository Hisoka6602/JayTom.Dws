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
        /// 到期时间
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// 机器码
        /// </summary>
        public string MachineCode { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 授权码
        /// </summary>
        public string LicenseCode { get; set; } = string.Empty;

        /// <summary>
        /// 授权码是否可用
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 扫码器上限
        /// </summary>
        public int MaxBindingScannerCount { get; set; } = 1;

        /// <summary>
        /// 应用模板名称
        /// </summary>
        public string AppliedTemplateName { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }
}