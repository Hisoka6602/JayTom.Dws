using System.ComponentModel;

namespace JayTom.Dws.CloudApiClient.Data.Models {

    public class DetailInfoItemModel {
        public int Num { get; set; }

        /// <summary>
        /// 包裹时间戳Id
        /// </summary>
        public long PackageTimestamped { get; set; }

        /// <summary>
        /// 包裹创建时间
        /// </summary>
        public DateTime PackageCreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 其他项
        /// </summary>
        public string? Other { get; set; }

        /// <summary>
        /// 条码信息
        /// </summary>
        [DisplayName("包裹创建时间")]
        public BarCodeInfoModel BarCodeInfo { get; set; } = new();

        /// <summary>
        /// 称重信息
        /// </summary>
        public WeightInfoModel WeightInfo { get; set; } = new();

        /// <summary>
        /// 体积信息
        /// </summary>
        public VolumeInfoModel VolumeInfo { get; set; } = new();

        /// <summary>
        /// 上传信息
        /// </summary>
        public UploadInfoModel UploadInfo { get; set; } = new();

        /// <summary>
        /// 格口信息
        /// </summary>
        public ExitInfoModel ExitInfo { get; set; } = new();

        /// <summary>
        /// 分拣信息
        /// </summary>
        public SortingInfoModel SortingInfo { get; set; } = new();

        /// <summary>
        /// 物流信息
        /// </summary>
        public LogisticsInfoModel LogisticsInfo { get; set; } = new();

        /// <summary>
        /// Ocr信息
        /// </summary>
        public OcrInfoModel OcrInfo { get; set; } = new();

        /// <summary>
        /// 设备信息
        /// </summary>
        public DeviceInfoModel DeviceInfos { get; set; } = new();

        /// <summary>
        /// 图片信息
        /// </summary>
        public List<ImageInfoModel> ImageInfos { get; set; } = new();

        /*/// <summary>
        /// 图片信息
        /// </summary>
        public List<ImageInfoModel> PanoramaImageInfos { get; set; } = new();*/
    }

