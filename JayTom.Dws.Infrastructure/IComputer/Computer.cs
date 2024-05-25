using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Management;
using System.Diagnostics;
using Microsoft.VisualBasic;
using System.Threading.Tasks;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using System.Security.Cryptography;
using LibreHardwareMonitor.Hardware;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Diagnostics.Eventing.Reader;

namespace JayTom.Dws.Infrastructure.IComputer {

    public class Computer : IComputer {
        private static LibreHardwareMonitor.Hardware.Computer? _computer = null;

        public Computer() {
            if (_computer is null) {
                _computer = new LibreHardwareMonitor.Hardware.Computer {
                    IsMotherboardEnabled = true,
                    IsCpuEnabled = true,
                    IsStorageEnabled = true,
                    IsBatteryEnabled = true,
                    IsControllerEnabled = true,
                    IsNetworkEnabled = true,
                    IsPsuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                };
                _computer.Open();
            }
            else {
                _computer.Reset();
            }
        }

        public List<DiskInfo> GetDiskInfo() {
            var diskInfoList = new List<DiskInfo>();
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            try {
                diskInfoList = DriveInfo.GetDrives()
                    .Where(drive => drive is { IsReady: true, DriveType: DriveType.Fixed })
                    .Select(drive => {
                        var availableSpace = drive.AvailableFreeSpace;
                        var usedSpace = drive.TotalSize - drive.AvailableFreeSpace;
                        var availableSpaceIndex = (int)Math.Floor(Math.Log(availableSpace, 1024));
                        var usedSpaceIndex = (int)Math.Floor(Math.Log(usedSpace, 1024));
                        return new DiskInfo {
                            Name = drive.Name?.Replace(":", string.Empty)?.Replace("\\", string.Empty) ?? string.Empty,
                            AvailableDiskSpace = drive.AvailableFreeSpace,
                            AvailableDiskSpaceFormat = $"{availableSpace / Math.Pow(1024, availableSpaceIndex):0.##} {sizes[availableSpaceIndex]}",
                            AvailableDiskSpacePercentage = (float)drive.AvailableFreeSpace / drive.TotalSize * 100,
                            UsedDiskSpacePercentage = (decimal)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100,
                            UsedDiskSpace = drive.TotalSize - drive.AvailableFreeSpace,
                            UsedDiskSpaceFormat = $"{usedSpace / Math.Pow(1024, usedSpaceIndex):0.##} {sizes[usedSpaceIndex]}"
                        };
                    }).ToList();
            }
            catch (Exception ex) {
                // 处理异常，例如记录日志或向用户显示错误消息
                Console.WriteLine("获取磁盘信息时出现异常：" + ex.Message);
            }

            return diskInfoList;
        }

        public async Task<List<DiskInfo>> GetDiskInfoAsync() {
            var diskInfoList = new List<DiskInfo>();
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };

            try {
                await Task.Delay(0);

                diskInfoList = DriveInfo.GetDrives()
                    .Where(drive => drive is { IsReady: true, DriveType: DriveType.Fixed })
                    .Select(drive => {
                        var availableSpace = drive.AvailableFreeSpace;
                        var usedSpace = drive.TotalSize - drive.AvailableFreeSpace;
                        var availableSpaceIndex = (int)Math.Floor(Math.Log(availableSpace, 1024));
                        var usedSpaceIndex = (int)Math.Floor(Math.Log(usedSpace, 1024));
                        return new DiskInfo {
                            Name = drive.Name?.Replace(":", string.Empty)?.Replace("\\", string.Empty) ?? string.Empty,
                            AvailableDiskSpace = drive.AvailableFreeSpace,
                            AvailableDiskSpaceFormat = $"{availableSpace / Math.Pow(1024, availableSpaceIndex):0.##} {sizes[availableSpaceIndex]}",
                            AvailableDiskSpacePercentage = (float)drive.AvailableFreeSpace / drive.TotalSize * 100,
                            UsedDiskSpacePercentage = (decimal)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100,
                            UsedDiskSpace = drive.TotalSize - drive.AvailableFreeSpace,
                            UsedDiskSpaceFormat = $"{usedSpace / Math.Pow(1024, usedSpaceIndex):0.##} {sizes[usedSpaceIndex]}"
                        };
                    }).ToList();
            }
            catch (Exception ex) {
                // 处理异常，例如记录日志或向用户显示错误消息
                NLog.LogManager.GetCurrentClassLogger().Error($"获取磁盘信息时出现异常:{ex}");
            }

