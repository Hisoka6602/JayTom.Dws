using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.CacheClearSettings
{

    public class CacheClearSettingsInfoModel : BindableBase
    {
        /// <summary>
        /// 条码数据清理时间（多少天前）
        /// </summary>
        public int BarcodeDataAgoDays
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 扫码图片清理时间（多少天前）
        /// </summary>
        public int ScanImageAgoDays
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 全景图片清理时间（多少天前）
        /// </summary>
        public int PanoramaImageAgoDays
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// FTP图片清理时间（多少天前）
        /// </summary>
        public int FtpImageAgoDays
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 日志数据清理时间（多少天前）
        /// </summary>
        public int LogDataAgoDays
        {
            get;
            set => SetProperty(ref field, value);
        }
    }
}
