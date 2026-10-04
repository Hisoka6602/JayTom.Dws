using System.Linq;
using System.Diagnostics;
using JayTom.Dws.Abstractions.Imaging;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Model;
using System.Collections.Concurrent;
using System.Threading;
using JayTom.Dws.Legacy.Contracts.DownstreamProtocols;
using JayTom.Dws.Domain.Packages;

namespace JayTom.Dws.Legacy.Contracts.Packages {

    public sealed class PackageSessionRegistry {
        private readonly ConcurrentDictionary<DateTime, PackageInfo> _packageInfos = new();
        /// <summary>维护稳定的包裹创建顺序，避免热路径每次查找都对并发快照重新排序。</summary>
        private readonly SortedSet<DateTime> _packageOrder = [];
        /// <summary>仅保存未赋值包裹，利用有序时间索引定位赋值窗口内的最近候选。</summary>
        private readonly SortedSet<DateTime> _unassignedPackageOrder = [];
        /// <summary>按下位机序号直接定位包裹，避免回调热路径扫描全部会话。</summary>
        private readonly Dictionary<long, DateTime> _packageIdIndex = [];
        /// <summary>直接定位同一次相机扫码已绑定的包裹，避免多条码判断扫描全部活动会话。</summary>
        private readonly Dictionary<(DateTime ScanTime, string SerialNumber), DateTime> _barcodeObservationIndex = [];
        /// <summary>保护创建顺序索引的一致性。</summary>
        private readonly object _packageOrderGate = new();
        /// <summary>保存可原子读取的当前包裹数量。</summary>
        private int _packageCount;

        /// <summary>
        /// 包裹移除
        /// </summary>
        public event EventHandler<PackageRemovedEventArgs>? PackageRemoved;

        /// <summary>
        /// 包裹完成
        /// </summary>
        public event EventHandler<PackageCompletedEventArgs>? PackageCompleted;

        private void OnPackageRemoved(PackageRemovedEventArgs e) {
            try {
                PackageRemoved?.Invoke(this, e);
            }
            finally {
                e.RemovedPackage.TakeImage()?.Dispose();
                e.RemovedPackage.DisposeTimers();
            }
        }

        private void OnPackageCompleted(PackageCompletedEventArgs e) {
            PackageCompleted?.Invoke(this, e);
        }

        //public static ConcurrentDictionary<DateTime, PackageInfo> GetPackageInfos() => _packageInfos;

        /// <summary>
        /// 添加包裹
        /// </summary>
        /// <param name="package"></param>
        /// <param name="removeTimers"></param>
        public void AddPackage(PackageInfo package, List<PackageTimer> removeTimers) {
            if (!TryAddPackage(package, removeTimers)) {
                throw new InvalidOperationException(
                    $"包裹创建时间键重复，未加入运行会话：{package.CreateTime:O}");
            }
        }

        /// <summary>尝试按创建时间键添加包裹，成功后才启动生命周期计时器。</summary>
        /// <param name="package">待添加包裹。</param>
        /// <param name="removeTimers">生命周期计时器。</param>
        /// <returns>是否成功添加。</returns>
        public bool TryAddPackage(PackageInfo package, List<PackageTimer> removeTimers) {
            lock (package.SyncRoot) {
                lock (_packageOrderGate) {
                    if (!_packageInfos.TryAdd(package.CreateTime, package)) {
                        return false;
                    }
                    _packageOrder.Add(package.CreateTime);
                    if (package.BarCodeInfo is null) {
                        _unassignedPackageOrder.Add(package.CreateTime);
                    }
                    if (package.Guid != 0) {
                        if (_packageIdIndex.TryGetValue(package.Guid, out var existingKey) &&
                            existingKey != package.CreateTime) {
                            _packageInfos.TryRemove(package.CreateTime, out _);
                            _packageOrder.Remove(package.CreateTime);
                            _unassignedPackageOrder.Remove(package.CreateTime);
                            return false;
                        }
                        _packageIdIndex[package.Guid] = package.CreateTime;
                    }
                    IndexBarcodeObservation(package);
                    Interlocked.Increment(ref _packageCount);
                }

                try {
                    package.StartTimers(_packageInfos, removeTimers, this);
                    return true;
                }
                catch {
                    // 计时器启动失败时不能留下一个永不按规则到期的半初始化会话。
                    ((ICollection<KeyValuePair<DateTime, PackageInfo>>)_packageInfos).Remove(
                        new KeyValuePair<DateTime, PackageInfo>(package.CreateTime, package));
                    lock (_packageOrderGate) {
                        _packageOrder.Remove(package.CreateTime);
                        _unassignedPackageOrder.Remove(package.CreateTime);
                        RemoveBarcodeObservation(package);
                        if (package.Guid != 0 &&
                            _packageIdIndex.TryGetValue(package.Guid, out var indexedKey) &&
                            indexedKey == package.CreateTime) {
                            _packageIdIndex.Remove(package.Guid);
                        }
                    }
                    Interlocked.Decrement(ref _packageCount);
                    package.DisposeTimers();
                    throw;
                }
            }
        }