            return diskInfoList;
        }

        public int GetFanSpeed() {
            try {
                var orDefault = _computer?.Hardware?.FirstOrDefault(f => f.HardwareType == HardwareType.Motherboard);
                orDefault?.Update();
                orDefault?.SubHardware?.FirstOrDefault(f => f.HardwareType == HardwareType.SuperIO)?.Update();
                var fanSensor = orDefault
                    ?.Sensors
                    ?.FirstOrDefault(s => s.SensorType == SensorType.Fan && s.Value.HasValue) ??
                                orDefault?.SubHardware?.FirstOrDefault(f => f.HardwareType == HardwareType.SuperIO)
                    ?.Sensors?.FirstOrDefault(f => f.SensorType == SensorType.Fan && f.Value.HasValue);

                return (int)(fanSensor?.Value ?? 0);
            }
            catch (Exception e) {
                return 0;
            }
        }

        public Task<int> GetFanSpeedAsync() {
            return Task.Run(() => {
                try {
                    var orDefault = _computer?.Hardware?.FirstOrDefault(f => f.HardwareType == HardwareType.Motherboard);
                    orDefault?.Update();
                    orDefault?.SubHardware?.FirstOrDefault(f => f.HardwareType == HardwareType.SuperIO)?.Update();
                    var fanSensor = orDefault
                        ?.Sensors
                        ?.FirstOrDefault(s => s.SensorType == SensorType.Fan && s.Value is > 0) ?? orDefault?.SubHardware?.FirstOrDefault(f => f.HardwareType == HardwareType.SuperIO)
                        ?.Sensors?.FirstOrDefault(f => f.SensorType == SensorType.Fan && f.Value is > 0);

                    /*if (_computer?.Hardware is not null) {
                        foreach (var hardware in _computer.Hardware) {
                            File.AppendAllLines($"{AppDomain.CurrentDomain.BaseDirectory}a.txt",
                                new[] { $"Hardware: {hardware.Name}--HardwareType:{hardware.HardwareType}" });

                            foreach (var subhardware in hardware.SubHardware) {
                                File.AppendAllLines($"{AppDomain.CurrentDomain.BaseDirectory}a.txt",
                                    new[] { $"\tSubhardware: {subhardware.Name}--HardwareType:{subhardware.HardwareType}" });

                                foreach (var sensor in subhardware.Sensors) {
                                    File.AppendAllLines($"{AppDomain.CurrentDomain.BaseDirectory}a.txt",
                                        new[] { $"\t\tSensor: {sensor.Name}, value: {sensor.Value}--SensorType:{sensor.SensorType}" });
                                }
                            }

                            foreach (var sensor in hardware.Sensors) {
                                File.AppendAllLines($"{AppDomain.CurrentDomain.BaseDirectory}a.txt",
                                    new[] { $"\tSensor: {sensor.Name}, value: {sensor.Value}--SensorType:{sensor.SensorType}" });
                            }
                        }

                        /*foreach (var hardware in _computer.Hardware) {
                            var hardwareName = hardware.Name;
                            var hardwareType = hardware.HardwareType;
                            foreach (var hardware1 in hardware.SubHardware) {
                                var hardware1Name = hardware1.Name;
                                var hardware1HardwareType = hardware1.HardwareType;
                                File.AppendAllLines($"{AppDomain.CurrentDomain.BaseDirectory}a.txt",
                                    new[]
                                    {
                                        $"---------SubHardware----------",
                                        $"hardware1Name:{hardware1Name}",
                                        $"hardware1HardwareType:{hardware1HardwareType}",
                                        $"hardware1.SubHardware.Length:{hardware1}",
                                    });
                                foreach (var hardware1Sensor in hardware1.Sensors) {
                                    var sensorName = hardware1Sensor.Name;
                                    var sensorType = hardware1Sensor.SensorType;
                                    var sensorValue = hardware1Sensor.Value;
                                    hardware1Sensor.Control.
                                    File.AppendAllLines($"{AppDomain.CurrentDomain.BaseDirectory}a.txt",
                                        new[]
                                        {
                                            $"sensorName:{sensorName}",
                                            $"sensorType:{sensorType}",
                                            $"sensorValue:{sensorValue}",
                                        });
                                }

                                File.AppendAllLines($"{AppDomain.CurrentDomain.BaseDirectory}a.txt",
                                    new[]
                                    {
                                        $"--------------------------------",
                                    });
                            }

                            foreach (var hardwareSensor in hardware.Sensors) {
                                var sensorName = hardwareSensor.Name;
                                var sensorType = hardwareSensor.SensorType;
                                var sensorValue = hardwareSensor.Value;

                                File.AppendAllLines($"{AppDomain.CurrentDomain.BaseDirectory}a.txt",
                                    new[]
                                    {
                                        $"hardwareName:{hardwareName}",
                                        $"hardwareType:{hardwareType}",
                                        $"sensorName:{sensorName}",
                                        $"sensorType:{sensorType}",
                                        $"sensorValue:{sensorValue}",
                                    });
                            }
                        }#1#
                    }
                    File.AppendAllLines($"{AppDomain.CurrentDomain.BaseDirectory}a.txt",
                        new[]
                        {
                            $"-----------------------------"
                        });*/
                    return (int)(fanSensor?.Value ?? 0);
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }

                return 0;
            });
        }

