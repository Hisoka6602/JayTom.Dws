using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Infrastructure.IComputer {

    public interface IComputer {

        /// <summary>
        /// 获取磁盘信息
        /// </summary>
        /// <returns></returns>
        List<DiskInfo> GetDiskInfo();

        public Task<List<DiskInfo>> GetDiskInfoAsync();

        /// <summary>
        /// 获取风扇转速
        /// </summary>
        /// <returns></returns>
        public int GetFanSpeed();

        /// <summary>
        /// 获取风扇转速
        /// </summary>
        /// <returns></returns>
        public Task<int> GetFanSpeedAsync();

        /// <summary>
        /// 获取Cpu信息
        /// </summary>
        /// <returns></returns>
        public CpuInfo GetCpuInfo();

        /// <summary>
        /// 获取Cpu信息(异步)
        /// </summary>
        /// <returns></returns>
        public Task<CpuInfo> GetCpuInfoAsync();

        /// <summary>
        /// 获取网络速度
        /// </summary>
        /// <returns></returns>
        public NetworkInfo GetNetworkInfo();

        /// <summary>
        /// 异步获取网络速度
        /// </summary>
        /// <returns></returns>
        public Task<NetworkInfo> GetNetworkInfoAsync();

        /// <summary>
        /// 获取内存使用率
        /// </summary>
        /// <returns></returns>
        public MemoryInfo GetMemoryInfo();

        /// <summary>
        /// 获取内存使用率
        /// </summary>
        /// <returns></returns>
        public Task<MemoryInfo> GetMemoryInfoAsync();

        /// <summary>
        /// 获取系统上次关机时间
        /// </summary>
        /// <returns></returns>
        public DateTime? GetLastShutdownTime();

        /// <summary>
        /// 上次是否意外关机
        /// </summary>
        /// <returns></returns>
        public bool GetLastShutdownUnexpected();

        /// <summary>
        /// 获取意外关机原因
        /// </summary>
        /// <returns></returns>
        public string GetLastShutdownReason();

        /// <summary>
        /// 获取显卡信息
        /// </summary>
        /// <returns></returns>
        public List<GpuInfo> GetGpuInformation();

        /// <summary>
        /// 获取显卡信息
        /// </summary>
        /// <returns></returns>
        public Task<List<GpuInfo>?> GetGpuInformationAsync();

        /// <summary>
        /// 获取系统信息
        /// </summary>
        /// <returns></returns>
        public SystemInfo GetSystemInfo();

        /// <summary>
        /// 获取网络信息组
        /// </summary>
        /// <returns></returns>
        public Task<List<LocalNetworkConnectionInfo>?> GetLocalNetworkConnectionInfosAsync();
    }

    public class CpuInfo {

        /// <summary>
        /// Cpu总温度(1位小数)
        /// </summary>
        public float CpuPackageTemperature { get; set; }

        /// <summary>
        /// Cpu名称
        /// </summary>
        public string CpuName { get; set; } = string.Empty;

        /// <summary>
        /// Cpu占用百分比
        /// </summary>
        public float CpuTotalUsedPercent { get; set; }

        /// <summary>
        /// Cpu总线速率
        /// </summary>
        public float CpuBusSpeed { get; set; }

        /// <summary>
        /// Cpu核信息
        /// </summary>
        public List<CpuCoreInfo> CpuCoreInfos { get; set; } = new();
    }

    /// <summary>
    /// Cpu核信息
    /// </summary>
    public class CpuCoreInfo {

        /// <summary>
        /// Cpu温度(1位小数)
        /// </summary>
        public float CpuTemperature { get; set; }

        /// <summary>
        /// 核名称
        /// </summary>
        public string CpuCoreName { get; set; } = string.Empty;

        /// <summary>
        /// Cpu占用百分比
        /// </summary>
        public float CpuUsedPercent { get; set; }

        /// <summary>
        /// 速率
        /// </summary>
        public float CpuCoreSpeed { get; set; }

        /// <summary>
        /// 电压
        /// </summary>
        public float Voltage { get; set; }
    }

    /// <summary>
    /// 内存信息
    /// </summary>
    public class MemoryInfo {

        /// <summary>
        /// 剩余内存百分比
        /// </summary>
        public float AvailableMemoryPercentage { get; set; }

        /// <summary>
        /// 使用内存百分比
        /// </summary>
        public float UsedMemoryPercent { get; set; }

        /// <summary>
        /// 使用内存
        /// </summary>
        public long UsedMemory { get; set; }

        /// <summary>
        /// 剩余内存
        /// </summary>
        public long AvailableMemory { get; set; }

        /// <summary>
        /// 使用内存
        /// </summary>
        public string UsedMemoryFormat { get; set; } = "0 KB";

        /// <summary>
        /// 剩余内存
        /// </summary>
        public string AvailableMemoryFormat { get; set; } = "0 KB";
    }

    /// <summary>
    /// 网络信息
    /// </summary>
    public class NetworkInfo {

        /// <summary>
        /// 下载速度
        /// </summary>
        public long NetworkDownloadSpeed { get; set; }

        /// <summary>
        /// 上传速度
        /// </summary>
        public long NetworkUploadSpeed { get; set; }

        /// <summary>
        /// 下载速度(格式化)
        /// </summary>
        public string NetworkDownloadSpeedFormat { get; set; } = "0 KB/s";

        /// <summary>
        /// 上传速度(格式化)
        /// </summary>
        public string NetworkUploadSpeedFormat { get; set; } = "0 KB/s";

        /// <summary>
        /// Ip地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Mac地址
        /// </summary>
        public string MacAddress { get; set; } = string.Empty;
    }

    /// <summary>
    /// 硬盘信息
    /// </summary>
    public class DiskInfo {

        /// <summary>
        /// 磁盘名称/盘符
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 剩余磁盘容量(格式化)
        /// </summary>
        public string AvailableDiskSpaceFormat { get; set; } = "0 KB";

        /// <summary>
        /// 剩余磁盘容量
        /// </summary>
        public long AvailableDiskSpace { get; set; }

        /// <summary>
        /// 剩余磁盘容量百分比
        /// </summary>
        public float AvailableDiskSpacePercentage { get; set; }

        /// <summary>
        /// 已用磁盘空间百分比
        /// </summary>
        public decimal UsedDiskSpacePercentage { get; set; }

        /// <summary>
        /// 已用磁盘空间百分比
        /// </summary>
        public long UsedDiskSpace { get; set; }

        /// <summary>
        /// 已用磁盘空间格式化
        /// </summary>
        public string UsedDiskSpaceFormat { get; set; } = "0 KB";
    }

    /// <summary>
    /// Gpu信息
    /// </summary>
    public class GpuInfo {

        /// <summary>
        /// 名称
        /// </summary>
        public string? Name { get; set; } = string.Empty;

        /// <summary>
        /// 利用率
        /// </summary>
        public int Utilization { get; set; }

        /// <summary>
        /// 内存大小
        /// </summary>
        public long TotalMemory { get; set; }

        /// <summary>
        /// 剩余内存
        /// </summary>
        public long FreeMemory { get; set; }
    }

    /// <summary>
    /// 系统信息类
    /// </summary>
    public class SystemInfo {

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;     // 设备名称

        /// <summary>
        /// 设备Id
        /// </summary>
        public string? DeviceId { get; set; } = string.Empty;       // 设备ID

        /// <summary>
        /// 产品Id
        /// </summary>
        public string ProductId { get; set; } = string.Empty;      // 产品ID

        /// <summary>
        /// 系统类型
        /// </summary>
        public string SystemType { get; set; } = string.Empty;    // 系统类型

        /// <summary>
        /// Windows 版本
        /// </summary>
        public string WindowsVersion { get; set; } = string.Empty; // Windows 版本

        /// <summary>
        /// 安装日期
        /// </summary>
        public DateTime? InstallDate { get; set; }    // 安装日期

        /// <summary>
        /// 操作系统版本
        /// </summary>
        public string OsVersion { get; set; } = string.Empty;    // 操作系统版本
    }

    /// <summary>
    /// 本地连接信息类
    /// </summary>
    public class LocalNetworkConnectionInfo {

        /// <summary>
        /// 连接名称
        /// </summary>
        public string ConnectionName { get; set; } = string.Empty;

        /// <summary>
        /// 上传速率
        /// </summary>
        public double UploadSpeed { get; set; }

        /// <summary>
        /// 下载速率
        /// </summary>
        public double DownloadSpeed { get; set; }

        /// <summary>
        /// 速度
        /// </summary>
        public long Speed { get; set; }
    }
}