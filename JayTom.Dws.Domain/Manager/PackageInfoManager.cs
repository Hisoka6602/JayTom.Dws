using NLog;
using System.Drawing;
using System.Diagnostics;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using System.Collections.Concurrent;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;

namespace JayTom.Dws.Domain.Manager {

    public static class PackageInfoManager {
        private static ConcurrentDictionary<DateTime, PackageInfo> _packageInfos = new();

        /// <summary>
        /// 包裹移除
        /// </summary>
        public static event EventHandler<PackageRemovedEventArgs>? PackageRemoved;

        /// <summary>
        /// 包裹完成
        /// </summary>
        public static event EventHandler<PackageCompletedEventArgs>? PackageCompleted;

        public static void OnPackageRemoved(PackageRemovedEventArgs e) {
            PackageRemoved?.Invoke(null, e);
        }

        public static void OnPackageCompleted(PackageCompletedEventArgs e) {
            PackageCompleted?.Invoke(null, e);
        }

        //public static ConcurrentDictionary<DateTime, PackageInfo> GetPackageInfos() => _packageInfos;

        /// <summary>
        /// 添加包裹
        /// </summary>
        /// <param name="package"></param>
        /// <param name="removeTimers"></param>
        public static void AddPackage(PackageInfo package, List<PackageRemoveTimer> removeTimers) {
            _packageInfos.TryAdd(package.CreateTime, package);
            package.StartRemovalTimers(_packageInfos, removeTimers);
        }

        /// <summary>
        /// 移除包裹
        /// </summary>
        /// <param name="createTime"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static bool RemovePackage(DateTime createTime, string description = "手动移除") {
            var tryRemove = _packageInfos.TryRemove(createTime, out var info);
            if (tryRemove && info is not null) {
                OnPackageRemoved(new PackageRemovedEventArgs(info, description));
            }

            return tryRemove;
        }