        public CpuInfo GetCpuInfo() {
            try {
                var hardware = _computer?.Hardware?.FirstOrDefault(f => f.HardwareType == HardwareType.Cpu);
                if (hardware is not null) {
                    hardware.Update();
                    return new CpuInfo() {
                        CpuPackageTemperature = hardware.Sensors?.FirstOrDefault(f => f.Name.Equals("CPU Package"))
                             ?.Value.GetValueOrDefault() ?? 0,
                        CpuTotalUsedPercent = hardware.Sensors?.FirstOrDefault(f => f.Name.Equals("CPU Total"))?.Value
                             .GetValueOrDefault() ?? 0,
                        CpuBusSpeed = hardware.Sensors?.FirstOrDefault(f => f.Name.Equals("Bus Speed"))?.Value
                             .GetValueOrDefault() ?? 0,
                        CpuName = $"{hardware.Name}",
                        CpuCoreInfos = hardware.Sensors?.Where(w => w.Name.StartsWith("CPU Core ") && w.SensorType == SensorType.Clock).GroupBy(g => g.Name)
                            .Select(s => new CpuCoreInfo {
                                CpuCoreName = hardware.Sensors.FirstOrDefault(f => f.Name.Equals(s.Key))?.Name ?? string.Empty,
                                CpuCoreSpeed = hardware.Sensors.FirstOrDefault(f =>
                                    f.Name.Equals(s.Key) && f.SensorType == SensorType.Clock)?.Value ?? 0,
                                CpuTemperature = hardware.Sensors.FirstOrDefault(f =>
                                    f.Name.Equals(s.Key) && f.SensorType == SensorType.Temperature)?.Value ?? 0,
                                CpuUsedPercent = hardware.Sensors.FirstOrDefault(f =>
                                    f.Name.Contains(s.Key) && f.SensorType == SensorType.Load)?.Value ?? 0,
                                Voltage = hardware.Sensors.FirstOrDefault(f =>
                                    f.Name.Equals(s.Key) && f.SensorType == SensorType.Voltage)?.Value ?? 0,
                            })?.ToList() ?? new List<CpuCoreInfo>()
                    };
                }
            }
            catch (Exception e) {
                // ignored
            }

            return new CpuInfo();
        }

