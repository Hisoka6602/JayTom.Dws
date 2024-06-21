using System;
using Prism.Mvvm;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models {

    public class ComputerInfoModel : BindableBase {
        private List<HardDiskInfoModel>? _hardDiskList = new();
        private MemoryInfoModel _memoryInfo = new();
        private CpuInfoModel _cpuInfo = new();
        private GpuInfoModel _gpuInfo = new();
        private DateTime _lastShutdownTime;
        private TimeSpan _upTime;
        private string? _userName;
        private string? _computerName;
        private bool _isUnexpectedShutdown;
        private string? _unexpectedShutdownReason;
        private NetworkInfoModel _networkInfo = new();
        private string _systemInfoString = string.Empty;
        private List<LocalNetworkConnectionInfoModel> _localNetworkConnectionInfos = new();

        /// <summary>
        /// 磁盘列表
        /// </summary>
        public List<HardDiskInfoModel>? HardDiskList {
            get => _hardDiskList;
            set => SetProperty(ref _hardDiskList, value);
        }

        /// <summary>
        /// 内存信息
        /// </summary>
        public MemoryInfoModel MemoryInfo {
            get => _memoryInfo;
            set => SetProperty(ref _memoryInfo, value);
        }

        /// <summary>
        /// Cpu信息
        /// </summary>
        public CpuInfoModel CpuInfo {
            get => _cpuInfo;
            set => SetProperty(ref _cpuInfo, value);
        }

        /// <summary>
        /// Gpu信息
        /// </summary>
        public GpuInfoModel GpuInfo {
            get => _gpuInfo;
            set => SetProperty(ref _gpuInfo, value);
        }

        /// <summary>
        /// 网络信息类
        /// </summary>
        public NetworkInfoModel NetworkInfo {
            get => _networkInfo;
            set => SetProperty(ref _networkInfo, value);
        }

        /// <summary>
        /// 本地连接信息类
        /// </summary>
        public List<LocalNetworkConnectionInfoModel> LocalNetworkConnectionInfos {
            get => _localNetworkConnectionInfos;
            set => SetProperty(ref _localNetworkConnectionInfos, value);
        }

        /// <summary>
        /// 上次关机时间
        /// </summary>
        public DateTime LastShutdownTime {
            get => _lastShutdownTime;
            set => SetProperty(ref _lastShutdownTime, value);
        }

        /// <summary>
        /// 本次开机运行时长
        /// </summary>
        public TimeSpan UpTime {
            get => _upTime;
            set => SetProperty(ref _upTime, value);
        }

        /// <summary>
        /// 计算机用户名称
        /// </summary>
        public string? UserName {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        /// <summary>
        /// 计算机名称
        /// </summary>
        public string? ComputerName {
            get => _computerName;
            set => SetProperty(ref _computerName, value);
        }

        /// <summary>
        /// 上次关机是否为意外关机
        /// </summary>
        public bool IsUnexpectedShutdown {
            get => _isUnexpectedShutdown;
            set => SetProperty(ref _isUnexpectedShutdown, value);
        }

        /// <summary>
        /// 意外关机原因
        /// </summary>
        public string? UnexpectedShutdownReason {
            get => _unexpectedShutdownReason;
            set => SetProperty(ref _unexpectedShutdownReason, value);
        }

        /// <summary>
        /// 系统信息
        /// </summary>
        public string SystemInfoString {
            get => _systemInfoString;
            set => SetProperty(ref _systemInfoString, value);
        }
    }

    /// <summary>
    /// 硬盘信息类
    /// </summary>
    public class HardDiskInfoModel : BindableBase {
        private string _diskName = string.Empty;
        private float _freeSpacePercentage;
        private long _freeSpaceBytes;

        /// <summary>
        /// 磁盘名称
        /// </summary>
        public string DiskName {
            get => _diskName;
            set => SetProperty(ref _diskName, value);
        }

        /// <summary>
        /// 磁盘剩余空间
        /// </summary>
        public float FreeSpacePercentage {
            get => _freeSpacePercentage;
            set => SetProperty(ref _freeSpacePercentage, value);
        }

        /// <summary>
        /// 磁盘剩余字节
        /// </summary>
        public long FreeSpaceBytes {
            get => _freeSpaceBytes;
            set => SetProperty(ref _freeSpaceBytes, value);
        }

        /// <summary>
        /// 已使用空间
        /// </summary>
        public float UsedSpacePercentage { get; set; }

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
        public float WriteSpeed { get; set; }

        /// <summary>
        /// 读取速度
        /// </summary>
        public float ReadSpeed { get; set; }

        /// <summary>
        /// 平均响应时间
        /// </summary>
        public float AverageResponseTime { get; set; }
    }

    /// <summary>
    /// 内存信息类
    /// </summary>
    public class MemoryInfoModel : BindableBase {
        private float _memoryRemaining;
        private string? _memoryType;
        private long _totalSizeBytes;
        private long _availableSizeBytes;
        private float _usedPercentage;

        /// <summary>
        /// 内存类型
        /// </summary>
        public string? MemoryType {
            get => _memoryType;
            set => SetProperty(ref _memoryType, value);
        }

        /// <summary>
        /// 内存总大小
        /// </summary>
        public long TotalSizeBytes {
            get => _totalSizeBytes;
            set => SetProperty(ref _totalSizeBytes, value);
        }

        /// <summary>
        /// 可用内存大小
        /// </summary>
        public long AvailableSizeBytes {
            get => _availableSizeBytes;
            set => SetProperty(ref _availableSizeBytes, value);
        }

        /// <summary>
        /// 使用内存百分比
        /// </summary>
        public float UsedPercentage {
            get => _usedPercentage;
            set => SetProperty(ref _usedPercentage, value);
        }

        /// <summary>
        /// 内存剩余比率
        /// </summary>
        public float MemoryRemaining {
            get => _memoryRemaining;
            set => SetProperty(ref _memoryRemaining, value);
        }
    }

    /// <summary>
    /// Cpu信息类
    /// </summary>
    public class CpuInfoModel : BindableBase {
        private string? _name;
        private string? _manufacturer;
        private string? _model;
        private int _numberOfCores;
        private float _clockSpeed;
        private float _usagePercentage;
        private int _numberOfLogicalProcessors;
        private int _socketCount;
        private float _cpuTemperature;
        private int _fanSpeed;

        /// <summary>
        /// 名称
        /// </summary>
        public string? Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// CPU 制造商
        /// </summary>
        public string? Manufacturer {
            get => _manufacturer;
            set => SetProperty(ref _manufacturer, value);
        }

        /// <summary>
        /// CPU 型号
        /// </summary>
        public string? Model {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        /// <summary>
        /// CPU 核心数
        /// </summary>
        public int NumberOfCores {
            get => _numberOfCores;
            set => SetProperty(ref _numberOfCores, value);
        }

        /// <summary>
        /// CPU 时钟速度
        /// </summary>
        public float ClockSpeed {
            get => _clockSpeed;
            set => SetProperty(ref _clockSpeed, value);
        }

        /// <summary>
        ///  CPU 使用率
        /// </summary>
        public float UsagePercentage {
            get => _usagePercentage;
            set => SetProperty(ref _usagePercentage, value);
        }

        /// <summary>
        /// 逻辑处理器数量
        /// </summary>
        public int NumberOfLogicalProcessors {
            get => _numberOfLogicalProcessors;
            set => SetProperty(ref _numberOfLogicalProcessors, value);
        }

        /// <summary>
        /// 插槽数量
        /// </summary>
        public int SocketCount {
            get => _socketCount;
            set => SetProperty(ref _socketCount, value);
        }

        /// <summary>
        /// Cpu温度
        /// </summary>
        public float CpuTemperature {
            get => _cpuTemperature;
            set => SetProperty(ref _cpuTemperature, value);
        }

        /// <summary>
        /// 风扇转速
        /// </summary>
        public int FanSpeed {
            get => _fanSpeed;
            set => SetProperty(ref _fanSpeed, value);
        }
    }

    /// <summary>
    /// GPU 信息类
    /// </summary>
    public class GpuInfoModel : BindableBase {
        private string? _name;
        private string? _manufacturer;
        private string? _model;
        private float _memorySizeGb;
        private float _usagePercentage;
        private float _usedMemoryGb;
        private float _usedMemoryPercentage;

        /// <summary>
        /// 名称
        /// </summary>
        public string? Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// GPU 制造商
        /// </summary>
        public string? Manufacturer {
            get => _manufacturer;
            set => SetProperty(ref _manufacturer, value);
        }

        /// <summary>
        /// GPU 型号
        /// </summary>
        public string? Model {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        /// <summary>
        /// GPU 内存大小
        /// </summary>
        public float MemorySizeGb {
            get => _memorySizeGb;
            set => SetProperty(ref _memorySizeGb, value);
        }

        /// <summary>
        ///  GPU 使用率
        /// </summary>
        public float UsagePercentage {
            get => _usagePercentage;
            set => SetProperty(ref _usagePercentage, value);
        }

        /// <summary>
        /// 已使用内存大小
        /// </summary>
        public float UsedMemoryGb {
            get => _usedMemoryGb;
            set => SetProperty(ref _usedMemoryGb, value);
        }

        /// <summary>
        /// 已使用内存百分比
        /// </summary>
        public float UsedMemoryPercentage {
            get => _usedMemoryPercentage;
            set => SetProperty(ref _usedMemoryPercentage, value);
        }
    }

    /// <summary>
    /// 网络信息类
    /// </summary>
    public class NetworkInfoModel : BindableBase {
        private string? _ipAddress;
        private string? _macAddress;

        /// <summary>
        /// IP 地址
        /// </summary>
        public string? IpAddress {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        /// <summary>
        /// MAC 地址
        /// </summary>
        public string? MacAddress {
            get => _macAddress;
            set => SetProperty(ref _macAddress, value);
        }

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
        public float UploadSpeed { get; set; }

        /// <summary>
        /// 网络下行速度
        /// </summary>
        public float DownloadSpeed { get; set; }

        // 其他网络相关字段
    }

    /// <summary>
    /// 本地连接信息类
    /// </summary>
    public class LocalNetworkConnectionInfoModel : BindableBase {
        private string _connectionName = string.Empty;
        private long _uploadSpeed;
        private long _downloadSpeed;
        private long _speed;
        private bool _isConnection;
        private NetworkType _type = NetworkType.Unknown;

        public bool IsConnection {
            get => _isConnection;
            set => SetProperty(ref _isConnection, value);
        }

        /// <summary>
        /// 连接名称
        /// </summary>
        public string ConnectionName {
            get => _connectionName;
            set => SetProperty(ref _connectionName, value);
        }

        /// <summary>
        /// 上传速率
        /// </summary>
        public long UploadSpeed {
            get => _uploadSpeed;
            set => SetProperty(ref _uploadSpeed, value);
        }

        /// <summary>
        /// 下载速率
        /// </summary>
        public long DownloadSpeed {
            get => _downloadSpeed;
            set => SetProperty(ref _downloadSpeed, value);
        }

        /// <summary>
        /// 速度
        /// </summary>
        public long Speed {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        /// <summary>
        /// 网络类型
        /// </summary>
        public NetworkType Type {
            get => _type;
            set => SetProperty(ref _type, value);
        }
    }

    public enum NetworkType {

        /// <summary>
        /// 以太网
        /// </summary>
        Ethernet,

        /// <summary>
        /// 蓝牙
        /// </summary>
        Bluetooth,

        /// <summary>
        /// Wifi
        /// </summary>
        Wifi,

        /// <summary>
        /// 隧道
        /// </summary>
        Tunnel,

        /// <summary>
        /// 移动连接
        /// </summary>
        Wman,

        /// <summary>
        /// 未知
        /// </summary>
        Unknown
    }
}