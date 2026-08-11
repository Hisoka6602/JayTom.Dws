using System.Linq;
using System.Diagnostics;
using JayTom.Dws.Abstractions.Imaging;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using System.Collections.Concurrent;
using System.Threading;
using JayTom.Dws.Domain.DownstreamProtocols;

namespace JayTom.Dws.Domain.Manager {

    public static class PackageInfoManager {
        private static readonly ConcurrentDictionary<DateTime, PackageInfo> _packageInfos = new();

        /// <summary>
        /// 包裹移除
        /// </summary>
        public static event EventHandler<PackageRemovedEventArgs>? PackageRemoved;

        /// <summary>
        /// 包裹完成
        /// </summary>
        public static event EventHandler<PackageCompletedEventArgs>? PackageCompleted;

        public static void OnPackageRemoved(PackageRemovedEventArgs e) {
            try {
                PackageRemoved?.Invoke(typeof(PackageInfoManager), e);
            }
            finally {
                e.RemovedPackage.TakeImage()?.Dispose();
                e.RemovedPackage.DisposeTimers();
            }
        }

        public static void OnPackageCompleted(PackageCompletedEventArgs e) {
            PackageCompleted?.Invoke(typeof(PackageInfoManager), e);
        }

        //public static ConcurrentDictionary<DateTime, PackageInfo> GetPackageInfos() => _packageInfos;

        /// <summary>
        /// 添加包裹
        /// </summary>
        /// <param name="package"></param>
        /// <param name="removeTimers"></param>
        public static void AddPackage(PackageInfo package, List<PackageTimer> removeTimers) {
            lock (package.SyncRoot) {
                if (!_packageInfos.TryAdd(package.CreateTime, package)) {
                    throw new InvalidOperationException(
                        $"包裹创建时间键重复，未加入运行会话：{package.CreateTime:O}");
                }

                package.StartTimers(_packageInfos, removeTimers);
            }
        }

        /// <summary>
        /// 移除包裹
        /// </summary>
        /// <param name="createTime"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static bool RemovePackage(DateTime createTime, string description = "手动移除") {
            if (!_packageInfos.TryGetValue(createTime, out var current)) {
                return false;
            }