        /// <summary>
        /// 移除包裹
        /// </summary>
        /// <param name="createTime"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public bool RemovePackage(DateTime createTime, string description = "手动移除") =>
            RemovePackage(createTime, description, null);

        /// <summary>在同一包裹锁内校验条件并移除包裹，避免过期回调删除已并发赋值的包裹。</summary>
        /// <param name="createTime">包裹创建时间。</param>
        /// <param name="description">移除原因。</param>
        /// <param name="predicate">需在锁内重新校验的移除条件。</param>
        /// <returns>是否成功移除。</returns>
        public bool RemovePackage(
            DateTime createTime,
            string description,
            Func<KeyValuePair<DateTime, PackageInfo>, bool>? predicate) =>
            RemovePackage(createTime, description, predicate, isDiscarded: false);

        /// <summary>保留内部移除通知，丢弃结果不触发常规业务输出。</summary>
        private bool RemovePackage(
            DateTime createTime,
            string description,
            Func<KeyValuePair<DateTime, PackageInfo>, bool>? predicate,
            bool isDiscarded) {
            if (!_packageInfos.TryGetValue(createTime, out var current)) {
                return false;
            }

            PackageInfo? info;
            bool tryRemove;
            lock (current.SyncRoot) {
                var pair = new KeyValuePair<DateTime, PackageInfo>(createTime, current);
                if (predicate is not null && !predicate(pair)) {
                    return false;
                }

                tryRemove = ((ICollection<KeyValuePair<DateTime, PackageInfo>>)_packageInfos).Remove(pair);
                info = tryRemove ? current : null;
            }
            if (tryRemove && info is not null) {
                Interlocked.Decrement(ref _packageCount);
                lock (_packageOrderGate) {
                    _packageOrder.Remove(createTime);
                    _unassignedPackageOrder.Remove(createTime);
                    RemoveBarcodeObservation(info);
                    if (info.Guid != 0 &&
                        _packageIdIndex.TryGetValue(info.Guid, out var indexedKey) &&
                        indexedKey == createTime) {
                        _packageIdIndex.Remove(info.Guid);
                    }
                }
                info.MarkRemoved();
                OnPackageRemoved(new PackageRemovedEventArgs(info, description, isDiscarded));
            }

            return tryRemove;
        }

