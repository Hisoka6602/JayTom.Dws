using NLog;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using JayTom.Dws.Interface;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using JayTom.Dws.Domain.Interface;
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
        public static void AddPackage(PackageInfo package, List<PackageTimer> removeTimers) {
            _packageInfos.TryAdd(package.CreateTime, package);
            package.StartTimers(_packageInfos, removeTimers);
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
                return (from kvp in _packageInfos.OrderByDescending(k => k.Key) where predicate(kvp) select kvp.Value).FirstOrDefault();
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
            foreach (var (key, value) in _packageInfos) {
                _packageInfos.Remove(key, out var info);
            }

            _packageInfos.Clear();
        }

        /// <summary>
        /// 完成包裹
        /// </summary>
        /// <param name="predicate"></param>

        public static void CompletedPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            var packageInfo = GetPackage(predicate);
            if (packageInfo == null || packageInfo.IsCompleted) return;
            packageInfo.LinkedCarCount = 1;
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
        /// 条码信息
        /// </summary>
        public BarCodeInfoModel? BarCodeInfo { get; set; }

        /// <summary>
        /// 节点信息
        /// </summary>
        public List<NodeInfoModel> NodeInfos { get; set; } = new();

        /// <summary>
        /// 格口信息
        /// </summary>
        public virtual ExitInfoModel? ExitInfo { get; set; }

        /// <summary>
        /// 格口信息
        /// </summary>
        public virtual ExitInfoModel? ExitInfo { get; set; }

        /// <summary>
        /// 是否已完成(完成输出、上传、但未从集合删除)
        /// </summary>
        public bool IsCompleted;

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
        /// 包裹时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 包裹异常类型
        /// </summary>
        public List<SortingExceptionReturnType> SortingExceptionReturnTypes { get; set; } = new();

        /// <summary>
        /// 接口返回内容
        /// </summary>
        public List<UploadResponse> UploadResponses { get; set; } = new();

        /// <summary>
        /// 接口返回内容
        /// </summary>
        public List<UploadResponse> UploadResponses { get; set; } = new();

        /// <summary>
        /// 灰度仪信息
        /// </summary>
        public int LinkedCarCount { get; set; } = 0;

        /// <summary>
        /// 分拣异常信息
        /// </summary>
        public string? SortingExceptionInfo { get; set; }

        /// <summary>
        /// 分拣异常信息
        /// </summary>
        public string? SortingExceptionInfo { get; set; }

        /// <summary>
        /// 移除包裹计时器
        /// </summary>
        public List<PackageRemoveTimer>? PackageRemoveTimers { get; private set; } = new();

        /// <summary>
        /// 完成包裹计时器
        /// </summary>
        public List<PackCompletedTimer>? PackCompletedTimers { get; private set; } = new();

        /// <summary>
        /// 包裹赋值计时器
        /// </summary>
        public List<PackageAssignmentTimer>? PackageAssignmentTimers { get; private set; } = new();

        /// <summary>
        /// 其他
        /// </summary>

        public object? Other { get; set; }

        private readonly object _removalLock = new();

        public void StartTimers(ConcurrentDictionary<DateTime, PackageInfo> packageInfos, List<PackageTimer> removeTimers) {
            foreach (var timer in removeTimers) {
                switch (timer) {
                    case PackageRemoveTimer packageRemoveTimer:
                        packageRemoveTimer.PackageRemovalTimer = new Timer(RemoveFromCollection, new Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageRemoveTimer>(packageInfos, packageRemoveTimer), packageRemoveTimer.RemovalTimeSpan, Timeout.InfiniteTimeSpan);
                        PackageRemoveTimers?.Add(packageRemoveTimer);
                        break;

                    case PackCompletedTimer packCompletedTimer:
                        packCompletedTimer.CompletTimer = new Timer(CompletedCollection, new Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackCompletedTimer>(packageInfos, packCompletedTimer), packCompletedTimer.CompletTimeSpan, Timeout.InfiniteTimeSpan);
                        PackCompletedTimers?.Add(packCompletedTimer);
                        break;

                    case PackageAssignmentTimer packageAssignmentTimer:
                        packageAssignmentTimer.AssignmentTimer = new Timer(AssignmentCollection, new Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageAssignmentTimer>(packageInfos, packageAssignmentTimer), packageAssignmentTimer.AssignmentTimeSpan, Timeout.InfiniteTimeSpan);
                        PackageAssignmentTimers?.Add(packageAssignmentTimer);
                        break;
                }
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

        private void CompletedCollection(object? state) {
            if (state is not null) {
                var (packageInfos, removeTimer) = (Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackCompletedTimer>)state;
                if (removeTimer.Predicate is not null) {
                    var valuePair = packageInfos.Where(removeTimer.Predicate)
                        ?.FirstOrDefault(f => f.Value != null && f.Value.CreateTime.Equals(CreateTime));
                    if (valuePair is { Value: not null }) {
                        PackageInfoManager.CompletedPackage(f => f.Value != null && f.Value.CreateTime.Equals(CreateTime));
                        removeTimer.CompletTimer?.Dispose();
                    }
                }
            }
        }

        private void AssignmentCollection(object? state) {
            if (state is not null) {
                // 解构传递进来的状态，获取包信息字典和赋值计时器
                var (packageInfos, assignmentTimer) = (Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageAssignmentTimer>)state;

                if (assignmentTimer.Predicate is not null) {
                    // 根据Predicate找到匹配的包信息
                    var valuePair = packageInfos.Where(assignmentTimer.Predicate)
                        ?.FirstOrDefault(f => f.Value != null && f.Value.CreateTime.Equals(CreateTime));

                    if (valuePair is { Value: not null } &&
                        assignmentTimer.AssignmentCallback is not null) {
                        // 执行赋值逻辑
                        assignmentTimer.AssignmentCallback(valuePair.Value.Value);
                        // 停止并释放计时器
                        assignmentTimer.AssignmentTimer?.Dispose();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 赋值计时器
    /// </summary>
    public class PackageAssignmentTimer : PackageTimer {
        public TimeSpan AssignmentTimeSpan { get; set; }
        public Timer? AssignmentTimer { get; set; }

        // 回调方法，接收PackageInfo参数，返回是否继续保留计时器
        public Func<PackageInfo, bool>? AssignmentCallback { get; set; }
    }

    /// <summary>
    /// 移除包裹计时器
    /// </summary>
    public class PackageRemoveTimer : PackageTimer {
        public TimeSpan RemovalTimeSpan { get; set; }
        public Timer? PackageRemovalTimer { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 完成计时器
    /// </summary>
    public class PackCompletedTimer : PackageTimer {
        public TimeSpan CompletTimeSpan { get; set; }
        public Timer? CompletTimer { get; set; }
    }

    public class PackageTimer {
        public Func<KeyValuePair<DateTime, PackageInfo>, bool>? Predicate { get; set; }
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