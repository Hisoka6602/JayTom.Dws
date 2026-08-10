using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Management;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using LibreHardwareMonitor.Hardware;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        /// 标记 CPU 采集异常是否已经记录。
        /// </summary>
        private static int _cpuInfoErrorLogged;

        /// <summary>
        /// 标记风扇采集异常是否已经记录。
        /// </summary>
        private static int _fanSpeedErrorLogged;

        /// <summary>
        /// 标记 GPU 采集异常是否已经记录。
        /// </summary>
        private static int _gpuInfoErrorLogged;

        /// <summary>
        /// 标记网络采集异常是否已经记录。
        /// </summary>
        private static int _networkInfoErrorLogged;

        /// <summary>
        /// 网络速率采样周期。
        /// </summary>
        private static readonly TimeSpan NetworkSampleInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 网络采样任务同步锁。
        /// </summary>
        private readonly System.Threading.Lock _networkSampleLock = new();

        /// <summary>
        /// 当前共享网络采样任务，使汇总网卡和主网卡查询复用同一次计数器采样。
        /// </summary>
        private Task<(NetworkInfo NetworkInfo, List<LocalNetworkConnectionInfo> ConnectionInfos)>?
            _networkSampleTask;

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
            try {
                diskInfoList = [.. DriveInfo.GetDrives()
                    .Where(drive => drive is { IsReady: true, DriveType: DriveType.Fixed })
                    .Select(drive => {
                        var availableSpace = drive.AvailableFreeSpace;
                        var totalSpace = drive.TotalSize;
                        var usedSpace = Math.Max(0, totalSpace - availableSpace);
                        var availablePercentage = totalSpace > 0
                            ? Math.Round(Convert.ToDecimal(availableSpace) / totalSpace * 100m, 2)
                            : 0m;
                        var usedPercentage = totalSpace > 0
                            ? Math.Round(Convert.ToDecimal(usedSpace) / totalSpace * 100m, 2)
                            : 0m;
                        return new DiskInfo {
                            Name = drive.Name?.Replace(":", string.Empty)?.Replace("\\", string.Empty) ?? string.Empty,
                            AvailableDiskSpace = availableSpace,
                            AvailableDiskSpaceFormat = FormatByteSize(availableSpace),
                            AvailableDiskSpacePercentage = Convert.ToSingle(availablePercentage),
                            UsedDiskSpacePercentage = usedPercentage,
                            UsedDiskSpace = usedSpace,
                            UsedDiskSpaceFormat = FormatByteSize(usedSpace)
                        };
                    })];
            }
            catch (Exception ex) {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "获取磁盘信息失败");
            }

            return diskInfoList;
        }

        public Task<List<DiskInfo>> GetDiskInfoAsync() {
            // 驱动器枚举可能触发系统调用，放到工作线程避免阻塞调用方。
            return Task.Run(GetDiskInfo);
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

                Interlocked.Exchange(ref _fanSpeedErrorLogged, 0);
                return (int)(fanSensor?.Value ?? 0);
            }
            catch (Exception e) {
                if (Interlocked.Exchange(ref _fanSpeedErrorLogged, 1) == 0) {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Warn(e, "风扇转速采集失败，后续相同异常将不再重复写入日志。");
                }
                return 0;
            }
        }

        public Task<int> GetFanSpeedAsync() {
            // 底层硬件更新是同步调用，使用独立工作线程保持与其他硬件采集并行。
            return Task.Run(GetFanSpeed);
        }

        public CpuInfo GetCpuInfo() {
            try {
                var hardware = _computer?.Hardware?.FirstOrDefault(f => f.HardwareType == HardwareType.Cpu);
                if (hardware is not null) {
                    hardware.Update();
                    var sensors = hardware.Sensors ?? [];
                    var sensorLookup = new Dictionary<(string Name, SensorType Type), ISensor>(sensors.Length);
                    var coreNames = new List<string>();
                    foreach (var sensor in sensors) {
                        sensorLookup.TryAdd((sensor.Name, sensor.SensorType), sensor);
                        if (sensor.SensorType == SensorType.Clock &&
                            sensor.Name.StartsWith("CPU Core ", StringComparison.Ordinal)) {
                            coreNames.Add(sensor.Name);
                        }
                    }

                    var coreInfos = new List<CpuCoreInfo>(coreNames.Count);
                    foreach (var coreName in coreNames) {
                        sensorLookup.TryGetValue((coreName, SensorType.Clock), out var clockSensor);
                        sensorLookup.TryGetValue((coreName, SensorType.Temperature), out var temperatureSensor);
                        sensorLookup.TryGetValue((coreName, SensorType.Voltage), out var voltageSensor);
                        var loadSensor = sensors.FirstOrDefault(sensor =>
                            sensor.SensorType == SensorType.Load &&
                            sensor.Name.Contains(coreName, StringComparison.Ordinal));
                        coreInfos.Add(new CpuCoreInfo {
                            CpuCoreName = coreName,
                            CpuCoreSpeed = clockSensor?.Value ?? 0,
                            CpuTemperature = temperatureSensor?.Value ?? 0,
                            CpuUsedPercent = loadSensor?.Value ?? 0,
                            Voltage = voltageSensor?.Value ?? 0,
                        });
                    }

                    var cpuInfo = new CpuInfo() {
                        CpuPackageTemperature = sensors.FirstOrDefault(f => f.Name.Equals("CPU Package"))
                             ?.Value.GetValueOrDefault() ?? 0,
                        CpuTotalUsedPercent = sensors.FirstOrDefault(f => f.Name.Equals("CPU Total"))?.Value
                             .GetValueOrDefault() ?? 0,
                        CpuBusSpeed = sensors.FirstOrDefault(f => f.Name.Equals("Bus Speed"))?.Value
                             .GetValueOrDefault() ?? 0,
                        CpuName = $"{hardware.Name}",
                        CpuCoreInfos = coreInfos
                    };
                    Interlocked.Exchange(ref _cpuInfoErrorLogged, 0);
                    return cpuInfo;
                }
            }
            catch (Exception e) {
                if (Interlocked.Exchange(ref _cpuInfoErrorLogged, 1) == 0) {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Warn(e, "CPU 信息采集失败，后续相同异常将不再重复写入日志。");
                }
            }

            return new CpuInfo();
        }

        public Task<CpuInfo> GetCpuInfoAsync() {
            // 底层硬件更新是同步调用，使用独立工作线程保持与其他硬件采集并行。
            return Task.Run(GetCpuInfo);
        }

        public NetworkInfo GetNetworkInfo() {
            return GetNetworkInfoAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<NetworkInfo> GetNetworkInfoAsync() {
            var sample = await GetNetworkSampleTask().ConfigureAwait(false);
            return sample.NetworkInfo;
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

                        Interlocked.Exchange(ref _gpuInfoErrorLogged, 0);
                        return (from gpu in gpuHardwareList let utilizationSensor = gpu.Sensors?.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "D3D 3D") let memorySensor = gpu.Sensors?.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "GPU Memory") let spaceSensor = gpu.Sensors?.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "GPU Memory Free") let gpuName = gpu.Name select new GpuInfo { Name = gpuName, Utilization = (int)(utilizationSensor?.Value ?? 0), TotalMemory = (long)(memorySensor?.Max ?? 0), FreeMemory = (long)(spaceSensor?.Value ?? 0) }).ToList();
                    }
                }
                catch (Exception e) {
                    if (Interlocked.Exchange(ref _gpuInfoErrorLogged, 1) == 0) {
                        NLog.LogManager.GetCurrentClassLogger()
                            .Warn(e, "GPU 信息采集失败，后续相同异常将不再重复写入日志。");
                    }
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
            var sample = await GetNetworkSampleTask().ConfigureAwait(false);
            return sample.ConnectionInfos;
        }

        /// <summary>
        /// 获取当前共享网络采样任务。
        /// </summary>
        /// <returns>包含主网卡和全部网卡速率的共享采样任务。</returns>
        private Task<(NetworkInfo NetworkInfo, List<LocalNetworkConnectionInfo> ConnectionInfos)>
            GetNetworkSampleTask() {
            lock (_networkSampleLock) {
                if (_networkSampleTask is null || _networkSampleTask.IsCompleted) {
                    _networkSampleTask = CollectNetworkSampleAsync();
                }

                return _networkSampleTask;
            }
        }

        /// <summary>
        /// 采集一次所有网卡计数器，并生成主网卡及本地连接结果。
        /// </summary>
        /// <returns>主网卡信息和本地连接信息。</returns>
        private static async Task<(NetworkInfo NetworkInfo,
            List<LocalNetworkConnectionInfo> ConnectionInfos)> CollectNetworkSampleAsync() {
            var networkInfo = new NetworkInfo();
            var connectionInfos = new List<LocalNetworkConnectionInfo>();
            try {
                var samples =
                    new List<(NetworkInterface Interface, long BytesReceived, long BytesSent)>();
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces()) {
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback) {
                        continue;
                    }

                    try {
                        var statistics = networkInterface.GetIPv4Statistics();
                        samples.Add((networkInterface, statistics.BytesReceived, statistics.BytesSent));
                    }
                    catch (NetworkInformationException) {
                        // 单个虚拟或瞬时失效网卡不可采样时跳过，不影响其他网卡。
                    }
                }

                if (samples.Count == 0) {
                    return (networkInfo, connectionInfos);
                }

                var startTimestamp = Stopwatch.GetTimestamp();
                await Task.Delay(NetworkSampleInterval).ConfigureAwait(false);
                var elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
                var primaryInterface = samples
                    .Select(sample => sample.Interface)
                    .FirstOrDefault(networkInterface =>
                        networkInterface.OperationalStatus == OperationalStatus.Up);

                foreach (var sample in samples) {
                    try {
                        var statistics = sample.Interface.GetIPv4Statistics();
                        var downloadSpeed = CalculateBytesPerSecond(
                            sample.BytesReceived, statistics.BytesReceived, elapsedTicks);
                        var uploadSpeed = CalculateBytesPerSecond(
                            sample.BytesSent, statistics.BytesSent, elapsedTicks);
                        connectionInfos.Add(new LocalNetworkConnectionInfo {
                            IsConnection = sample.Interface.OperationalStatus == OperationalStatus.Up,
                            ConnectionName = sample.Interface.Name,
                            DownloadSpeed = downloadSpeed / 1024,
                            UploadSpeed = uploadSpeed / 1024,
                            Speed = sample.Interface.Speed,
                            Type = MapNetworkType(sample.Interface.NetworkInterfaceType)
                        });

                        if (!ReferenceEquals(sample.Interface, primaryInterface)) {
                            continue;
                        }

                        networkInfo.NetworkDownloadSpeed = downloadSpeed;
                        networkInfo.NetworkUploadSpeed = uploadSpeed;
                        networkInfo.NetworkDownloadSpeedFormat = FormatByteRate(downloadSpeed);
                        networkInfo.NetworkUploadSpeedFormat = FormatByteRate(uploadSpeed);
                        networkInfo.MacAddress = sample.Interface.GetPhysicalAddress().ToString();
                        networkInfo.IpAddress = sample.Interface.GetIPProperties()
                            .UnicastAddresses
                            .FirstOrDefault(address =>
                                address.Address.AddressFamily == AddressFamily.InterNetwork)
                            ?.Address.ToString() ?? string.Empty;
                    }
                    catch (NetworkInformationException) {
                        // 采样期间失效的网卡仅跳过当前结果。
                    }
                }
            }
            catch (Exception exception) {
                if (Interlocked.Exchange(ref _networkInfoErrorLogged, 1) == 0) {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Warn(exception, "网络信息采集失败，后续相同异常将不再重复写入日志。");
                }
                return (networkInfo, connectionInfos);
            }

            Interlocked.Exchange(ref _networkInfoErrorLogged, 0);
            return (networkInfo, connectionInfos);
        }

        /// <summary>
        /// 根据计数器差值和实际经过时间计算每秒字节数。
        /// </summary>
        /// <param name="startBytes">采样开始字节数。</param>
        /// <param name="endBytes">采样结束字节数。</param>
        /// <param name="elapsedTicks">采样经过的高精度计时刻度。</param>
        /// <returns>每秒字节数。</returns>
        private static long CalculateBytesPerSecond(long startBytes, long endBytes, long elapsedTicks) {
            if (endBytes <= startBytes || elapsedTicks <= 0) {
                return 0;
            }

            var bytes = Convert.ToDecimal(endBytes - startBytes);
            var rate = bytes * Stopwatch.Frequency / elapsedTicks;
            return decimal.ToInt64(decimal.Truncate(rate));
        }

        /// <summary>
        /// 将网络接口类型转换为客户端连接类型。
        /// </summary>
        /// <param name="networkInterfaceType">系统网络接口类型。</param>
        /// <returns>客户端连接类型。</returns>
        private static NetworkType MapNetworkType(NetworkInterfaceType networkInterfaceType) {
            return networkInterfaceType switch {
                NetworkInterfaceType.Wireless80211 => NetworkType.Wifi,
                NetworkInterfaceType.Ethernet or
                    NetworkInterfaceType.Ethernet3Megabit or
                    NetworkInterfaceType.FastEthernetT or
                    NetworkInterfaceType.FastEthernetFx or
                    NetworkInterfaceType.GigabitEthernet => NetworkType.Ethernet,
                NetworkInterfaceType.Tunnel => NetworkType.Tunnel,
                NetworkInterfaceType.Wman or
                    NetworkInterfaceType.Wwanpp or
                    NetworkInterfaceType.Wwanpp2 => NetworkType.Wman,
                _ => NetworkType.Unknown
            };
        }

        /// <summary>
        /// 将每秒字节数格式化为可读速率。
        /// </summary>
        /// <param name="bytesPerSecond">每秒字节数。</param>
        /// <returns>带速率单位的文本。</returns>
        private static string FormatByteRate(long bytesPerSecond) {
            return $"{FormatByteSize(bytesPerSecond)}/s";
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

                // DWS-HEX-COMPACT: 许可证机器码必须保持既有的无分隔符格式。
                machineCode = Convert.ToHexString(MD5.HashData(
                    Encoding.UTF8.GetBytes($"{machineCode}Hisoka")));
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

            string[] units = ["B", "KB", "MB", "GB", "TB"];
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
