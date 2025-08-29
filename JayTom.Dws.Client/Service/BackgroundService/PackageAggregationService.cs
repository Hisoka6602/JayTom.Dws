using System;
using ImTools;
using NPOI.Util;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// 集包服务
    /// </summary>
    public class PackageAggregationService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IPackageRepository _packageRepository;
        private readonly IExitMonitor _exitMonitor;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;

        private ConcurrentDictionary<long, PackageAggregationInfo> _exitPackageAggregationItems = new();
        private ConcurrentQueue<PushPackageInfo> _packageInfoItems = new();
        private ConcurrentDictionary<DateTime, PackageAggregationInfo> _overexitPackageAggregationItems = new();
        private SemaphoreSlim _createPackageSlim = new(1);

        public PackageAggregationService(IPackageRepository packageRepository,
            IExitMonitor exitMonitor, IPackageExitDefinitionRepository packageExitDefinitionRepository) {
            _packageRepository = packageRepository;
            _exitMonitor = exitMonitor;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;

            EventAggregator.Instance.Subscribe<PushPackageInfo>(async item => {
                if (item is { } model) {
                    //判断加入历史包还是当前包
                    try {
                        await _createPackageSlim.WaitAsync();
                        if (model.SignalCallbackTime is not null) {
                            var packageAggregationInfo = _overexitPackageAggregationItems.OrderBy(o => o.Key).FirstOrDefault(f =>
                                f.Value.ExitId.Equals(model.PackageExitUpdateEvent.ExitId) &&
                                f.Key.AddMilliseconds(200) >= model.SignalCallbackTime).Value;
                            if (packageAggregationInfo is not null) {
                                packageAggregationInfo.PackageItems.Add(new PackageInfoModel() {
                                    BarCodeInfo = new BarCodeInfoModel() {
                                        Barcode = model?.PackageInfo?.BarCodeInfo?.Barcode ?? string.Empty
                                    },
                                    PackageTimestamped = model?.PackageInfo?.Timestamp ?? 0
                                });
                                NLog.LogManager.GetCurrentClassLogger().Error($"集包:加入历史包:{model?.PackageInfo?.BarCodeInfo?.Barcode}-格口:{packageAggregationInfo.ExitId + 1}-{packageAggregationInfo.AggregatePackageCode}");
                            }
                            else {
                                NLog.LogManager.GetCurrentClassLogger().Error($"集包:加入实时包:{model.PackageInfo?.BarCodeInfo?.Barcode}-格口:{model.PackageExitUpdateEvent.ExitName}");
                                _packageInfoItems.Enqueue(model);
                            }
                        }
                        else {
                            NLog.LogManager.GetCurrentClassLogger().Error($"集包:未获取到落格指令时间");
                        }
                    }
                    finally {
                        _createPackageSlim.Release();
                    }
                }
            });

            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item => {
                if (item is { } info) {
                    if (info.Status == ApplicationStatus.Stop) {
                        _exitPackageAggregationItems.Clear();
                        _overexitPackageAggregationItems.Clear();
                        _packageInfoItems.Clear();
                    }
                }
            });

            _exitMonitor.LockExitEvent += async (sender, model) => {
                /*var (key, value) = _exitPackageAggregationItems
                    .FirstOrDefault(f => f.Key.Equals(model.Id));*/
                NLog.LogManager.GetCurrentClassLogger().Error($"硬件锁格");
                try {
                    await _createPackageSlim.WaitAsync();
                    //从原包裹分离数据
                    var packageItems = new List<PackageInfoModel>();
                    var (key, value) = _exitPackageAggregationItems.
                        FirstOrDefault(f =>
                        f.Key.Equals(model.Id));
                    if (value is not null) {
                        //取出数据

                        packageItems = value.PackageItems.Select(s => new PackageInfoModel {
                            PackageTimestamped = s.PackageTimestamped,
                            PackageCreateTime = s.PackageCreateTime,
                            AggregatePackagesInfo = s.AggregatePackagesInfo,
                            BarCodeInfo = s.BarCodeInfo,
                            CloudVideoUploadInfo = s.CloudVideoUploadInfo,
                            ExitInfo = s.ExitInfo,
                        })?.ToList();

                        //清空原包信息
                        value.PackageItems.Clear();
                    }
                    //锁格后创建历史包
                    var packagingTime = DateTime.Now;
                    var packageAggregationInfo = new PackageAggregationInfo() {
                        PackageExitDefinitionInfo = model,
                        PackagingTime = packagingTime,
                        ExitId = model.Id,
                        AggregatePackageCode =
                            $"PK{DateTimeOffset.Now.ToUnixTimeSeconds()}{model.Id.ToString().PadLeft(3, '0')}",
                        PackageItems = packageItems ?? new List<PackageInfoModel>(),
                    };
                    _overexitPackageAggregationItems.TryAdd(packagingTime, packageAggregationInfo);

                    NLog.LogManager.GetCurrentClassLogger().Error($"创建历史包-{packageAggregationInfo.AggregatePackageCode}");
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            _exitMonitor.UnLockExitEvent += (sender, model) => {
                /*
                var packageAggregationInfo = _exitPackageAggregationItems
                    .FirstOrDefault(f => f.Key.Equals(model.Id)).Value;
                    */

                //清空
                // packageAggregationInfo?.PackageItems.Clear();
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //遍历格口

            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.IsActive &&
                s.Type != ExitType.AbnormalExit,
                o => o.Id, stoppingToken);
            packageExitDefinitionInfoModels.ForEach(s => {
                var packageAggregationInfo = new PackageAggregationInfo {
                    PackageExitDefinitionInfo = s,
                    PackageItems = new List<PackageInfoModel>(),
                    AggregatePackageCode =
                        $"PK{DateTimeOffset.Now.ToUnixTimeSeconds()}{s.Id.ToString().PadLeft(3, '0')}",
                };
                _exitPackageAggregationItems.TryAdd(s.Id, packageAggregationInfo);
            });

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    var tryDequeue = _packageInfoItems.TryDequeue(out var packageInfo);
                    if (tryDequeue && packageInfo is not null) {
                        //检查所有格口有没有对应的包裹

                        var packageAggregationInfo = _exitPackageAggregationItems
                            .FirstOrDefault(f => f.Key.Equals(packageInfo.PackageExitUpdateEvent?.ExitId ?? 0)).Value;
                        if (packageAggregationInfo is not null) {
                            packageAggregationInfo.PackageItems.Add(new PackageInfoModel() {
                                BarCodeInfo = new BarCodeInfoModel() {
                                    Barcode = packageInfo?.PackageInfo?.BarCodeInfo?.Barcode ?? string.Empty
                                },
                                PackageTimestamped = packageInfo?.PackageInfo?.Timestamp ?? 0
                            });
                        }
                        else {
                            _packageInfoItems.Enqueue(packageInfo);
                        }
                    }
                    var keyValuePairs = _overexitPackageAggregationItems.Where(w =>
                        DateTime.Now.Subtract(w.Value.PackagingTime).TotalSeconds >= 23)?.ToList();
                    if (keyValuePairs?.Any() == true) {
                        NLog.LogManager.GetCurrentClassLogger().Error("推送集包");
                        Parallel.ForEach(keyValuePairs, svalue => {
                            _overexitPackageAggregationItems.TryRemove(svalue.Key, out var info);
                            EventAggregator.Instance.Publish(info);
                        });
                    }
                    await Task.Delay(30, stoppingToken);
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
        }
    }

    public class PackageAggregationInfo {
        public PackageExitDefinitionInfoModel PackageExitDefinitionInfo { get; set; } = new();

        public List<PackageInfoModel> PackageItems { get; set; } = new();

        /// <summary>
        /// 聚合包裹码
        /// </summary>
        public string AggregatePackageCode { get; set; } = string.Empty;

        /// <summary>
        /// 打包时间
        /// </summary>
        public DateTime PackagingTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 格口Id
        /// </summary>
        public long ExitId { get; set; }
    }
}