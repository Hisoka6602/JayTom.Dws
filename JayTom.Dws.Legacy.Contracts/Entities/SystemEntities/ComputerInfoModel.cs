using System.ComponentModel;

namespace JayTom.Dws.Legacy.Contracts.Entities.SystemEntities {

    public class ComputerInfoModel {

        /// <summary>
        /// 磁盘列表
        /// </summary>
        public List<HardDiskInfoModel>? HardDiskList { get; set; }

        /// <summary>
        /// 内存信息
        /// </summary>
        public MemoryInfoModel? MemoryInfo { get; set; }

        /// <summary>
        /// Cpu信息
        /// </summary>
        public CpuInfoModel? CpuInfo { get; set; }

        /// <summary>
        /// Gpu信息
        /// </summary>
        public GpuInfoModel? GpuInfo { get; set; }

        /// <summary>
        /// 网络信息类
        /// </summary>
        public NetworkInfoModel? NetworkInfo { get; set; }

        /// <summary>
        /// 本地连接信息类
        /// </summary>
        public List<LocalNetworkConnectionInfoModel>? LocalNetworkConnectionInfos { get; set; }

        /// <summary>
        /// 上次关机时间
        /// </summary>
        public DateTime LastShutdownTime { get; set; }

        /// <summary>
        /// 本次开机运行时长
        /// </summary>
        public TimeSpan UpTime { get; set; }

        /// <summary>
        /// 计算机用户名称
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// 计算机名称
        /// </summary>
        public string? ComputerName { get; set; }

        /// <summary>
        /// 上次关机是否为意外关机
        /// </summary>
        public bool IsUnexpectedShutdown { get; set; }

        /// <summary>
        /// 意外关机原因
        /// </summary>
        public string? UnexpectedShutdownReason { get; set; }

        /// <summary>
        /// 系统信息
        /// </summary>
        public string? SystemInfoString { get; set; }

        /// <summary>
        /// 占用 CPU 进程排行
        /// </summary>
        public List<string>? CpuUsageProcesses { get; set; }

        /// <summary>
        /// 占用内存进程排行
        /// </summary>
        public List<string>? MemoryUsageProcesses { get; set; }
    }

    /// <summary>
    /// 硬盘信息类
    /// </summary>
    public class HardDiskInfoModel {

        /// <summary>
        /// 磁盘名称
        /// </summary>
        public string DiskName { get; set; } = string.Empty;

        /// <summary>
        /// 磁盘剩余空间
        /// </summary>
        public decimal FreeSpacePercentage { get; set; }

        /// <summary>
        /// 磁盘剩余字节
        /// </summary>
        public long FreeSpaceBytes { get; set; }

        /// <summary>
        /// 已使用空间
        /// </summary>
        public decimal UsedSpacePercentage { get; set; }

        /// <summary>
        /// 剩余空间
        /// </summary>
        public long UsedSpaceBytes { get; set; }

        /// <summary>
        /// 是否系统盘
        /// </summary>
        public bool IsSystemDisk { get; set; }

        /// <summary>
        /// 磁盘类型
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// 写入速度
        /// </summary>
        public decimal WriteSpeed { get; set; }

        /// <summary>
        /// 读取速度
        /// </summary>
        public decimal ReadSpeed { get; set; }

        /// <summary>
        /// 平均响应时间
        /// </summary>
        public decimal AverageResponseTime { get; set; }
    }

    /// <summary>
    /// 内存信息类
    /// </summary>
    public class MemoryInfoModel {

        /// <summary>
        /// 内存类型
        /// </summary>
        public string? MemoryType { get; set; }

        /// <summary>
        /// 内存总大小
        /// </summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// 可用内存大小
        /// </summary>
        public long AvailableSizeBytes { get; set; }

        /// <summary>
        /// 使用内存百分比
        /// </summary>
        public decimal UsedPercentage { get; set; }

