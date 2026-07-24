using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Management;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading.Tasks;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using System.Security.Cryptography;
using LibreHardwareMonitor.Hardware;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Diagnostics.Eventing.Reader;
using System.Threading;

namespace JayTom.Dws.Infrastructure.IComputer {

    public class Computer : IComputer {
        /// <summary>
        /// 线程安全的硬件监控器工厂，确保进程内只初始化一次底层驱动和传感器。
        /// </summary>
        private static readonly Lazy<LibreHardwareMonitor.Hardware.Computer?> HardwareMonitor =
            new(CreateHardwareMonitor, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 当前进程共享的硬件监控器；初始化失败时为空并自动降级为系统接口采集。
        /// </summary>
        private readonly LibreHardwareMonitor.Hardware.Computer? _computer;

        /// <summary>
        /// 标记内存采集异常是否已经记录，避免周期采集失败时重复刷写日志。
        /// </summary>
        private static int _memoryInfoErrorLogged;

        /// <summary>
        /// Windows 全局内存状态数据。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx {
            /// <summary>
            /// 结构体字节长度。
            /// </summary>
            public uint Length;

            /// <summary>
            /// 内存使用百分比。
            /// </summary>
            public uint MemoryLoad;

            /// <summary>
            /// 物理内存总字节数。
            /// </summary>
            public ulong TotalPhysicalMemory;

            /// <summary>
            /// 可用物理内存字节数。
            /// </summary>
            public ulong AvailablePhysicalMemory;

            /// <summary>
            /// 页面文件总字节数。
            /// </summary>
            public ulong TotalPageFile;

            /// <summary>
            /// 可用页面文件字节数。
            /// </summary>
            public ulong AvailablePageFile;

            /// <summary>
            /// 虚拟内存总字节数。
            /// </summary>
            public ulong TotalVirtualMemory;

            /// <summary>
            /// 可用虚拟内存字节数。
            /// </summary>
            public ulong AvailableVirtualMemory;

            /// <summary>
            /// 扩展虚拟内存可用字节数。
            /// </summary>
            public ulong AvailableExtendedVirtualMemory;
        }

        /// <summary>
        /// 读取 Windows 全局内存状态。
        /// </summary>
        /// <param name="buffer">接收内存状态的结构体。</param>
        /// <returns>读取成功时为真。</returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        /// <summary>
        /// 初始化电脑信息采集服务。
        /// </summary>
        public Computer() {
            _computer = HardwareMonitor.Value;
        }

        /// <summary>
        /// 创建硬件监控器，并逐类隔离不兼容的硬件枚举异常。
        /// </summary>
        /// <returns>可用的硬件监控器；基础初始化失败时返回空。</returns>
        private static LibreHardwareMonitor.Hardware.Computer? CreateHardwareMonitor() {
            var computer = new LibreHardwareMonitor.Hardware.Computer();
            try {
                // 先以空硬件组打开，再逐类启用，避免单个设备组异常导致整个客户端无法启动。
                computer.Open();
            }
            catch (Exception exception) {
                NLog.LogManager.GetCurrentClassLogger().Warn(exception,
                    "硬件监控基础组件初始化失败，已降级为系统接口采集。");
                return null;
            }

            TryEnableHardwareCategory("CPU", () => computer.IsCpuEnabled = true);
            TryEnableHardwareCategory("主板", () => computer.IsMotherboardEnabled = true);
            TryEnableHardwareCategory("GPU", () => computer.IsGpuEnabled = true);
            return computer;
        }

