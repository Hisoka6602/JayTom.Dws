using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.CacheClearSettings {

    public class CacheClearSettingsInfoModel : BindableBase {
        private int _barcodeDataAgoDays;
        private int _scanImageAgoDays;
        private int _panoramaImageAgoDays;
        private int _ftpImageAgoDays;
        private int _logDataAgoDays;

        /// <summary>
        /// 条码数据清理时间（多少天前）
        /// </summary>
        public int BarcodeDataAgoDays {
            get => _barcodeDataAgoDays;
            set => SetProperty(ref _barcodeDataAgoDays, value);
        }

        /// <summary>
        /// 扫码图片清理时间（多少天前）
        /// </summary>
        public int ScanImageAgoDays {
            get => _scanImageAgoDays;
            set => SetProperty(ref _scanImageAgoDays, value);
        }

        /// <summary>
        /// 全景图片清理时间（多少天前）
        /// </summary>
        public int PanoramaImageAgoDays {
            get => _panoramaImageAgoDays;
            set => SetProperty(ref _panoramaImageAgoDays, value);
        }

        /// <summary>
        /// FTP图片清理时间（多少天前）
        /// </summary>
        public int FtpImageAgoDays {
            get => _ftpImageAgoDays;
            set => SetProperty(ref _ftpImageAgoDays, value);
        }

        /// <summary>
        /// 日志数据清理时间（多少天前）
        /// </summary>
        public int LogDataAgoDays {
            get => _logDataAgoDays;
            set => SetProperty(ref _logDataAgoDays, value);
        }
    }
}