using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Infrastructure.IComputer;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class ComputerInfoBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IComputerInfoReporter _computerInfoReporter;
        private readonly IConfiguration _configuration;
        private readonly IComputer _computer;

        public ComputerInfoBackgroundService(IComputerInfoReporter computerInfoReporter,
            IConfiguration configuration, IComputer computer) {
            _computerInfoReporter = computerInfoReporter;
            _configuration = configuration;
            _computer = computer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var counter = new PerformanceCounter("System", "System Up Time");
            while (!stoppingToken.IsCancellationRequested) {
                /*await Task.Yield();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                return;*/
                //获取Cpu信息
                var cpuInfoAsync = await _computer.GetCpuInfoAsync();
                //获取风扇信息
                var fanSpeed = _computer.GetFanSpeed();
                //获取内存信息
                var memoryInfoAsync = await _computer.GetMemoryInfoAsync();
                //获取显卡信息
                var gpuInfos = _computer.GetGpuInformation();
                //获取网络信息
                var networkInfo = await _computer.GetNetworkInfoAsync();
                //获取硬盘信息
                var diskInfoAsync = await _computer.GetDiskInfoAsync();
                var lastShutdownTime = _computer.GetLastShutdownTime();
                var lastShutdownUnexpected = _computer.GetLastShutdownUnexpected();
                //提交到事件
                _computerInfoReporter.OnComputerInfoReceived(new ComputerInfoModel() {
                    CpuInfo = new CpuInfoModel() {
                        ClockSpeed = cpuInfoAsync.CpuBusSpeed,
                        CpuTemperature = cpuInfoAsync.CpuPackageTemperature,
                        FanSpeed = fanSpeed,
                        Name = cpuInfoAsync.CpuName,
                        NumberOfCores = cpuInfoAsync.CpuCoreInfos?.Count ?? 0,
                        UsagePercentage = cpuInfoAsync.CpuTotalUsedPercent,
                    },
                    MemoryInfo = new MemoryInfoModel() {
                        AvailableSizeBytes = memoryInfoAsync.AvailableMemory,
                        UsedPercentage = memoryInfoAsync.UsedMemoryPercent,
                        MemoryRemaining = memoryInfoAsync.AvailableMemoryPercentage,
                        TotalSizeBytes = memoryInfoAsync.UsedMemory + memoryInfoAsync.AvailableMemory
                    },
                    GpuInfo = new GpuInfoModel() {
                        UsagePercentage = gpuInfos[0]?.Utilization ?? 0,
                        MemorySizeGb = (float)(gpuInfos[0]?.TotalMemory ?? 0) / 1024 / 1024 / 1024,
                        UsedMemoryGb = (float)((gpuInfos[0]?.TotalMemory ?? 0) - (gpuInfos[0]?.FreeMemory ?? 0)) / 1024 / 1024 / 1024,
                        UsedMemoryPercentage = gpuInfos[0]?.Utilization ?? 0,
                    },
                    NetworkInfo = new NetworkInfoModel() {
                        DownloadSpeed = networkInfo.NetworkDownloadSpeed,
                        UploadSpeed = networkInfo.NetworkUploadSpeed,
                        IpAddress = networkInfo.IpAddress,
                    },
                    HardDiskList = diskInfoAsync?.Select(s => new HardDiskInfoModel {
                        DiskName = s.Name,
                        FreeSpaceBytes = s.AvailableDiskSpace,
                        FreeSpacePercentage = s.AvailableDiskSpacePercentage,
                        UsedSpaceBytes = s.UsedDiskSpace,
                        UsedSpacePercentage = (float)s.UsedDiskSpacePercentage,
                    })?.ToList(),
                    UpTime = TimeSpan.FromSeconds(counter.NextValue())
                });
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}