        /// <summary>
        /// 内存剩余比率
        /// </summary>
        public decimal MemoryRemaining { get; set; }
    }

    /// <summary>
    /// Cpu信息类
    /// </summary>
    public class CpuInfoModel {

        /// <summary>
        /// 名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// CPU 制造商
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// CPU 型号
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// CPU 核心数
        /// </summary>
        public int NumberOfCores { get; set; }

        /// <summary>
        /// CPU 时钟速度
        /// </summary>
        public decimal ClockSpeed { get; set; }

        /// <summary>
        ///  CPU 使用率
        /// </summary>
        public decimal UsagePercentage { get; set; }

        /// <summary>
        /// 逻辑处理器数量
        /// </summary>
        public int NumberOfLogicalProcessors { get; set; }

        /// <summary>
        /// 插槽数量
        /// </summary>
        public int SocketCount { get; set; }

        /// <summary>
        /// Cpu温度
        /// </summary>
        public decimal CpuTemperature { get; set; }

        /// <summary>
        /// 风扇转速
        /// </summary>
        public int FanSpeed { get; set; }
    }

    /// <summary>
    /// GPU 信息类
    /// </summary>
    public class GpuInfoModel {

        /// <summary>
        /// 名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// GPU 制造商
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// GPU 型号
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// GPU 内存大小
        /// </summary>
        public decimal MemorySizeGb { get; set; }

        /// <summary>
        ///  GPU 使用率
        /// </summary>
        public decimal UsagePercentage { get; set; }

        /// <summary>
        /// 已使用内存大小
        /// </summary>
        public decimal UsedMemoryGb { get; set; }

        /// <summary>
        /// 已使用内存百分比
        /// </summary>
        public decimal UsedMemoryPercentage { get; set; }
    }

    /// <summary>
    /// 网络信息类
    /// </summary>
    public class NetworkInfoModel {

        /// <summary>
        /// IP 地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// MAC 地址
        /// </summary>
        public string? MacAddress { get; set; }

        /// <summary>
        /// 子网掩码
        /// </summary>
        public string? SubnetMask { get; set; }

        /// <summary>
        /// 默认网关
        /// </summary>
        public string? DefaultGateway { get; set; }

        /// <summary>
        /// IP所在地区
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// 网络上行速度
        /// </summary>
        public decimal UploadSpeed { get; set; }

        /// <summary>
        /// 网络下行速度
        /// </summary>
        public decimal DownloadSpeed { get; set; }

        // 其他网络相关字段
    }

    /// <summary>
    /// 本地连接信息类
    /// </summary>
    public class LocalNetworkConnectionInfoModel {
        public bool IsConnection { get; set; }

        /// <summary>
        /// 连接名称
        /// </summary>
        public string ConnectionName { get; set; } = string.Empty;

        /// <summary>
        /// 上传速率
        /// </summary>
        public long UploadSpeed { get; set; }

        /// <summary>
        /// 下载速率
        /// </summary>
        public long DownloadSpeed { get; set; }

        /// <summary>
        /// 速度
        /// </summary>
        public long Speed { get; set; }

        /// <summary>
        /// 网络类型
        /// </summary>
        public NetworkType Type { get; set; }
    }

    public enum NetworkType {

        /// <summary>
        /// 以太网
        /// </summary>
        [Description("以太网")]
        Ethernet,

        /// <summary>
        /// 蓝牙
        /// </summary>
        [Description("蓝牙")]
        Bluetooth,

        /// <summary>
        /// Wifi
        /// </summary>
        [Description("Wifi")]
        Wifi,

        /// <summary>
        /// 隧道
        /// </summary>
        [Description("隧道")]
        Tunnel,

        /// <summary>
        /// 移动连接
        /// </summary>
        [Description("移动连接")]
        Wman,

        /// <summary>
        /// 未知
        /// </summary>
        [Description("未知")]
        Unknown
    }
}