        /// <summary>
        /// 移除包裹
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static bool RemovePackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate, string description = "手动移除") {
            var (key, value) = _packageInfos.FirstOrDefault(predicate);
            if (value is not null) {
                var tryRemove = _packageInfos.TryRemove(key, out var info);
                if (tryRemove && info is not null) {
                    OnPackageRemoved(new PackageRemovedEventArgs(info, description));
                }
                return tryRemove;
            }

            return false;
        }

        /// <summary>
        /// 包裹是否存在
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static bool PackageExists(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            return _packageInfos.Any(predicate);
        }

        /// <summary>
        /// 获取包裹
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static PackageInfo? GetPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            // 检查 _packageInfos 是否为空
            if (_packageInfos.IsEmpty) {
                return null;
            }

            try {
                // 尝试找到第一个符合条件的键值对
                var result = _packageInfos.OrderBy(o => o.Key).FirstOrDefault(predicate);

                // 检查 result 是否为默认值
                if (EqualityComparer<KeyValuePair<DateTime, PackageInfo>>.Default.Equals(result, default)) {
                    return null;
                }

                var (key, value) = result;

                if (value == null) {
                    return null;
                }

                // 尝试获取包信息
                return _packageInfos.GetValueOrDefault(key);
            }
            catch (Exception ex) {
                NLog.LogManager.GetCurrentClassLogger().Error($"An error occurred in GetPackage: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取最后一个
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static PackageInfo? GetLastPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            // 确保 _packageInfos 是线程安全的，例如 ConcurrentDictionary
            if (_packageInfos.IsEmpty) {
                return null;
            }

            try {
                // 逆向遍历集合，找到第一个符合条件的键值对
                foreach (var kvp in _packageInfos.OrderByDescending(k => k.Key)) {
                    if (predicate(kvp)) {
                        return kvp.Value;
                    }
                }
                return null;
            }
            catch (Exception ex) {
                NLog.LogManager.GetCurrentClassLogger().Error($"An error occurred in GetPackage: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取包裹
        /// </summary>
        /// <param name="createTime"></param>
        /// <returns></returns>
        public static PackageInfo? GetPackage(DateTime createTime) {
            _packageInfos.TryGetValue(createTime, out var package);
            return package;
        }

        /// <summary>
        /// 获取包裹
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static List<PackageInfo>? GetPackages(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            return _packageInfos?.Any(predicate) == true ?
                _packageInfos.Where(predicate)?.
                    OrderBy(o => o.Key)?.
                    Select(s => s.Value)
                    ?.ToList()
                : new List<PackageInfo>();
        }

        /// <summary>
        /// 获取包裹数量
        /// </summary>
        /// <returns></returns>
        public static int GetPackageCount() {
            return _packageInfos.Count;
        }

        /// <summary>
        /// 清空包裹
        /// </summary>
        public static void ClearAllPackages() {
            _packageInfos.Clear();
        }

        /// <summary>
        /// 完成包裹
        /// </summary>
        /// <param name="predicate"></param>

        public static void CompletedPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            var packageInfo = GetPackage(predicate);
            if (packageInfo == null) return;
            packageInfo.VolumeInfo ??= new VolumeInfoModel();
            packageInfo.WeightInfo ??= new WeightInfoModel();
            packageInfo.BarCodeInfo ??= new BarCodeInfoModel();
            packageInfo.GrayscaleResultInfo ??= new GrayscaleResult();
            packageInfo.IsStackedPackage ??= false;
            packageInfo.IsCompleted = true;
            OnPackageCompleted(new PackageCompletedEventArgs(packageInfo, string.Empty));
        }
    }

    public class PackageInfo {

        /// <summary>
        /// 包裹创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Guid
        /// </summary>
        public long Guid { get; set; }

        /// <summary>
        /// 条码图片
        /// </summary>
        public Image? Image { get; set; }

        /// <summary>
        /// 条码信息
        /// </summary>
        public BarCodeInfoModel? BarCodeInfo { get; set; }

        /// <summary>
        /// 体积信息
        /// </summary>
        public VolumeInfoModel? VolumeInfo { get; set; }

        /// <summary>
        /// 称重信息
        /// </summary>
        public WeightInfoModel? WeightInfo { get; set; }

        /// <summary>
        /// 是否已完成(完成输出、上传、但未从集合删除)
        /// </summary>
        public bool IsCompleted;

        /// <summary>
        /// 是否完成存图
        /// </summary>
        public bool IsSavedImage;

        /// <summary>
        /// 需要扣除的长度
        /// </summary>
        public float LengthToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的宽度
        /// </summary>
        public float WidthToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的高度
        /// </summary>
        public float HeightToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的体积
        /// </summary>
        public float VolumeToDeduct { get; set; }

        /// <summary>
        /// 创建包裹指令
        /// </summary>
        public string PackageCreationInstruction { get; set; } = string.Empty;

        /// <summary>
        /// 是否由下位机创建
        /// </summary>
        public bool IsCreatedByLowerMachine { get; set; }

        /// <summary>
        /// 全景图信息
        /// </summary>
        public List<PanoramaCameraImageInfo> PanoramaCameraImageInfo { get; set; } = new();

        /// <summary>
        /// 是否叠包
        /// </summary>
        public bool? IsStackedPackage { get; set; }

        /// <summary>
        /// 包裹时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /*/// <summary>
        /// 包裹异常信息
        /// </summary>
        public string PackageExceptionMsg { get; set; } = "分拣成功";

        /// <summary>
        /// 包裹异常状态
        /// </summary>
        public int PackageExceptionStatus { get; set; } = 0;*/

        /// <summary>
        /// 包裹异常类型
        /// </summary>
        public List<SortingExceptionReturnType> SortingExceptionReturnTypes { get; set; } = new();

        /// <summary>
        /// 供包台信号类型
        /// </summary>
        public List<SupplyCounterPackageSignal> SupplyCounterPackageSignalItem { get; set; } = new();

        /// <summary>
        /// 灰度仪信息
        /// </summary>
        public GrayscaleResult? GrayscaleResultInfo { get; set; }

        /// <summary>
        /// 联动车辆
        /// </summary>
        public int LinkedCarCount { get; set; } = 0;

        /// <summary>
        /// 移除包裹计时器
        /// </summary>
        public List<PackageRemoveTimer>? PackageRemoveTimers { get; private set; } = new();

        private readonly object _removalLock = new();

        public void StartRemovalTimers(ConcurrentDictionary<DateTime, PackageInfo> packageInfos, List<PackageRemoveTimer> removeTimers) {
            foreach (var timer in removeTimers) {
                timer.PackageRemovalTimer = new Timer(RemoveFromCollection, new Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageRemoveTimer>(packageInfos, timer), timer.RemovalTimeSpan, Timeout.InfiniteTimeSpan);
                PackageRemoveTimers?.Add(timer);
            }
        }

        private void RemoveFromCollection(object? state) {
            if (state is not null) {
                var (packageInfos, removeTimer) = (Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageRemoveTimer>)state;
                if (removeTimer.Predicate is not null) {
                    var any = packageInfos.Where(removeTimer.Predicate)?.All(a => !a.Key.Equals(CreateTime));
                    if (any == true) {
                        removeTimer.PackageRemovalTimer?.Dispose();
                        return;
                    }
                }
                if (packageInfos.TryRemove(CreateTime, out var removedPackage)) {
                    removeTimer.PackageRemovalTimer?.Dispose();
                    PackageInfoManager.OnPackageRemoved(new PackageRemovedEventArgs(removedPackage, removeTimer.Description));
                }
            }
        }
    }

    /// <summary>
    /// 移除包裹计时器
    /// </summary>
    public class PackageRemoveTimer {
        public TimeSpan RemovalTimeSpan { get; set; }
        public Timer? PackageRemovalTimer { get; set; }
        public Func<KeyValuePair<DateTime, PackageInfo>, bool>? Predicate { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class PackageRemovedEventArgs : EventArgs {
        public PackageInfo RemovedPackage { get; }
        public string Description { get; }

        public PackageRemovedEventArgs(PackageInfo removedPackage, string description) {
            RemovedPackage = removedPackage;
            Description = description;
        }
    }

    public class PackageCompletedEventArgs : EventArgs {
        public PackageInfo CompletedPackage { get; }
        public string Description { get; }

        public PackageCompletedEventArgs(PackageInfo completedPackage, string description) {
            CompletedPackage = completedPackage;
            Description = description;
        }
    }
}