        public async Task<CpuInfo> GetCpuInfoAsync() {
            try {
                await Task.Delay(0);

                var hardware = _computer?.Hardware?.FirstOrDefault(f => f.HardwareType == HardwareType.Cpu);
                if (hardware is not null) {
                    hardware.Update();
                    return new CpuInfo() {
                        CpuPackageTemperature = hardware.Sensors?.FirstOrDefault(f => f.Name.Equals("CPU Package"))
                             ?.Value.GetValueOrDefault() ?? 0,
                        CpuTotalUsedPercent = hardware.Sensors?.FirstOrDefault(f => f.Name.Equals("CPU Total"))?.Value
                             .GetValueOrDefault() ?? 0,
                        CpuBusSpeed = hardware.Sensors?.FirstOrDefault(f => f.Name.Equals("Bus Speed"))?.Value
                             .GetValueOrDefault() ?? 0,
                        CpuName = $"{hardware.Name}",
                        CpuCoreInfos = hardware.Sensors?.Where(w => w.Name.StartsWith("CPU Core ") && w.SensorType == SensorType.Clock).GroupBy(g => g.Name)
                             .Select(s => new CpuCoreInfo {
                                 CpuCoreName = hardware.Sensors.FirstOrDefault(f => f.Name.Equals(s.Key))?.Name ?? string.Empty,
                                 CpuCoreSpeed = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Equals(s.Key) && f.SensorType == SensorType.Clock)?.Value ?? 0,
                                 CpuTemperature = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Equals(s.Key) && f.SensorType == SensorType.Temperature)?.Value ?? 0,
                                 CpuUsedPercent = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Contains(s.Key) && f.SensorType == SensorType.Load)?.Value ?? 0,
                                 Voltage = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Equals(s.Key) && f.SensorType == SensorType.Voltage)?.Value ?? 0,
                             })?.ToList() ?? new List<CpuCoreInfo>()
                    };
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return new CpuInfo();
        }

        public NetworkInfo GetNetworkInfo() {
            try {
                var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(ni =>
                        ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
                if (networkInterface != null) {
                    var statsAtStart = networkInterface.GetIPv4Statistics();
                    Thread.Sleep(1000);
                    var statsAtEnd = networkInterface.GetIPv4Statistics();
                    var downloadSpeed = (statsAtEnd.BytesReceived - statsAtStart.BytesReceived);
                    var uploadSpeed = (statsAtEnd.BytesSent - statsAtStart.BytesSent);
                    string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s", "TB/s" };
                    var uploadSpeedIndex = (int)Math.Floor(Math.Log(uploadSpeed, 1024));
                    var downloadSpeedIndex = (int)Math.Floor(Math.Log(downloadSpeed, 1024));
                    return new NetworkInfo {
                        NetworkDownloadSpeed = downloadSpeed,
                        NetworkUploadSpeed = uploadSpeed,
                        NetworkDownloadSpeedFormat = $"{downloadSpeed / Math.Pow(1024, downloadSpeedIndex):0.##} {sizes[downloadSpeedIndex]}",
                        NetworkUploadSpeedFormat = $"{uploadSpeed / Math.Pow(1024, uploadSpeedIndex):0.##} {sizes[uploadSpeedIndex]}",
                    };
                }
            }
            catch (Exception) {
                // ignored
            }

            return new NetworkInfo();
        }

        public async Task<NetworkInfo> GetNetworkInfoAsync() {
            try {
                var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(ni =>
                        ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
                if (networkInterface != null) {
                    var statsAtStart = networkInterface.GetIPv4Statistics();
                    var startTime = DateTime.Now;
                    await Task.Delay(1000);
                    var statsAtEnd = networkInterface.GetIPv4Statistics();
                    var downloadSpeed = (statsAtEnd.BytesReceived - statsAtStart.BytesReceived);
                    var uploadSpeed = (statsAtEnd.BytesSent - statsAtStart.BytesSent);
                    string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s", "TB/s" };
                    var downloadSpeedIndex = Math.Clamp((int)Math.Floor(Math.Log(downloadSpeed, 1024)), 0, sizes.Length - 1);
                    var uploadSpeedIndex = Math.Clamp((int)Math.Floor(Math.Log(uploadSpeed, 1024)), 0, sizes.Length - 1);
                    return new NetworkInfo {
                        NetworkDownloadSpeed = downloadSpeed,
                        NetworkUploadSpeed = uploadSpeed,
                        NetworkDownloadSpeedFormat = $"{downloadSpeed / Math.Pow(1024, downloadSpeedIndex):0.##} {sizes[downloadSpeedIndex]}",
                        NetworkUploadSpeedFormat = $"{uploadSpeed / Math.Pow(1024, uploadSpeedIndex):0.##} {sizes[uploadSpeedIndex]}",
                    };
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return new NetworkInfo();
        }

        public MemoryInfo GetMemoryInfo() {
            try {
                var managementClass = new ManagementClass("Win32_ComputerSystem");
                var totalMemory = managementClass.GetInstances().Cast<ManagementObject>()
                    .Sum(m => long.TryParse(m["TotalPhysicalMemory"].ToString(), out var result) ? result : 0);
                var process = Process.GetCurrentProcess();
                var usedMemory = process.WorkingSet64;
                var availableMemory = totalMemory - usedMemory;
                var availableMemoryPercent = (float)Math.Round((double)availableMemory / totalMemory * 100, 2);
                var usedMemoryPercent = (float)Math.Round((double)usedMemory / totalMemory * 100, 2);
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                var sizeIndex = (int)Math.Floor(Math.Log(availableMemory, 1024));
                var formattedAvailableMemory = availableMemory / Math.Pow(1024, sizeIndex);
                var availableMemoryFormat = $"{formattedAvailableMemory:0.##} {sizes[sizeIndex]}";

                sizeIndex = (int)Math.Floor(Math.Log(usedMemory, 1024));
                var formattedUsedMemory = usedMemory / Math.Pow(1024, sizeIndex);
                var usedMemoryFormat = $"{formattedUsedMemory:0.##} {sizes[sizeIndex]}";
                return new MemoryInfo() {
                    UsedMemory = usedMemory,
                    AvailableMemory = availableMemory,
                    AvailableMemoryFormat = availableMemoryFormat,
                    AvailableMemoryPercentage = availableMemoryPercent,
                    UsedMemoryFormat = usedMemoryFormat,
                    UsedMemoryPercent = usedMemoryPercent,
                };
            }
            catch {
                // Do nothing and return default MemoryInfo object
            }
            return new MemoryInfo();
        }

        public async Task<MemoryInfo> GetMemoryInfoAsync() {
            try {
                await Task.Yield();
                var managementClass = new ManagementClass("Win32_OperatingSystem");
                var instances = managementClass.GetInstances();
                var managementObject = instances.Cast<ManagementObject>().FirstOrDefault();

                if (managementObject != null) {
                    var totalPhysicalMemory = long.Parse(managementObject["TotalVisibleMemorySize"].ToString() ?? string.Empty);
                    var freePhysicalMemory = long.Parse(managementObject["FreePhysicalMemory"].ToString() ?? string.Empty);
                    var usedMemory = totalPhysicalMemory - freePhysicalMemory;

                    var availableMemoryPercent = (float)Math.Round((double)freePhysicalMemory / totalPhysicalMemory * 100, 2);
                    var usedMemoryPercent = (float)Math.Round((double)usedMemory / totalPhysicalMemory * 100, 2);

                    string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                    var sizeIndex = (int)Math.Floor(Math.Log(freePhysicalMemory, 1024));
                    var formattedAvailableMemory = freePhysicalMemory / Math.Pow(1024, sizeIndex);
                    var availableMemoryFormat = $"{formattedAvailableMemory:0.##} {sizes[sizeIndex]}";

                    sizeIndex = (int)Math.Floor(Math.Log(usedMemory, 1024));
                    var formattedUsedMemory = usedMemory / Math.Pow(1024, sizeIndex);
                    var usedMemoryFormat = $"{formattedUsedMemory:0.##} {sizes[sizeIndex]}";

                    return new MemoryInfo() {
                        UsedMemory = usedMemory,
                        AvailableMemory = freePhysicalMemory,
                        AvailableMemoryFormat = availableMemoryFormat,
                        AvailableMemoryPercentage = availableMemoryPercent,
                        UsedMemoryFormat = usedMemoryFormat,
                        UsedMemoryPercent = usedMemoryPercent,
                    };
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                // Do nothing and return default MemoryInfo object
            }

            return new MemoryInfo();
        }

        public DateTime? GetLastShutdownTime() {
            try {
                var query = new EventLogQuery("System", PathType.LogName, "*[System/EventID=1074 or System/EventID=12]");

                using var reader = new EventLogReader(query);
                var record = reader.ReadEvent();

                return record?.TimeCreated;
            }
            catch (Exception e) {
                return null;
            }
        }

        public bool GetLastShutdownUnexpected() {
            var query = new EventLogQuery("System", PathType.LogName, "*[System/EventID=41]");

            using var reader = new EventLogReader(query);
            var record = reader.ReadEvent();

            return record != null;
        }

        public string GetLastShutdownReason() {
            var query = new EventLogQuery("System", PathType.LogName, "*[System/EventID=1074]");

            using var reader = new EventLogReader(query);
            var record = reader.ReadEvent();
            if (record == null) return "Unknown"; // 未找到关机事件
            var shutdownReason = record.FormatDescription();
            return shutdownReason; // 返回关机原因
        }

        public List<GpuInfo> GetGpuInformation() {
            var gpuInfoList = new List<GpuInfo>();

            var query = new ObjectQuery("SELECT * FROM Win32_VideoController");
            var searcher = new ManagementObjectSearcher(query);
            var gpuList = searcher.Get();

            foreach (var o in gpuList) {
                var gpu = (ManagementObject)o;
                var gpuInfo = new GpuInfo {
                    Name = gpu?["Name"] as string ?? string.Empty,
                    //Utilization = (int)(gpu?["AdapterDACType"] ?? 0),
                    //TotalMemory = (int)(gpu?["AdapterRAM"] ?? 0),
                    //FreeMemory = (int)(gpu?["AdapterRAM"] ?? 0) - (int)(gpu?["AdapterDedicatedMemory"] ?? 0)
                };
                gpuInfoList.Add(gpuInfo);
            }

            return gpuInfoList;
        }

        public Task<List<GpuInfo>?> GetGpuInformationAsync() {
            return Task.Run(() => {
                try {
                    var gpuHardwareList = _computer?.Hardware
                        ?.Where(h => h.HardwareType is HardwareType.GpuIntel or HardwareType.GpuAmd)
                        .ToList();

                    if (gpuHardwareList is { Count: > 0 }) {
                        foreach (var gpu in gpuHardwareList) {
                            gpu.Update();
                        }

                        return (from gpu in gpuHardwareList let utilizationSensor = gpu.Sensors?.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "D3D 3D") let memorySensor = gpu.Sensors?.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "GPU Memory") let spaceSensor = gpu.Sensors?.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "GPU Memory Free") let gpuName = gpu.Name select new GpuInfo { Name = gpuName, Utilization = (int)(utilizationSensor?.Value ?? 0), TotalMemory = (long)(memorySensor?.Max ?? 0), FreeMemory = (long)(spaceSensor?.Value ?? 0) }).ToList();
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }

                return null;
            });
        }

        public SystemInfo GetSystemInfo() {
            var systemInfo = new SystemInfo();

            try {
                // 构造 WMI 查询语句
                const string query = "SELECT * FROM Win32_OperatingSystem";

                // 创建 ManagementObjectSearcher 对象
                using var searcher = new ManagementObjectSearcher(query);
                // 获取查询结果集合
                var result = searcher.Get();

                // 遍历结果集合并读取系统信息
                foreach (var o in result) {
                    var obj = (ManagementObject)o;
                    systemInfo.DeviceName = obj?["CSName"]?.ToString() ?? string.Empty;          // 设备名称
                    systemInfo.ProductId = obj?["SerialNumber"]?.ToString() ?? string.Empty;     // 产品ID
                    systemInfo.SystemType = obj?["OSArchitecture"]?.ToString() ?? string.Empty;  // 系统类型
                    systemInfo.WindowsVersion = obj?["Version"]?.ToString() ?? string.Empty;     // Windows 版本
                    systemInfo.InstallDate = FormatDateTime(obj?["InstallDate"]?.ToString()); // 安装日期
                    systemInfo.OsVersion = obj?["Caption"]?.ToString() ?? string.Empty;           // 操作系统版本
                    // 获取设备ID
                    systemInfo.DeviceId = GetComputerSystemUuid();
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return systemInfo;
        }

        public async Task<List<LocalNetworkConnectionInfo>?> GetLocalNetworkConnectionInfosAsync1() {
            var connectionInfos = new List<LocalNetworkConnectionInfo>();
            return connectionInfos;
            /*await Task.Run(() => {
                try {
                    var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    if (_computer?.Hardware != null) {
                        foreach (var hardwareItem in _computer.Hardware) {
                            hardwareItem.Update();
                            if (hardwareItem.HardwareType == HardwareType.Network) {
                                foreach (var sensor in hardwareItem.Sensors) {
                                    if (sensor.SensorType == SensorType.Throughput && sensor.Name == "Upload Speed") {
                                        double uploadSpeed = sensor.Value.GetValueOrDefault();
                                        var connectionInfo = new LocalNetworkConnectionInfo {
                                            ConnectionName = hardwareItem.Name,
                                            UploadSpeed = uploadSpeed / 1024,
                                            Speed = interfaces?.FirstOrDefault(f => f.Name.Equals(hardwareItem.Name))?.Speed ?? 0
                                        };
                                        connectionInfos.Add(connectionInfo);
                                    }
                                    else if (sensor is { SensorType: SensorType.Throughput, Name: "Download Speed" }) {
                                        double downloadSpeed = sensor.Value.GetValueOrDefault();
                                        var connectionInfo =
                                            connectionInfos.FirstOrDefault(c => c.ConnectionName == hardwareItem.Name);
                                        if (connectionInfo != null) {
                                            connectionInfo.DownloadSpeed = downloadSpeed / 1024;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
            });

            return connectionInfos;*/
        }

        public async Task<List<LocalNetworkConnectionInfo>?> GetLocalNetworkConnectionInfosAsync() {
            var connectionInfos = new List<LocalNetworkConnectionInfo>();
            await Task.Run(async () => {
                try {
                    var statsAtStarts = new List<IPv4InterfaceStatistics>();
                    var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(w =>
                            w.NetworkInterfaceType != NetworkInterfaceType.Loopback)?.ToList();
                    if (interfaces?.Any() == true) {
                        statsAtStarts.AddRange(interfaces.Select(t => t.GetIPv4Statistics()));

                        await Task.Delay(1000);
                        for (var i = 0; i < interfaces.Count; i++) {
                            var statsAtEnd = interfaces[i].GetIPv4Statistics();
                            var downloadSpeed = (statsAtEnd.BytesReceived - statsAtStarts[i].BytesReceived);
                            var uploadSpeed = (statsAtEnd.BytesSent - statsAtStarts[i].BytesSent);
                            connectionInfos.Add(new LocalNetworkConnectionInfo() {
                                IsConnection = interfaces[i].OperationalStatus == OperationalStatus.Up,
                                ConnectionName = interfaces[i].Name,
                                DownloadSpeed = downloadSpeed / 1024,
                                UploadSpeed = uploadSpeed / 1024,
                                Speed = interfaces[i].Speed,
                                Type = interfaces[i].NetworkInterfaceType switch {
                                    NetworkInterfaceType.Wireless80211 => NetworkType.Wifi,
                                    var ethernetTypes when new[]
                                    {
                                    NetworkInterfaceType.Ethernet,
                                    NetworkInterfaceType.Ethernet3Megabit,
                                    NetworkInterfaceType.FastEthernetT,
                                    NetworkInterfaceType.FastEthernetFx,
                                    NetworkInterfaceType.GigabitEthernet
                                }.Contains(ethernetTypes) => NetworkType.Ethernet,
                                    NetworkInterfaceType.Tunnel => NetworkType.Tunnel,
                                    var wmanTypes when new[]
                                    {
                                    NetworkInterfaceType.Wman,
                                    NetworkInterfaceType.Wwanpp,
                                    NetworkInterfaceType.Wwanpp2
                                }.Contains(wmanTypes) => NetworkType.Wman,

                                    _ => NetworkType.Unknown
                                }
                            });
                        }
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            });
            return connectionInfos;
        }

        public async Task<string> GenerateMachineCode() {
            await Task.Yield();
            var cpuSerialNumber = string.Empty;
            var hardDiskId = string.Empty;
            var machineName = string.Empty;
            var versionString = string.Empty;
            var machineCode = string.Empty;
            try {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                var collection = searcher.Get();
                foreach (var o in collection) {
                    var obj = (ManagementObject)o;
                    cpuSerialNumber += obj?["ProcessorId"].ToString();
                }
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                collection = searcher.Get();
                foreach (var o in collection) {
                    var obj = (ManagementObject)o;
                    hardDiskId += obj?["SerialNumber"].ToString();
                }

                machineName = Environment.MachineName;
                versionString = Environment.OSVersion.VersionString;

                machineCode = $"{cpuSerialNumber}{hardDiskId}{machineName}{versionString}";

                using (var md5 = MD5.Create()) {
                    var result = md5.ComputeHash(Encoding.UTF8.GetBytes($"{machineCode}Hisoka"));
                    var strResult = BitConverter.ToString(result);
                    machineCode = strResult.Replace("-", "");
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error(e);
            }
            return machineCode;
        }

        private string? GetComputerSystemUuid() {
            using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
            foreach (var o in searcher.Get()) {
                var obj = (ManagementObject)o;
                return obj["UUID"]?.ToString();
            }

            return "";
        }

        private DateTime? FormatDateTime(string? dateTimeString) {
            if (dateTimeString is { Length: >= 14 }) {
                return DateTime.ParseExact(dateTimeString[..14], "yyyyMMddHHmmss", null);
            }
            return null;
        }
    }
}