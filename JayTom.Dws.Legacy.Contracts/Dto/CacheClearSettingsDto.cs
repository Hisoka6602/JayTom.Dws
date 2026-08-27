using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto {

    public class CacheClearSettingsDto {

        /// <summary>
        /// 条码数据清理时间（多少天前）
        /// </summary>
        public int BarcodeDataAgoDays { get; set; }

        /// <summary>
        /// 扫码图片清理时间（多少天前）
        /// </summary>
        public int ScanImageAgoDays { get; set; }

        /// <summary>
        /// 全景图片清理时间（多少天前）
        /// </summary>
        public int PanoramaImageAgoDays { get; set; }

        /// <summary>
        /// FTP图片清理时间（多少天前）
        /// </summary>
        public int FtpImageAgoDays { get; set; }

        /// <summary>
        /// 日志数据清理时间（多少天前）
        /// </summary>
        public int LogDataAgoDays { get; set; }

        /// <summary>
        /// 最小空间保留（以MB为单位）
        /// </summary>
        public long MinimumSpaceRetention { get; set; }
    }
}