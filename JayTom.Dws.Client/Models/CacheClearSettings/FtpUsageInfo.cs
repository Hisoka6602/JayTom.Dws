using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.CacheClearSettings {

    public class FtpUsageInfo : BindableBase {
        private long _usedBytes;
        private double _dataUsagePercentage;
        private double _scanImageUsagePercentage;
        private double _panoramaImageUsagePercentage;
        private double _otherUsagePercentage;
        private double _diskUsagePercentage;

        /// <summary>
        /// 磁盘占用百分比
        /// </summary>
        public double DiskUsagePercentage {
            get => _diskUsagePercentage;
            set => SetProperty(ref _diskUsagePercentage, value);
        }

        /// <summary>
        /// FTP已使用字节数
        /// </summary>
        public long UsedBytes {
            get => _usedBytes;
            set => SetProperty(ref _usedBytes, value);
        }

        /// <summary>
        /// 数据占用百分比
        /// </summary>
        public double DataUsagePercentage {
            get => _dataUsagePercentage;
            set => SetProperty(ref _dataUsagePercentage, value);
        }

        /// <summary>
        /// 扫码图片占用百分比
        /// </summary>
        public double ScanImageUsagePercentage {
            get => _scanImageUsagePercentage;
            set => SetProperty(ref _scanImageUsagePercentage, value);
        }

        /// <summary>
        /// 全景图片占用百分比
        /// </summary>
        public double PanoramaImageUsagePercentage {
            get => _panoramaImageUsagePercentage;
            set => SetProperty(ref _panoramaImageUsagePercentage, value);
        }

        /// <summary>
        /// 非程序占用百分比
        /// </summary>
        public double OtherUsagePercentage {
            get => _otherUsagePercentage;
            set => SetProperty(ref _otherUsagePercentage, value);
        }
    }
}