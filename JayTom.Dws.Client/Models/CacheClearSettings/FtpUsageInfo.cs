using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.CacheClearSettings
{

    public class FtpUsageInfo : BindableBase
    {
        /// <summary>
        /// 磁盘占用百分比
        /// </summary>
        public double DiskUsagePercentage
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// FTP已使用字节数
        /// </summary>
        public long UsedBytes
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 数据占用百分比
        /// </summary>
        public double DataUsagePercentage
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 扫码图片占用百分比
        /// </summary>
        public double ScanImageUsagePercentage
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 全景图片占用百分比
        /// </summary>
        public double PanoramaImageUsagePercentage
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 非程序占用百分比
        /// </summary>
        public double OtherUsagePercentage
        {
            get;
            set => SetProperty(ref field, value);
        }
    }
}
