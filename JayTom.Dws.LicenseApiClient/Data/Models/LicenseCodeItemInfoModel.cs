using System.ComponentModel;
using JayTom.Dws.LicenseApiClient.Plugin.Excel.Attributes;

namespace JayTom.Dws.LicenseApiClient.Data.Models {
    public class LicenseCodeItemInfoModel : BaseItemInfoModel {

        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// 授权码
        /// </summary>
        [DisplayName("授权码"), ExcelInfo(Width = 20000)]
        public string LicenseCode { get; set; } = string.Empty;

        /// <summary>
        /// 客户端上限数量
        /// </summary>
        [DisplayName("客户端上限数量"), ExcelInfo(Width = 6000)]
        public int MaxClientCount { get; set; } = 0;

        /// <summary>
        /// 已激活数量
        /// </summary>
        public int ActivatedClientCount { get; set; } = 0;

        /// <summary>
        /// 到期时间
        /// </summary>
        [DisplayName("到期时间"), ExcelInfo(Width = 6000)]
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// 客户名称/客户信息
        /// </summary>
        [DisplayName("客户名称"), ExcelInfo(Width = 6000)]
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// 用户代码
        /// </summary>
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 分组名称
        /// </summary>
        public string GroupName { get; set; } = string.Empty;
        /// <summary>
        /// 机器码
        /// </summary>
        public List<MachineCodeItemInfoModel> MachineCodeItem { get; set; } = new();
    }
}