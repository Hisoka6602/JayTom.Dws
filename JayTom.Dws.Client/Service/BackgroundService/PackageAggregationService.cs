using System;
using ImTools;
using NPOI.Util;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;

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
        private ConcurrentDictionary<long, PackageAggregationInfo> _overexitPackageAggregationItems = new();

        public PackageAggregationService(IPackageRepository packageRepository,
            IExitMonitor exitMonitor, IPackageExitDefinitionRepository packageExitDefinitionRepository) {
            _packageRepository = packageRepository;
            _exitMonitor = exitMonitor;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            /*EventAggregator.Instance.Subscribe<CallBackPackageInfo>(async item => {
                if (item is CallBackPackageInfo info) {
                    var (key, value) = await _packageRepository.FirstOrDefaultInfo(f => f.PackageCreateTime.Equals(info.PackageCreateTime));
                    if (key && value is not null) {
                        //添加包裹到队列
                        _packageInfoItems.Enqueue(value);
                    }
                }
            });*/
            EventAggregator.Instance.Subscribe<PushPackageInfo>(async item => {
                if (item is PushPackageInfo model) {
                    _packageInfoItems.Enqueue(model);
                }
            });

            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item => {
                if (item is ApplicationStatusChanged info) {
                    _packageInfoItems.Clear();
                }
            });

            _exitMonitor.LockExitEvent += async (sender, model) => {
                var (key, value) = _exitPackageAggregationItems
                    .FirstOrDefault(f => f.Key.Equals(model.Id));
                var packageAggregationInfo = _exitPackageAggregationItems
                    .FirstOrDefault(f => f.Key.Equals(model.Id)).Value;

                if (packageAggregationInfo is not null) {
                    packageAggregationInfo.PackagingTime = DateTime.Now;
                    var aggregationInfo = packageAggregationInfo.Copy();
                    _overexitPackageAggregationItems.TryAdd(model.Id, aggregationInfo);

                    //EventAggregator.Instance.Publish(packageAggregationInfo);

                    /*packageAggregationInfo.Copy();

                    _exitPackageAggregationItems.TryRemove(key, out _);
                    _exitPackageAggregationItems.TryAdd(key, new PackageAggregationInfo() {
                        PackageExitDefinitionInfo = packageAggregationInfo.PackageExitDefinitionInfo,
                        PackageItems = new List<PackageInfoModel>(),
                        AggregatePackageCode =
                            $"PK{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                    });*/
                    packageAggregationInfo.PackageItems.Clear();
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

            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.IsActive,
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
                var tryDequeue = _packageInfoItems.TryDequeue(out var packageInfo);
                if (tryDequeue && packageInfo is not null) {
                    //检查所有格口有没有对应的包裹

                    foreach (var info in _exitPackageAggregationItems) {
                        var packageInfoModel = info.Value.PackageItems.FirstOrDefault(f =>
                            f.PackageTimestamped.Equals(packageInfo.PackageInfo.Timestamp));
                        if (packageInfoModel is not null) {
                            info.Value.PackageItems.Remove(packageInfoModel);
                        }
                    }
                    //判断是否在历史里面

                    var aggregationInfo = _overexitPackageAggregationItems.FirstOrDefault(f =>
                        f.Key.Equals(packageInfo.PackageExitUpdateEvent?.ExitId ?? 0) &&
                        f.Value.PackagingTime.CompareTo(packageInfo.PackageInfo.CreateTime) > 0).Value;
                    if (aggregationInfo is not null) {
                        aggregationInfo.PackageItems.Add(new PackageInfoModel() {
                            BarCodeInfo = new BarCodeInfoModel() {
                                Barcode = packageInfo?.PackageInfo?.BarCodeInfo?.Barcode ?? string.Empty
                            },
                            PackageTimestamped = packageInfo?.PackageInfo?.Timestamp ?? 0
                        });

                        NLog.LogManager.GetCurrentClassLogger().Error($"{packageInfo?.PackageInfo?.BarCodeInfo?.Barcode} 加入历史包");
                    }
                    else {
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
                }

                await Task.Delay(30, stoppingToken);
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
    }
}