            PackageInfo? info;
            bool tryRemove;
            lock (current.SyncRoot) {
                tryRemove = ((ICollection<KeyValuePair<DateTime, PackageInfo>>)_packageInfos)
                    .Remove(new KeyValuePair<DateTime, PackageInfo>(createTime, current));
                info = tryRemove ? current : null;
            }
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
            foreach (var pair in _packageInfos.OrderBy(item => item.Key)) {
                PackageInfo? removedPackage = null;
                lock (pair.Value.SyncRoot) {
                    if (!predicate(pair)) {
                        continue;
                    }
                    if (((ICollection<KeyValuePair<DateTime, PackageInfo>>)_packageInfos)
                        .Remove(pair)) {
                        removedPackage = pair.Value;
                    }
                }

                if (removedPackage is not null) {
                    OnPackageRemoved(new PackageRemovedEventArgs(removedPackage, description));
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 包裹是否存在
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static bool PackageExists(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            foreach (var pair in _packageInfos) {
                lock (pair.Value.SyncRoot) {
                    if (predicate(pair)) {
                        return true;
                    }
                }
            }

            return false;
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

            PackageInfo? result = null;
            var resultKey = DateTime.MaxValue;
            foreach (var pair in _packageInfos) {
                lock (pair.Value.SyncRoot) {
                    if (pair.Key < resultKey && predicate(pair)) {
                        result = pair.Value;
                        resultKey = pair.Key;
                    }
                }
            }

            return result;
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

            PackageInfo? result = null;
            var resultKey = DateTime.MinValue;
            foreach (var pair in _packageInfos) {
                lock (pair.Value.SyncRoot) {
                    if (pair.Key > resultKey && predicate(pair)) {
                        result = pair.Value;
                        resultKey = pair.Key;
                    }
                }
            }

            return result;
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
        public static List<PackageInfo> GetPackages(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            var packages = new List<PackageInfo>();
            foreach (var pair in _packageInfos.OrderBy(item => item.Key)) {
                lock (pair.Value.SyncRoot) {
                    if (predicate(pair)) {
                        packages.Add(pair.Value);
                    }
                }
            }

            return packages;
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
            foreach (var key in _packageInfos.Keys) {
                if (!_packageInfos.TryGetValue(key, out var current)) {
                    continue;
                }

                PackageInfo? info = null;
                lock (current.SyncRoot) {
                    if (((ICollection<KeyValuePair<DateTime, PackageInfo>>)_packageInfos)
                        .Remove(new KeyValuePair<DateTime, PackageInfo>(key, current))) {
                        info = current;
                    }
                }

                if (info is not null) {
                    OnPackageRemoved(new PackageRemovedEventArgs(info, "清空全部包裹"));
                }
            }
        }

        /// <summary>
        /// 完成包裹
        /// </summary>
        /// <param name="predicate"></param>

        public static void CompletedPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            var packageInfo = GetPackage(predicate);
            if (packageInfo == null) return;

            lock (packageInfo.SyncRoot) {
                if (!_packageInfos.TryGetValue(packageInfo.CreateTime, out var current) ||
                    !ReferenceEquals(current, packageInfo)) {
                    return;
                }
                if (packageInfo.IsCompleted) return;
                if (packageInfo.VolumeInfo is null ||
                    packageInfo.WeightInfo is null ||
                    packageInfo.BarCodeInfo is null) {
                    return;
                }
                if (packageInfo.LinkedCarCount <= 0) {
                    packageInfo.LinkedCarCount = 1;
                }
                packageInfo.MarkCompleted();
            }

            OnPackageCompleted(new PackageCompletedEventArgs(packageInfo, "包裹信息填充完成"));
        }
    }

    public class PackageInfo {
        /// <summary>
        /// 保护包裹内部的可变字段和集合。ConcurrentDictionary 只能保护容器，
        /// 不能保护存放在其中的 PackageInfo 实例。
        /// </summary>
        public object SyncRoot { get; } = new();

        /// <summary>
        /// 包裹创建时间
        /// </summary>
        public DateTime CreateTime { get; init; } = DateTime.Now;

        /// <summary>获取包裹创建时的单调时钟时间戳，仅用于进程内耗时判断。</summary>
        public long CreatedAtMonotonicTimestamp { get; } = Stopwatch.GetTimestamp();

        /// <summary>
        /// 包裹运行期标识
        /// </summary>
        public long Id { get; set; }

        /// <summary>获取或设置兼容旧调用点的包裹标识。</summary>
        public long Guid {
            get => Id;
            set => Id = value;
        }

        /// <summary>
        /// 条码图片
        /// </summary>
        public ImageHandle? Image { get; set; }

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
        /// 格口信息
        /// </summary>
        public virtual ExitInfoModel? ExitInfo { get; set; }

        /// <summary>
        /// 是否已完成(完成输出、上传、但未从集合删除)
        /// </summary>
        private int _isCompleted;
        /// <summary>
        /// 获取或设置包裹信息是否已经填充完成。
        /// </summary>
        public bool IsCompleted {
            get => Volatile.Read(ref _isCompleted) != 0;
        }

        /// <summary>以不可逆方式将包裹标记为完成。</summary>
        public void MarkCompleted() => Volatile.Write(ref _isCompleted, 1);

        /// <summary>
        /// 是否完成存图
        /// </summary>
        private int _isSavedImage;
        /// <summary>
        /// 获取或设置包裹图像是否已经转交保存流程。
        /// </summary>
        public bool IsImageSaveRequested {
            get => Volatile.Read(ref _isSavedImage) != 0;
            init {
                if (value) {
                    Volatile.Write(ref _isSavedImage, 1);
                }
            }
        }

        /// <summary>标记图像已经转交存图流程。</summary>
        public void MarkImageSaveRequested() => Volatile.Write(ref _isSavedImage, 1);

        /// <summary>在存图请求失败、允许重试时清除转交标记。</summary>
        public void ResetImageSaveRequest() => Volatile.Write(ref _isSavedImage, 0);

        /// <summary>
        /// 需要扣除的长度
        /// </summary>
        public decimal LengthToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的宽度
        /// </summary>
        public decimal WidthToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的高度
        /// </summary>
        public decimal HeightToDeduct { get; set; }

        /// <summary>
        /// 需要扣除的体积
        /// </summary>
        public decimal VolumeToDeduct { get; set; }

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
        /// <remarks><see langword="null"/> 表示尚未执行叠包检测。</remarks>
        public bool? IsStackedPackage { get; set; }

        /// <summary>
        /// 包裹时间戳（毫秒）
        /// </summary>
        public long TimestampMilliseconds { get; set; }

        /// <summary>获取或设置兼容旧调用点的毫秒时间戳。</summary>
        public long Timestamp {
            get => TimestampMilliseconds;
            set => TimestampMilliseconds = value;
        }

        /// <summary>
        /// 包裹异常类型
        /// </summary>
        public List<SortingExceptionReturnType> SortingExceptionReturnTypes { get; set; } = new();

        /// <summary>
        /// 供包台信号类型
        /// </summary>
        public List<SupplyCounterPackageSignal> SupplyCounterPackageSignalItem { get; set; } = new();

        /// <summary>
        /// 接口返回内容
        /// </summary>
        /// <summary>
        /// 灰度仪信息
        /// </summary>
        public GrayscaleResult? GrayscaleResultInfo { get; set; }

        /// <summary>
        /// 联动车辆
        /// </summary>
        public int LinkedCarCount { get; set; } = 0;

        /// <summary>
        /// 分拣异常信息
        /// </summary>
        public string? SortingExceptionInfo { get; set; }

        /// <summary>
        /// 移除包裹计时器
        /// </summary>
        private List<PackageRemoveTimer> PackageRemoveTimers { get; } = new();

        /// <summary>
        /// 完成包裹计时器
        /// </summary>
        private List<PackCompletedTimer> PackCompletedTimers { get; } = new();

        /// <summary>
        /// 包裹赋值计时器
        /// </summary>
        private List<PackageAssignmentTimer> PackageAssignmentTimers { get; } = new();

        /// <summary>
        /// 其他
        /// </summary>

        public object? Other { get; set; }

        private readonly System.Threading.Lock _removalLock = new();

        public void StartTimers(ConcurrentDictionary<DateTime, PackageInfo> packageInfos, List<PackageTimer> removeTimers) {
            lock (SyncRoot) {
                foreach (var timer in removeTimers) {
                    switch (timer) {
                        case PackageRemoveTimer packageRemoveTimer:
                            packageRemoveTimer.PackageRemovalTimer = new Timer(RemoveFromCollection, new Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageRemoveTimer>(packageInfos, packageRemoveTimer), packageRemoveTimer.RemovalTimeSpan, Timeout.InfiniteTimeSpan);
                            PackageRemoveTimers.Add(packageRemoveTimer);
                            break;

                        case PackCompletedTimer packCompletedTimer:
                            packCompletedTimer.CompletTimer = new Timer(CompletedCollection, new Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackCompletedTimer>(packageInfos, packCompletedTimer), packCompletedTimer.CompletTimeSpan, Timeout.InfiniteTimeSpan);
                            PackCompletedTimers.Add(packCompletedTimer);
                            break;

                        case PackageAssignmentTimer packageAssignmentTimer:
                            packageAssignmentTimer.AssignmentTimer = new Timer(AssignmentCollection, new Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageAssignmentTimer>(packageInfos, packageAssignmentTimer), packageAssignmentTimer.AssignmentTimeSpan, Timeout.InfiniteTimeSpan);
                            PackageAssignmentTimers.Add(packageAssignmentTimer);
                            break;
                    }
                }
            }
        }

        public ImageHandle? TakeImage() {
            lock (SyncRoot) {
                var image = Image;
                Image = null;
                return image;
            }
        }

        public void DisposeTimers() {
            lock (SyncRoot) {
                PackageRemoveTimers.ForEach(timer => timer.PackageRemovalTimer?.Dispose());
                PackCompletedTimers.ForEach(timer => timer.CompletTimer?.Dispose());
                PackageAssignmentTimers.ForEach(timer => timer.AssignmentTimer?.Dispose());
                PackageRemoveTimers.Clear();
                PackCompletedTimers.Clear();
                PackageAssignmentTimers.Clear();
            }
        }

        private void RemoveFromCollection(object? state) {
            if (state is not null) {
                var (packageInfos, removeTimer) = (Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageRemoveTimer>)state;
                if (!packageInfos.TryGetValue(CreateTime, out var current)) {
                    removeTimer.PackageRemovalTimer?.Dispose();
                    return;
                }

                lock (current.SyncRoot) {
                    if (removeTimer.Predicate is not null &&
                        !removeTimer.Predicate(
                            new KeyValuePair<DateTime, PackageInfo>(CreateTime, current))) {
                        removeTimer.PackageRemovalTimer?.Dispose();
                        return;
                    }
                }

                if (PackageInfoManager.RemovePackage(CreateTime, removeTimer.Description)) {
                    removeTimer.PackageRemovalTimer?.Dispose();
                }
            }
        }

        private void CompletedCollection(object? state) {
            if (state is not null) {
                var (packageInfos, removeTimer) = (Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackCompletedTimer>)state;
                if (packageInfos.TryGetValue(CreateTime, out var current)) {
                    lock (current.SyncRoot) {
                    if (removeTimer.Predicate is not null &&
                        !removeTimer.Predicate(
                            new KeyValuePair<DateTime, PackageInfo>(CreateTime, current))) {
                        removeTimer.CompletTimer?.Dispose();
                        return;
                        }
                    }

                    PackageInfoManager.CompletedPackage(pair => pair.Key.Equals(CreateTime));
                    removeTimer.CompletTimer?.Dispose();
                }
            }
        }

        private void AssignmentCollection(object? state) {
            if (state is not null) {
                // 解构传递进来的状态，获取包信息字典和赋值计时器
                var (packageInfos, assignmentTimer) = (Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageAssignmentTimer>)state;

                if (assignmentTimer.Predicate is not null) {
                    if (packageInfos.TryGetValue(CreateTime, out var current) &&
                        assignmentTimer.AssignmentCallback is not null) {
                        lock (current.SyncRoot) {
                            var pair = new KeyValuePair<DateTime, PackageInfo>(
                                CreateTime,
                                current);
                            if (!assignmentTimer.Predicate(pair)) {
                                assignmentTimer.AssignmentTimer?.Dispose();
                                return;
                            }
                            var keepTimer = assignmentTimer.AssignmentCallback(current);
                            if (keepTimer) {
                                assignmentTimer.AssignmentTimer?.Change(
                                    assignmentTimer.AssignmentTimeSpan,
                                    Timeout.InfiniteTimeSpan);
                                return;
                            }
                        }
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
        internal Timer? AssignmentTimer { get; set; }

        // 回调方法，接收PackageInfo参数，返回是否继续保留计时器
        public Func<PackageInfo, bool>? AssignmentCallback { get; set; }
    }

    /// <summary>
    /// 移除包裹计时器
    /// </summary>
    public class PackageRemoveTimer : PackageTimer {
        public TimeSpan RemovalTimeSpan { get; set; }
        internal Timer? PackageRemovalTimer { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 完成计时器
    /// </summary>
    public class PackCompletedTimer : PackageTimer {
        public TimeSpan CompletTimeSpan { get; set; }
        internal Timer? CompletTimer { get; set; }
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