    /// <summary>
    /// 条码信息
    /// </summary>
    public class BarCodeInfoModel {

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 来源
        /// </summary>
        public int Source { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// 称重信息
    /// </summary>
    public class WeightInfoModel {

        /// <summary>
        /// 来源类型
        /// </summary>
        public int SourceType { get; set; }

        /// <summary>
        /// 源字符
        /// </summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// 格式化后重量
        /// </summary>
        public float FormattedWeight { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 称重模式
        /// </summary>
        public int WeighingMode { get; set; }
    }

    /// <summary>
    /// 体积信息
    /// </summary>
    public class VolumeInfoModel {

        /// <summary>
        /// 来源类型
        /// </summary>
        public int SourceType { get; set; }

        /// <summary>
        /// 源字符
        /// </summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// 格式化后的长
        /// </summary>
        public float FormattedLength { get; set; }

        /// <summary>
        /// 格式化后的宽
        /// </summary>
        public float FormattedWidth { get; set; }

        /// <summary>
        /// 格式化后的高
        /// </summary>
        public float FormattedHeight { get; set; }

        /// <summary>
        /// 格式化的体积
        /// </summary>
        public float FormattedVolume { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 上传信息
    /// </summary>
    public class UploadInfoModel {

        /// <summary>
        /// 上传状态(1成功、2失败、0未上传)
        /// </summary>
        public int RequestStatus { get; set; }

        /// <summary>
        /// 上传内容
        /// </summary>
        public string RequestContent { get; set; } = string.Empty;

        /// <summary>
        /// 响应内容
        /// </summary>
        public string ResponseContent { get; set; } = string.Empty;

        /// <summary>
        /// 请求时间
        /// </summary>
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// 响应时间
        /// </summary>
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 耗时(秒)
        /// </summary>
        public float DurationInSeconds { get; set; }

        /// <summary>
        /// 接口参数
        /// </summary>
        public string InterfaceParameters { get; set; } = string.Empty;

        /// <summary>
        /// 请求地址
        /// </summary>
        public string RequestUrl { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息
        /// </summary>
        public string ExceptionMessage { get; set; } = string.Empty;

        /// <summary>
        /// Api异常类型
        /// </summary>
        public int ApiExceptionType { get; set; }
    }

    /// <summary>
    /// 格口信息
    /// </summary>
    public class ExitInfoModel {

        /// <summary>
        /// 理论格口
        /// </summary>
        public string TheoreticalExit { get; set; } = string.Empty;

        /// <summary>
        /// 物理格口
        /// </summary>
        public string PhysicalExit { get; set; } = string.Empty;

        /// <summary>
        /// 物理格口Id
        /// </summary>
        public long PhysicalExitId { get; set; }
    }

    /// <summary>
    /// 分拣信息
    /// </summary>
    public class SortingInfoModel {

        /// <summary>
        /// 是否使用分拣
        /// </summary>
        public bool IsSortingUsed { get; set; }

        /// <summary>
        /// 分拣模式
        /// </summary>
        public int SortingMode { get; set; }

        /// <summary>
        /// 发送的指令
        /// </summary>
        public string SentInstruction { get; set; } = string.Empty;

        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTime SendTime { get; set; }

        /// <summary>
        /// 接收的指令
        /// </summary>
        public string ReceivedInstruction { get; set; } = string.Empty;

        /// <summary>
        /// 接收时间
        /// </summary>
        public DateTime ReceivedTime { get; set; }

        /// <summary>
        /// 创建包裹指令
        /// </summary>
        public string PackageCreationInstruction { get; set; } = string.Empty;

        /// <summary>
        /// 是否有下位机创建
        /// </summary>
        public bool IsCreatedByLowerMachine { get; set; }

        /// <summary>
        /// 指令目标
        /// </summary>
        public string CommandTarget { get; set; } = string.Empty;

        /// <summary>
        /// 通讯方式
        /// </summary>
        public int CommunicationMethod { get; set; }

        /// <summary>
        /// 效验协议名称
        /// </summary>
        public string ChecksumProtocolName { get; set; } = string.Empty;

        /// <summary>
        /// 连接名称
        /// </summary>
        public string ConnectionName { get; set; } = string.Empty;

        /// <summary>
        /// 是否异常分拣
        /// </summary>

        public bool IsAbnormalSorting { get; set; }
    }

    /// <summary>
    /// 物流信息
    /// </summary>
    public class LogisticsInfoModel {

        /// <summary>
        /// 物流代码
        /// </summary>
        public string LogisticsCode { get; set; } = string.Empty;

        /// <summary>
        /// 物流名称
        /// </summary>
        public string LogisticsName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ocr信息
    /// </summary>
    public class OcrInfoModel {

        /// <summary>
        /// 原始内容
        /// </summary>
        public string OriginalContent { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用Ocr
        /// </summary>
        public bool IsUseOcr { get; set; }

        /// <summary>
        /// 三段码
        /// </summary>
        public string ThreeSegmentCode { get; set; } = string.Empty;

        /// <summary>
        /// 识别耗时
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// 识别时间
        /// </summary>
        public DateTime RecognizeTime { get; set; }

        /// <summary>
        /// 虚拟号码后四位。
        /// </summary>
        public string VirtualNumberLast4 { get; set; } = string.Empty;

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 提交图时间
        /// </summary>
        public long SubmitTimestamp { get; set; }

        /// <summary>
        /// Ocr详细信息
        /// </summary>
        public List<OcrDetailedInfoModel>? OcrDetailedInfos { get; set; }
    }

    /// <summary>
    /// 图片信息
    /// </summary>
    public class ImageInfoModel {

        /// <summary>
        /// 相机名称
        /// </summary>
        public string CameraName { get; set; } = string.Empty;

        /// <summary>
        /// 相机自定义名
        /// </summary>
        public string CustomCameraName { get; set; } = string.Empty;

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 图片类型(0=扫码、1=全景、2=体积云点、3=面单抠图)
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 图片网络路径
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ocr详细信息
    /// </summary>
    public class OcrDetailedInfoModel {

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 电话
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 信息类型(收件人信息、发件人信息)
        /// </summary>
        public int InformationType { get; set; }
    }

    /// <summary>
    /// 设备信息
    /// </summary>
    public class DeviceInfoModel {

        /// <summary>
        /// 机器码
        /// </summary>
        public string MachineCode { get; set; } = string.Empty;

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName { get; set; } = string.Empty;
    }
}