using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {
    [Table("Data_OcrInfo", Schema = "dbo")]
    public class OcrInfoModel : BaseBarCodeForeignKeyInfo {

        /// <summary>
        /// 原始内容
        /// </summary>
        public string OriginalContent { get; set; } = string.Empty;

        /// <summary>
        /// 接口名称
        /// </summary>
        public string OcrInterfaceName { get; set; } = string.Empty;

        /// <summary>
        /// 解析后名称
        /// </summary>
        public string ParsedContent { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 是否使用Ocr
        /// </summary>
        public bool IsUseOcr { get; set; }
    }
}