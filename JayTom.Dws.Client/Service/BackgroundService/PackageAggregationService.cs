using System;
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
        private ConcurrentQueue<PackageInfoModel> _packageInfoItems = new();

        public PackageAggregationService(IPackageRepository packageRepository,
            IExitMonitor exitMonitor, IPackageExitDefinitionRepository packageExitDefinitionRepository) {
            _packageRepository = packageRepository;
            _exitMonitor = exitMonitor;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            EventAggregator.Instance.Subscribe<CallBackPackageInfo>(async item => {
                if (item is CallBackPackageInfo info) {
                    var (key, value) = await _packageRepository.FirstOrDefaultInfo(f => f.PackageCreateTime.Equals(info.PackageCreateTime));
                    if (key && value is not null) {
                        //添加包裹到队列
                        _packageInfoItems.Enqueue(value);
                    }
                }
            });
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item => {
                if (item is ApplicationStatusChanged info) {
                    _packageInfoItems.Clear();
                }
            });
            _exitMonitor.LockExitEvent += (sender, model) => {
                var packageAggregationInfo = _exitPackageAggregationItems
                    .FirstOrDefault(f => f.Key.Equals(model.Id)).Value;

                if (packageAggregationInfo is not null) {
                    packageAggregationInfo.PackagingTime = DateTime.Now;
                    EventAggregator.Instance.Publish(packageAggregationInfo);
                }
            };
            _exitMonitor.UnLockExitEvent += (sender, model) => {
                var packageAggregationInfo = _exitPackageAggregationItems
                    .FirstOrDefault(f => f.Key.Equals(model.Id)).Value;

                packageAggregationInfo?.PackageItems.Clear();
                //清空
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
                    var packageAggregationInfo = _exitPackageAggregationItems
                        .FirstOrDefault(f => f.Key.Equals(packageInfo.ExitInfo?.PhysicalExitId ?? 0)).Value;
                    if (packageAggregationInfo is not null) {
                        packageAggregationInfo.PackageItems.Add(packageInfo);
                    }
                    else {
                        _packageInfoItems.Enqueue(packageInfo);
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