        /// <summary>
        /// 启用单个硬件类别；类别不兼容时记录告警并继续启动。
        /// </summary>
        /// <param name="categoryName">硬件类别名称。</param>
        /// <param name="enableAction">启用硬件类别的操作。</param>
        private static void TryEnableHardwareCategory(string categoryName, Action enableAction) {
            try {
                enableAction();
            }
            catch (Exception exception) {
                NLog.LogManager.GetCurrentClassLogger().Warn(exception,
                    $"硬件监控类别“{categoryName}”初始化失败，已跳过该类别。");
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
                var memoryStatus = new MemoryStatusEx {
                    Length = Convert.ToUInt32(Marshal.SizeOf<MemoryStatusEx>())
                };
                if (!GlobalMemoryStatusEx(ref memoryStatus)) {
                    throw new InvalidOperationException(
                        $"读取系统内存状态失败，Windows 错误码：{Marshal.GetLastWin32Error()}。");
                }

                var totalMemory = Convert.ToInt64(memoryStatus.TotalPhysicalMemory);
                var availableMemory = Convert.ToInt64(memoryStatus.AvailablePhysicalMemory);
                var usedMemory = Math.Max(0, totalMemory - availableMemory);
                var availableMemoryPercent = totalMemory > 0
                    ? Math.Round(Convert.ToDecimal(availableMemory) / totalMemory * 100m, 2)
                    : 0m;
                var usedMemoryPercent = totalMemory > 0
                    ? Math.Round(Convert.ToDecimal(usedMemory) / totalMemory * 100m, 2)
                    : 0m;

                Interlocked.Exchange(ref _memoryInfoErrorLogged, 0);
                return new MemoryInfo {
                    UsedMemory = usedMemory,
                    AvailableMemory = availableMemory,
                    AvailableMemoryFormat = FormatByteSize(availableMemory),
                    AvailableMemoryPercentage = Convert.ToSingle(availableMemoryPercent),
                    UsedMemoryFormat = FormatByteSize(usedMemory),
                    UsedMemoryPercent = Convert.ToSingle(usedMemoryPercent),
                };
            }
            catch (Exception exception) {
                if (Interlocked.Exchange(ref _memoryInfoErrorLogged, 1) == 0) {
                    NLog.LogManager.GetCurrentClassLogger().Warn(exception,
                        "物理内存信息采集失败，后续相同异常将不再重复写入日志。");
                }
            }

            return new MemoryInfo();
        }

        public Task<MemoryInfo> GetMemoryInfoAsync() {
            return Task.FromResult(GetMemoryInfo());
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
            try {
                using var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var currentVersion = localMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
                using var cryptography = localMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Cryptography", false);

                var systemInfo = new SystemInfo {
                    DeviceName = Environment.MachineName,
                    DeviceId = cryptography?.GetValue("MachineGuid")?.ToString() ?? string.Empty,
                    ProductId = currentVersion?.GetValue("ProductId")?.ToString() ?? string.Empty,
                    SystemType = Environment.Is64BitOperatingSystem ? "64 位操作系统" : "32 位操作系统",
                    WindowsVersion = Environment.OSVersion.Version.ToString(),
                    OsVersion = currentVersion?.GetValue("ProductName")?.ToString()
                        ?? Environment.OSVersion.VersionString
                };

                var installDateValue = currentVersion?.GetValue("InstallDate")?.ToString();
                if (long.TryParse(installDateValue, out var installDateSeconds)) {
                    systemInfo.InstallDate =
                        DateTimeOffset.FromUnixTimeSeconds(installDateSeconds).LocalDateTime;
                }

                return systemInfo;
            }
            catch (Exception exception) {
                NLog.LogManager.GetCurrentClassLogger().Warn(exception,
                    "系统信息读取失败，已返回基础环境信息。");
                return new SystemInfo {
                    DeviceName = Environment.MachineName,
                    SystemType = Environment.Is64BitOperatingSystem ? "64 位操作系统" : "32 位操作系统",
                    WindowsVersion = Environment.OSVersion.Version.ToString(),
                    OsVersion = Environment.OSVersion.VersionString
                };
            }
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

        /// <summary>
        /// 将字节数格式化为便于界面显示的定点数容量文本。
        /// </summary>
        /// <param name="byteCount">字节数。</param>
        /// <returns>带容量单位的文本。</returns>
        private static string FormatByteSize(long byteCount) {
            if (byteCount <= 0) {
                return "0 B";
            }

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            var value = Convert.ToDecimal(byteCount);
            var unitIndex = 0;
            while (value >= 1024m && unitIndex < units.Length - 1) {
                value /= 1024m;
                unitIndex++;
            }

            return $"{value:0.##} {units[unitIndex]}";
        }
    }
}
