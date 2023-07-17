using System;
using System.Linq;
using System.Text;
using System.Management;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Net.NetworkInformation;
using System.Diagnostics.Eventing.Reader;

namespace JayTom.Dws.Infrastructure.IComputer {

    public class Computer : IComputer {

        public List<DiskInfo> GetDiskInfo() {
            var diskInfoList = new List<DiskInfo>();
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            try {
                diskInfoList = DriveInfo.GetDrives()
                    .Where(drive => drive.IsReady && drive.DriveType == DriveType.Fixed)
                    .Select(drive => {
                        var availableSpace = drive.AvailableFreeSpace;
                        var usedSpace = drive.TotalSize - drive.AvailableFreeSpace;
                        var availableSpaceIndex = (int)Math.Floor(Math.Log(availableSpace, 1024));
                        var usedSpaceIndex = (int)Math.Floor(Math.Log(usedSpace, 1024));
                        return new DiskInfo {
                            Name = drive.Name?.Replace(":", string.Empty)?.Replace("\\", string.Empty),
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
                    .Where(drive => drive.IsReady && drive.DriveType == DriveType.Fixed)
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

        public int GetFanSpeed() {
            var computer = new LibreHardwareMonitor.Hardware.Computer {
                IsMotherboardEnabled = true,
                IsCpuEnabled = true,
                IsStorageEnabled = true,
                IsBatteryEnabled = true,
                IsControllerEnabled = true,
                IsNetworkEnabled = true,
                IsPsuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true
            };

            try {
                computer.Open();

                var fanSensor = computer.Hardware
                    .FirstOrDefault(h => h.HardwareType == HardwareType.GpuIntel)?
                    .Sensors
                    .FirstOrDefault(s => s.SensorType == SensorType.Fan && s.Value.HasValue);

                return (int)(fanSensor?.Value ?? 0);
            }
            finally {
                computer.Close();
            }
        }

        public CpuInfo GetCpuInfo() {
            var computer = new LibreHardwareMonitor.Hardware.Computer { IsCpuEnabled = true };

            try {
                computer.Open();

                var hardware = computer.Hardware?.FirstOrDefault(f => f.HardwareType == HardwareType.Cpu);
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
                                 CpuCoreName = hardware.Sensors.FirstOrDefault(f => f.Name.Equals(s.Key))?.Name,
                                 CpuCoreSpeed = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Equals(s.Key) && f.SensorType == SensorType.Clock)?.Value ?? 0,
                                 CpuTemperature = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Equals(s.Key) && f.SensorType == SensorType.Temperature)?.Value ?? 0,
                                 CpuUsedPercent = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Contains(s.Key) && f.SensorType == SensorType.Load)?.Value ?? 0,
                                 Voltage = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Equals(s.Key) && f.SensorType == SensorType.Voltage)?.Value ?? 0,
                             })?.ToList()
                    };
                }
            }
            catch (Exception e) {
                // ignored
            }
            finally {
                computer.Close();
            }

            return new CpuInfo();
        }

        public async Task<CpuInfo> GetCpuInfoAsync() {
            var computer = new LibreHardwareMonitor.Hardware.Computer { IsCpuEnabled = true };

            try {
                await Task.Delay(0);
                computer.Open();
                var hardware = computer.Hardware?.FirstOrDefault(f => f.HardwareType == HardwareType.Cpu);
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
                                 CpuCoreName = hardware.Sensors.FirstOrDefault(f => f.Name.Equals(s.Key))?.Name,
                                 CpuCoreSpeed = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Equals(s.Key) && f.SensorType == SensorType.Clock)?.Value ?? 0,
                                 CpuTemperature = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Equals(s.Key) && f.SensorType == SensorType.Temperature)?.Value ?? 0,
                                 CpuUsedPercent = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Contains(s.Key) && f.SensorType == SensorType.Load)?.Value ?? 0,
                                 Voltage = hardware.Sensors.FirstOrDefault(f =>
                                     f.Name.Equals(s.Key) && f.SensorType == SensorType.Voltage)?.Value ?? 0,
                             })?.ToList()
                    };
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
                // ignored
            }
            finally {
                computer.Close();
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
            catch {
                // Do nothing and return default MemoryInfo object
            }

            return new MemoryInfo();
        }

        public TimeSpan GetWindowsUptime() {
            var counter = new PerformanceCounter("System", "System Up Time");
            return TimeSpan.FromSeconds(counter.NextValue());
        }

        public DateTime? GetLastShutdownTime() {
            var query = new EventLogQuery("System", PathType.LogName, "*[System/EventID=1074]");

            using var reader = new EventLogReader(query);
            var record = reader.ReadEvent();

            return record?.TimeCreated;
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
                Debug.WriteLine(gpu.Properties);
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
    }
}