        /// <summary>
        /// 移除包裹
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public bool RemovePackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate, string description = "手动移除") {
            foreach (var key in GetOrderedKeys()) {
                if (!_packageInfos.TryGetValue(key, out var value)) {
                    continue;
                }
                var pair = new KeyValuePair<DateTime, PackageInfo>(key, value);
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
                    Interlocked.Decrement(ref _packageCount);
                    lock (_packageOrderGate) {
                        _packageOrder.Remove(pair.Key);
                        _unassignedPackageOrder.Remove(pair.Key);
                        RemoveBarcodeObservation(removedPackage);
                        if (removedPackage.Guid != 0 &&
                            _packageIdIndex.TryGetValue(removedPackage.Guid, out var indexedKey) &&
                            indexedKey == pair.Key) {
                            _packageIdIndex.Remove(removedPackage.Guid);
                        }
                    }
                    removedPackage.MarkRemoved();
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
        public bool PackageExists(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            foreach (var key in GetOrderedKeys()) {
                if (!_packageInfos.TryGetValue(key, out var package)) {
                    continue;
                }
                var pair = new KeyValuePair<DateTime, PackageInfo>(key, package);
                lock (package.SyncRoot) {
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
        public PackageInfo? GetPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            // 检查 _packageInfos 是否为空
            if (_packageInfos.IsEmpty) {
                return null;
            }

            foreach (var key in GetOrderedKeys()) {
                if (!_packageInfos.TryGetValue(key, out var package)) {
                    continue;
                }
                var pair = new KeyValuePair<DateTime, PackageInfo>(key, package);
                lock (package.SyncRoot) {
                    if (predicate(pair)) {
                        return package;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取最后一个
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public PackageInfo? GetLastPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            // 确保 _packageInfos 是线程安全的，例如 ConcurrentDictionary
            if (_packageInfos.IsEmpty) {
                return null;
            }

            var keys = GetOrderedKeys();
            for (var index = keys.Length - 1; index >= 0; index--) {
                var key = keys[index];
                if (!_packageInfos.TryGetValue(key, out var package)) {
                    continue;
                }
                var pair = new KeyValuePair<DateTime, PackageInfo>(key, package);
                lock (package.SyncRoot) {
                    if (predicate(pair)) {
                        return package;
                    }
                }
            }
            return null;
        }

        /// <summary>以设备或 TCP 观测时间为基准，原子选择并更新一个未赋值包裹。</summary>
        /// <param name="observedAt">条码被设备或通讯层观测到的时间。</param>
        /// <param name="queueOrder">区间内候选包裹按用户选择的创建时间正序或倒序匹配。</param>
        /// <param name="enforceAssignmentInterval">是否校验赋值时间窗口。</param>
        /// <param name="minimumAssignmentMilliseconds">赋值时间窗口下限，单位毫秒。</param>
        /// <param name="maximumAssignmentMilliseconds">赋值时间窗口上限，单位毫秒。</param>
        /// <param name="emptyPackageExpiryMilliseconds">空包裹删除时间，到达该时间后不再允许赋值。</param>
        /// <param name="processingAt">程序实际处理条码的时间，用于阻止排队后的过期条码错配。</param>
        /// <param name="assignment">在包裹锁内执行的赋值操作。</param>
        /// <returns>赋值成功的包裹，无合适包裹时返回空。</returns>
        public PackageInfo? TryBindBarcode(
            DateTime observedAt,
            BarcodeQueueOrderEnum queueOrder,
            bool enforceAssignmentInterval,
            int minimumAssignmentMilliseconds,
            int maximumAssignmentMilliseconds,
            int? emptyPackageExpiryMilliseconds,
            DateTime processingAt,
            Action<PackageInfo> assignment) =>
            TryBindBarcode(
                observedAt, queueOrder, enforceAssignmentInterval,
                minimumAssignmentMilliseconds, maximumAssignmentMilliseconds,
                emptyPackageExpiryMilliseconds, processingAt, assignment, out _);

        /// <summary>绑定条码，并在拒绝时返回本次决策的原因及候选包裹时间。</summary>
        public PackageInfo? TryBindBarcode(
            DateTime observedAt,
            BarcodeQueueOrderEnum queueOrder,
            bool enforceAssignmentInterval,
            int minimumAssignmentMilliseconds,
            int maximumAssignmentMilliseconds,
            int? emptyPackageExpiryMilliseconds,
            DateTime processingAt,
            Action<PackageInfo> assignment,
            out BarcodeAssignmentRejection? rejection) {
            ArgumentNullException.ThrowIfNull(assignment);
            return TryConsumeBarcodeResult(
                observedAt, queueOrder, enforceAssignmentInterval,
                minimumAssignmentMilliseconds, maximumAssignmentMilliseconds,
                emptyPackageExpiryMilliseconds, processingAt, assignment,
                discard: false, out rejection);
        }

        /// <summary>按相同匹配规则原子消费一个 NoRead 占位，释放资源且不完成业务包裹。</summary>
        public PackageInfo? TryDiscardUnassignedPackage(
            DateTime observedAt,
            BarcodeQueueOrderEnum queueOrder,
            bool enforceAssignmentInterval,
            int minimumAssignmentMilliseconds,
            int maximumAssignmentMilliseconds,
            int? emptyPackageExpiryMilliseconds,
            DateTime processingAt,
            out BarcodeAssignmentRejection? rejection) =>
            TryConsumeBarcodeResult(
                observedAt, queueOrder, enforceAssignmentInterval,
                minimumAssignmentMilliseconds, maximumAssignmentMilliseconds,
                emptyPackageExpiryMilliseconds, processingAt, assignment: null,
                discard: true, out rejection);

        /// <summary>只消费指定小车的相机结果；空赋值操作表示丢弃，拒绝时关闭原包裹且绝不搜索下一辆车。</summary>
        public PackageInfo? TryConsumeCameraResultForPackage(
            DateTime packageCreatedAt,
            DateTime observedAt,
            bool enforceAssignmentInterval,
            int minimumAssignmentMilliseconds,
            int maximumAssignmentMilliseconds,
            int? emptyPackageExpiryMilliseconds,
            DateTime processingAt,
            Action<PackageInfo>? assignment,
            out BarcodeAssignmentRejection? rejection) =>
            TryConsumeBarcodeResult(
                observedAt, BarcodeQueueOrderEnum.TimeAscending, enforceAssignmentInterval,
                minimumAssignmentMilliseconds, maximumAssignmentMilliseconds,
                emptyPackageExpiryMilliseconds, processingAt, assignment,
                discard: assignment is null, out rejection, packageCreatedAt);

        /// <summary>赋值和丢弃共用选择、时窗与包裹锁，避免先查找再移除的并发间隙。</summary>
        private PackageInfo? TryConsumeBarcodeResult(
            DateTime observedAt,
            BarcodeQueueOrderEnum queueOrder,
            bool enforceAssignmentInterval,
            int minimumAssignmentMilliseconds,
            int maximumAssignmentMilliseconds,
            int? emptyPackageExpiryMilliseconds,
            DateTime processingAt,
            Action<PackageInfo>? assignment,
            bool discard,
            out BarcodeAssignmentRejection? rejection,
            DateTime? originalCreatedAt = null) {
            rejection = null;

            BarcodeAssignmentRejection DescribeRejection(
                BarcodeAssignmentRejectionReason reason,
                DateTime? createdAt = null,
                long? packageId = null,
                long? packageTimestamp = null) {
                PackageInfo? snapshot = null;
                if (createdAt is { } key && (!packageId.HasValue || !packageTimestamp.HasValue))
                    _packageInfos.TryGetValue(key, out snapshot);
                return new(reason, observedAt, processingAt, createdAt, packageId ?? snapshot?.Guid) {
                    PackageTimestamp = packageTimestamp ?? snapshot?.Timestamp
                };
            }

            var minimumTicks = Math.Max(0L, (long)minimumAssignmentMilliseconds * TimeSpan.TicksPerMillisecond);
            var maximumTicks = (long)maximumAssignmentMilliseconds * TimeSpan.TicksPerMillisecond;
            var emptyPackageExpiryTicks = emptyPackageExpiryMilliseconds is > 0
                ? (long)emptyPackageExpiryMilliseconds.Value * TimeSpan.TicksPerMillisecond
                : long.MaxValue;
            if (enforceAssignmentInterval && maximumTicks < minimumTicks) {
                rejection = DescribeRejection(BarcodeAssignmentRejectionReason.InvalidAssignmentWindow, originalCreatedAt);
                if (originalCreatedAt is { } invalidKey) {
                    RemovePackage(invalidKey, "相机结果已消费，赋值窗口配置无效",
                        static pair => pair.Value.BarCodeInfo is null, isDiscarded: true);
                }
                return null;
            }

            var observedTicks = observedAt.Ticks;
            var processingTicks = processingAt.Ticks;
            while (originalCreatedAt.HasValue || !_packageInfos.IsEmpty) {
                KeyValuePair<DateTime, PackageInfo>? selected = null;
                if (originalCreatedAt is { } originalKey) {
                    if (!_packageInfos.TryGetValue(originalKey, out var originalPackage)) {
                        rejection = DescribeRejection(BarcodeAssignmentRejectionReason.OriginalPackageUnavailable, originalKey);
                        return null;
                    }
                    selected = new KeyValuePair<DateTime, PackageInfo>(originalKey, originalPackage);
                }
                else lock (_packageOrderGate) {
                    while (_unassignedPackageOrder.Count > 0) {
                        var key = SelectBarcodeCandidateKey(
                            observedTicks,
                            processingTicks,
                            queueOrder,
                            enforceAssignmentInterval,
                            minimumTicks,
                            maximumTicks,
                            emptyPackageExpiryTicks);
                        if (key is null) {
                            // 倒序选择会跳过太新的包裹；全部候选过早时仍记录最早候选的实际间隔。
                            var earliestKey = _unassignedPackageOrder.Min;
                            _packageInfos.TryGetValue(earliestKey, out var earliestPackage);
                            rejection = DescribeRejection(
                                observedTicks < earliestKey.Ticks
                                    ? BarcodeAssignmentRejectionReason.ObservedBeforePackageCreation
                                    : BarcodeAssignmentRejectionReason.BelowMinimumInterval,
                                earliestKey, earliestPackage?.Guid, earliestPackage?.Timestamp);
                            return null;
                        }
                        if (_packageInfos.TryGetValue(key.Value, out var package) &&
                            package.BarCodeInfo is null) {
                            selected = new KeyValuePair<DateTime, PackageInfo>(key.Value, package);
                            break;
                        }
                        _unassignedPackageOrder.Remove(key.Value);
                    }
                }

                if (selected is not { } candidate) {
                    rejection = DescribeRejection(BarcodeAssignmentRejectionReason.NoUnassignedPackage);
                    return null;
                }

                var expiredWhileQueued = false;
                PackageInfo? discardedPackage = null;
                lock (candidate.Value.SyncRoot) {
                    if (!_packageInfos.TryGetValue(candidate.Key, out var current) ||
                        !ReferenceEquals(current, candidate.Value) ||
                        current.BarCodeInfo is not null) {
                        if (originalCreatedAt.HasValue) {
                            rejection = DescribeRejection(BarcodeAssignmentRejectionReason.OriginalPackageUnavailable,
                                candidate.Key, candidate.Value.Guid, candidate.Value.Timestamp);
                            return null;
                        }
                        continue;
                    }

                    var ageTicks = observedTicks - candidate.Key.Ticks;
                    var processingAgeTicks = processingTicks - candidate.Key.Ticks;
                    if (ageTicks < 0 && (enforceAssignmentInterval || !originalCreatedAt.HasValue)) {
                        rejection = DescribeRejection(
                            BarcodeAssignmentRejectionReason.ObservedBeforePackageCreation,
                            candidate.Key, current.Guid, current.Timestamp);
                        if (!originalCreatedAt.HasValue) {
                            return null;
                        }
                    }
                    else if (ageTicks >= emptyPackageExpiryTicks ||
                        processingAgeTicks >= emptyPackageExpiryTicks) {
                        // 窗口内可用候选已全部筛选；当前对象仅作为失败诊断或过期清理对象。
                        expiredWhileQueued = true;
                        rejection = DescribeRejection(
                            ageTicks >= emptyPackageExpiryTicks
                                ? BarcodeAssignmentRejectionReason.EmptyPackageExpiredAtObservation
                                : BarcodeAssignmentRejectionReason.EmptyPackageExpiredWhileQueued,
                            candidate.Key, current.Guid, current.Timestamp);
                    }
                    else if (enforceAssignmentInterval &&
                        (ageTicks < minimumTicks || ageTicks > maximumTicks)) {
                        rejection = DescribeRejection(
                            ageTicks < minimumTicks
                                ? BarcodeAssignmentRejectionReason.BelowMinimumInterval
                                : BarcodeAssignmentRejectionReason.AboveMaximumInterval,
                            candidate.Key, current.Guid, current.Timestamp);
                        if (!originalCreatedAt.HasValue) {
                            return null;
                        }
                    }
                    else {
                        if (discard) {
                            lock (_packageOrderGate) {
                                if (!((ICollection<KeyValuePair<DateTime, PackageInfo>>)_packageInfos)
                                    .Remove(candidate)) {
                                    continue;
                                }
                                _packageOrder.Remove(candidate.Key);
                                _unassignedPackageOrder.Remove(candidate.Key);
                                if (current.Guid != 0 &&
                                    _packageIdIndex.TryGetValue(current.Guid, out var indexedKey) &&
                                    indexedKey == candidate.Key) {
                                    _packageIdIndex.Remove(current.Guid);
                                }
                                Interlocked.Decrement(ref _packageCount);
                            }
                            current.MarkRemoved();
                            discardedPackage = current;
                        }
                        else {
                            assignment!(current);
                            lock (_packageOrderGate) {
                                _unassignedPackageOrder.Remove(candidate.Key);
                                IndexBarcodeObservation(current);
                            }
                            return current;
                        }
                    }
                }

                if (discardedPackage is not null) {
                    OnPackageRemoved(new PackageRemovedEventArgs(
                        discardedPackage, "NoRead 已消费并丢弃", isDiscarded: true));
                    return discardedPackage;
                }
                if (originalCreatedAt.HasValue) {
                    RemovePackage(candidate.Key, "相机结果已消费，原小车未通过赋值校验",
                        static pair => pair.Value.BarCodeInfo is null, isDiscarded: true);
                    return null;
                }
                if (expiredWhileQueued) {
                    RemovePackage(
                        candidate.Key,
                        "空包裹过期优先，拒绝晚到条码",
                        static pair => pair.Value.BarCodeInfo is null,
                        isDiscarded: discard);
                }
                return null;
            }

            rejection = DescribeRejection(BarcodeAssignmentRejectionReason.NoUnassignedPackage);
            return null;
        }

        /// <summary>先筛选全部未赋值且未过期的窗口候选，再按用户配置的时间顺序选择首个；无命中时返回最近候选用于诊断。</summary>
        private DateTime? SelectBarcodeCandidateKey(
            long observedTicks,
            long processingTicks,
            BarcodeQueueOrderEnum queueOrder,
            bool enforceAssignmentInterval,
            long minimumTicks,
            long maximumTicks,
            long emptyPackageExpiryTicks) {
            var oldestKey = _unassignedPackageOrder.Min;
            if (!enforceAssignmentInterval) {
                if (observedTicks - oldestKey.Ticks >= emptyPackageExpiryTicks ||
                    processingTicks - oldestKey.Ticks >= emptyPackageExpiryTicks) {
                    return oldestKey;
                }
                return queueOrder == BarcodeQueueOrderEnum.TimeAscending
                    ? oldestKey
                    : _unassignedPackageOrder.Max;
            }

            var firstEligibleTicks = Math.Max(DateTime.MinValue.Ticks, observedTicks - maximumTicks);
            var lastEligibleTicks = observedTicks - minimumTicks;
            if (emptyPackageExpiryTicks != long.MaxValue) {
                firstEligibleTicks = Math.Max(firstEligibleTicks,
                    Math.Max(observedTicks, processingTicks) - emptyPackageExpiryTicks + 1);
            }
            if (firstEligibleTicks <= lastEligibleTicks) {
                // 区间视图按用户配置迭代；不复制或重新排序整个队列，也不因首个区间外包裹而提前拒绝。
                var window = _unassignedPackageOrder.GetViewBetween(
                    new DateTime(firstEligibleTicks), new DateTime(lastEligibleTicks));
                IEnumerable<DateTime> orderedCandidates = queueOrder == BarcodeQueueOrderEnum.TimeAscending
                    ? window : window.Reverse();
                using var eligible = orderedCandidates.GetEnumerator();
                if (eligible.MoveNext()) {
                    return eligible.Current;
                }
            }

            // 只有整个有效区间为空才进入失败诊断；保留最近已创建对象的真实时间与拒绝原因。
            using var observedCandidates = _unassignedPackageOrder.GetViewBetween(
                DateTime.MinValue, new DateTime(observedTicks)).Reverse().GetEnumerator();
            return observedCandidates.MoveNext() ? observedCandidates.Current : oldestKey;
        }

        /// <summary>
        /// 获取包裹
        /// </summary>
        /// <param name="createTime"></param>
        /// <returns></returns>
        public PackageInfo? GetPackage(DateTime createTime) {
            _packageInfos.TryGetValue(createTime, out var package);
            return package;
        }

        /// <summary>按下位机序号直接获取仍在运行的包裹。</summary>
        public PackageInfo? GetPackageById(long packageId) {
            DateTime createTime;
            lock (_packageOrderGate) {
                if (!_packageIdIndex.TryGetValue(packageId, out createTime)) {
                    return null;
                }
            }
            return GetPackage(createTime);
        }

        /// <summary>按相机观测键直接查找多条码所属的活动包裹。</summary>
        public PackageInfo? GetPackageByBarcodeObservation(DateTime scanTime, string serialNumber) {
            DateTime createTime;
            lock (_packageOrderGate) {
                if (!_barcodeObservationIndex.TryGetValue((scanTime, serialNumber), out createTime)) {
                    return null;
                }
            }
            var package = GetPackage(createTime);
            var barcode = package?.BarCodeInfo;
            return barcode is not null && barcode.ScanTime == scanTime &&
                   string.Equals(barcode.SerialNumber, serialNumber, StringComparison.Ordinal)
                ? package
                : null;
        }

        /// <summary>仅在索引锁内维护已赋值包裹的相机观测键。</summary>
        private void IndexBarcodeObservation(PackageInfo package) {
            if (package.BarCodeInfo is { } barcode) {
                _barcodeObservationIndex[(barcode.ScanTime, barcode.SerialNumber)] = package.CreateTime;
            }
        }

        /// <summary>相同观测键被后续包裹复用时，移除旧包裹不能删除新映射。</summary>
        private void RemoveBarcodeObservation(PackageInfo package) {
            if (package.BarCodeInfo is { } barcode) {
                var key = (barcode.ScanTime, barcode.SerialNumber);
                if (_barcodeObservationIndex.TryGetValue(key, out var createTime) &&
                    createTime == package.CreateTime) {
                    _barcodeObservationIndex.Remove(key);
                }
            }
        }

        /// <summary>
        /// 获取包裹
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public List<PackageInfo> GetPackages(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            var packages = new List<PackageInfo>();
            foreach (var key in GetOrderedKeys()) {
                if (!_packageInfos.TryGetValue(key, out var value)) {
                    continue;
                }
                var pair = new KeyValuePair<DateTime, PackageInfo>(key, value);
                lock (pair.Value.SyncRoot) {
                    if (predicate(pair)) {
                        packages.Add(pair.Value);
                    }
                }
            }

            return packages;
        }

        /// <summary>获取活动包裹的不可变读取快照，调用方无法修改注册表或聚合内部集合。</summary>
        public IReadOnlyList<PackageSessionSnapshot> GetSnapshot() {
            var snapshots = new List<PackageSessionSnapshot>();
            foreach (var key in GetOrderedKeys()) {
                if (!_packageInfos.TryGetValue(key, out var package)) {
                    continue;
                }

                lock (package.SyncRoot) {
                    snapshots.Add(package.CreateSnapshot());
                }
            }

            return Array.AsReadOnly(snapshots.ToArray());
        }

        /// <summary>
        /// 获取包裹数量
        /// </summary>
        /// <returns></returns>
        public int GetPackageCount() {
            return Math.Max(0, Volatile.Read(ref _packageCount));
        }

        /// <summary>以常数时间判断当前是否存在尚未赋值的包裹。</summary>
        public bool HasUnassignedPackage() {
            lock (_packageOrderGate) {
                while (_unassignedPackageOrder.Count > 0) {
                    var key = _unassignedPackageOrder.Min;
                    if (_packageInfos.TryGetValue(key, out var package) &&
                        package.BarCodeInfo is null) {
                        return true;
                    }
                    _unassignedPackageOrder.Remove(key);
                }
                return false;
            }
        }

        /// <summary>
        /// 清空包裹
        /// </summary>
        public void ClearAllPackages() {
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
                    Interlocked.Decrement(ref _packageCount);
                    lock (_packageOrderGate) {
                        _packageOrder.Remove(key);
                        _unassignedPackageOrder.Remove(key);
                        RemoveBarcodeObservation(info);
                        if (info.Guid != 0 &&
                            _packageIdIndex.TryGetValue(info.Guid, out var indexedKey) &&
                            indexedKey == key) {
                            _packageIdIndex.Remove(info.Guid);
                        }
                    }
                    OnPackageRemoved(new PackageRemovedEventArgs(info, "清空全部包裹"));
                }
            }
            lock (_packageOrderGate) {
                if (_packageInfos.IsEmpty) {
                    _packageOrder.Clear();
                    _unassignedPackageOrder.Clear();
                    _packageIdIndex.Clear();
                    _barcodeObservationIndex.Clear();
                    Volatile.Write(ref _packageCount, 0);
                }
            }
        }

        /// <summary>获取按创建时间升序排列的稳定键快照。</summary>
        private DateTime[] GetOrderedKeys() {
            lock (_packageOrderGate) {
                return [.. _packageOrder];
            }
        }

        /// <summary>
        /// 完成包裹
        /// </summary>
        /// <param name="predicate"></param>

        public void CompletedPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            var packageInfo = GetPackage(predicate);
            if (packageInfo == null) return;

            CompletedPackage(packageInfo.CreateTime);
        }

        /// <summary>按创建时间键直接完成包裹，避免已知主键时再次扫描全部会话。</summary>
        /// <param name="createTime">包裹创建时间。</param>
        public void CompletedPackage(DateTime createTime) {
            if (!_packageInfos.TryGetValue(createTime, out var packageInfo)) return;

            lock (packageInfo.SyncRoot) {
                if (!_packageInfos.TryGetValue(createTime, out var current) ||
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

    /// <summary>
    /// 赋值计时器
    /// </summary>
    public class PackageAssignmentTimer : PackageTimer {
        public TimeSpan AssignmentTimeSpan { get; set; }
        internal PackageScheduledCallback? AssignmentTimer { get; set; }

        // 回调方法，接收PackageInfo参数，返回是否继续保留计时器
        public Func<PackageInfo, bool>? AssignmentCallback { get; set; }
    }

    /// <summary>
    /// 移除包裹计时器
    /// </summary>
    public class PackageRemoveTimer : PackageTimer {
        public TimeSpan RemovalTimeSpan { get; set; }
        internal PackageScheduledCallback? PackageRemovalTimer { get; set; }
        public string Description { get; set; } = string.Empty;

        /// <summary>可选的过期调度器，用于将过期移除与包裹修改串行化；返回 false 时由计时器就地移除。</summary>
        public Func<Action, bool>? TryDispatch { get; set; }
    }

    /// <summary>
    /// 完成计时器
    /// </summary>
    public class PackCompletedTimer : PackageTimer {
        public TimeSpan CompletTimeSpan { get; set; }
        internal PackageScheduledCallback? CompletTimer { get; set; }
    }

    public class PackageTimer {
        public Func<KeyValuePair<DateTime, PackageInfo>, bool>? Predicate { get; set; }
    }

    public class PackageRemovedEventArgs : EventArgs {
        public PackageInfo RemovedPackage { get; }
        public string Description { get; }
        /// <summary>是否仅消费占位并丢弃业务结果；内部资源清理订阅者仍会收到通知。</summary>
        public bool IsDiscarded { get; }

        public PackageRemovedEventArgs(PackageInfo removedPackage, string description) {
            RemovedPackage = removedPackage;
            Description = description;
        }

        /// <summary>创建可区分内部丢弃与常规移除的资源清理通知。</summary>
        public PackageRemovedEventArgs(PackageInfo removedPackage, string description, bool isDiscarded)
            : this(removedPackage, description) {
            IsDiscarded = isDiscarded;
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
