using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.CacheClearSettings
{
    public class LocalDiskUsageInfo : BindableBase
    {
        /// <summary>
        /// 磁盘占用百分比
        /// </summary>
        public decimal DiskUsagePercentage
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 已使用字节数
        /// </summary>
        public long UsedBytes
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 数据占用百分比
        /// </summary>
        public decimal DataUsagePercentage
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 扫码图片占用百分比
        /// </summary>
        public decimal ScanImageUsagePercentage
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 全景图片占用百分比
        /// </summary>
        public decimal PanoramaImageUsagePercentage
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 日志文件占用比率
        /// </summary>
        public decimal LogFileUsagePercentage
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 非程序占用百分比
        /// </summary>
        public decimal OtherUsagePercentage
        {
            get;
            set => SetProperty(ref field, value);
        }
    }
}
