using System.Collections.Concurrent;
using System.Diagnostics;
using JayTom.Dws.Abstractions.Imaging;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.DownstreamProtocols;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Model;
using JayTom.Dws.Domain.Packages;

namespace JayTom.Dws.Legacy.Contracts.Packages {

    /// <summary>维护单个运行包裹的数据、不变量、状态和资源所有权。</summary>
    public class PackageInfo {
        /// <summary>提供可测试的本地业务时钟。</summary>
        private readonly TimeProvider _timeProvider;
        /// <summary>仅由聚合方法追加的领域事件。</summary>
        private readonly List<IPackageDomainEvent> _domainEvents = [];
        /// <summary>使用整数进行原子发布的显式生命周期状态。</summary>
        private int _lifecycleState = (int)PackageLifecycleState.Collecting;

        /// <summary>使用系统时钟创建包裹运行时实体。</summary>
        public PackageInfo()
            : this(TimeProvider.System) {
        }

        /// <summary>使用指定时钟创建包裹运行时实体。</summary>
        /// <param name="timeProvider">业务时间提供器。</param>
        public PackageInfo(TimeProvider timeProvider) {
            ArgumentNullException.ThrowIfNull(timeProvider);
            _timeProvider = timeProvider;
            CreateTime = timeProvider.GetLocalNow().DateTime;
        }

        /// <summary>
        /// 保护包裹内部的可变字段和集合。ConcurrentDictionary 只能保护容器，
        /// 不能保护存放在其中的 PackageInfo 实例。
        /// </summary>
        public object SyncRoot { get; } = new();

        /// <summary>获取当前生命周期状态。</summary>
        public PackageLifecycleState LifecycleState =>
            (PackageLifecycleState)Volatile.Read(ref _lifecycleState);

        /// <summary>获取领域事件的只读副本。</summary>
        public IReadOnlyList<IPackageDomainEvent> DomainEvents {
            get {
                lock (SyncRoot) {
                    return Array.AsReadOnly(_domainEvents.ToArray());
                }
            }
        }

        /// <summary>
        /// 包裹创建时间
        /// </summary>
        public DateTime CreateTime { get; init; }

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
        /// <summary>保存通过原子读写发布的条码信息引用。</summary>
        private BarCodeInfoModel? _barCodeInfo;

        /// <summary>
        /// 条码信息使用原子引用发布，使匹配热路径可以无锁跳过已赋值包裹；
        /// 实际赋值仍由包裹锁保护。
        /// </summary>
        public BarCodeInfoModel? BarCodeInfo {
            get => Volatile.Read(ref _barCodeInfo);
            set => Volatile.Write(ref _barCodeInfo, value);
        }

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
        /// <summary>标记当前包裹是否必须保留到格口指令发送完成。</summary>
        private int _isSortingInstructionExpected;
        /// <summary>标记当前包裹的全部格口指令是否已成功写入物理连接。</summary>
        private int _isSortingInstructionSent;
        /// <summary>下位机原车已发出格口指令，必须保留到落格确认。</summary>
        private int _isFallConfirmationExpected;
        /// <summary>真实回执或现有超时补记已完成该原车的落格确认。</summary>
        private int _isFallConfirmed;
        /// <summary>
        /// 获取或设置包裹信息是否已经填充完成。
        /// </summary>
        public bool IsCompleted {
            get => Volatile.Read(ref _isCompleted) != 0;
        }

        /// <summary>以不可逆方式将包裹标记为完成。</summary>
        public void MarkCompleted() {
            Volatile.Write(ref _isCompleted, 1);
            TransitionTo(IsWaitingForSortingInstruction
                ? PackageLifecycleState.AwaitingSortingInstruction
                : PackageLifecycleState.ReadyForSorting);
        }

        /// <summary>获取当前包裹是否仍需等待格口指令发送完成。</summary>
        public bool IsWaitingForSortingInstruction =>
            Volatile.Read(ref _isSortingInstructionExpected) != 0 &&
            Volatile.Read(ref _isSortingInstructionSent) == 0;

        /// <summary>标记当前包裹在离开活动会话前必须完成格口指令发送。</summary>
        public void MarkSortingInstructionExpected() {
            Volatile.Write(ref _isSortingInstructionExpected, 1);
            TransitionTo(PackageLifecycleState.AwaitingSortingInstruction);
        }

        /// <summary>标记当前包裹的全部格口指令已经成功写入物理连接。</summary>
        public void MarkSortingInstructionSent() {
            if (IsCreatedByLowerMachine) {
                Volatile.Write(ref _isFallConfirmationExpected, 1);
            }
            Volatile.Write(ref _isSortingInstructionSent, 1);
            TransitionTo(IsCompleted
                ? PackageLifecycleState.Completed
                : PackageLifecycleState.Collecting);
        }

        /// <summary>确认当前运行模式无需发送格口指令并释放生命周期等待。</summary>
        public void MarkSortingInstructionNotRequired() {
            Volatile.Write(ref _isFallConfirmationExpected, 0);
            Volatile.Write(ref _isSortingInstructionSent, 1);
            if (IsCompleted) {
                TransitionTo(PackageLifecycleState.Completed);
            }
        }

        /// <summary>防止填充完整信息清理抢在下位机落格回执之前移除原车。</summary>
        public bool IsWaitingForFallConfirmation =>
            Volatile.Read(ref _isFallConfirmationExpected) != 0 &&
            Volatile.Read(ref _isFallConfirmed) == 0;

        /// <summary>收到原车物理格口事件后释放清理等待；确认早于发送通知时同样保留此状态。</summary>
        public void MarkFallConfirmed() => Volatile.Write(ref _isFallConfirmed, 1);

        /// <summary>仅接受一次下位机最终回执，重复回执不再写入分拣历史。</summary>
        public bool TryConfirmFall() => Interlocked.Exchange(ref _isFallConfirmed, 1) == 0;

        /// <summary>将包裹标记为已移出活动会话；已移除状态不可逆。</summary>
        internal void MarkRemoved() => TransitionTo(PackageLifecycleState.Removed);

        /// <summary>通过值对象绑定条码并记录不可变领域事件。</summary>
        public bool TryAssignBarcode(PackageBarcode barcode, DateTime scanTime, DateTime bindTime) {
            lock (SyncRoot) {
                if (BarCodeInfo is not null || LifecycleState == PackageLifecycleState.Removed) {
                    return false;
                }

                BarCodeInfo = new BarCodeInfoModel {
                    Barcode = barcode.Value,
                    ScanTime = scanTime,
                    BindTime = bindTime
                };
                _domainEvents.Add(new PackageBarcodeAssigned(
                    Id,
                    barcode,
                    _timeProvider.GetUtcNow()));
                return true;
            }
        }

        /// <summary>清除已经由应用层发布的领域事件。</summary>
        public void ClearDomainEvents() {
            lock (SyncRoot) {
                _domainEvents.Clear();
            }
        }

        /// <summary>创建不包含可变实体引用的读取快照。</summary>
        internal PackageSessionSnapshot CreateSnapshot() => new(
            Id,
            CreateTime,
            LifecycleState,
            BarCodeInfo is not null,
            WeightInfo is not null,
            VolumeInfo is not null);

        /// <summary>以原子方式改变生命周期并记录事件。</summary>
        private void TransitionTo(PackageLifecycleState target) {
            lock (SyncRoot) {
                var previous = LifecycleState;
                if (previous == PackageLifecycleState.Removed || previous == target) {
                    return;
                }

                Volatile.Write(ref _lifecycleState, (int)target);
                _domainEvents.Add(new PackageLifecycleChanged(
                    Id,
                    previous,
                    target,
                    _timeProvider.GetUtcNow()));
            }
        }

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
        private List<PackageRemoveTimer>? _packageRemoveTimers;

        /// <summary>
        /// 完成包裹计时器
        /// </summary>
        private List<PackCompletedTimer>? _packCompletedTimers;

        /// <summary>
        /// 包裹赋值计时器
        /// </summary>
        private List<PackageAssignmentTimer>? _packageAssignmentTimers;

        /// <summary>拥有当前包裹生命周期的会话注册表。</summary>
        private PackageSessionRegistry? _sessionRegistry;

        /// <summary>
        /// 其他
        /// </summary>

        public object? Other { get; set; }

        /// <summary>兼容旧移除流程的包裹级互斥锁。</summary>
        private readonly System.Threading.Lock _removalLock = new();

        /// <summary>启动当前包裹拥有的赋值、完成和移除计时器。</summary>
        public void StartTimers(
            ConcurrentDictionary<DateTime, PackageInfo> packageInfos,
            List<PackageTimer> removeTimers,
            PackageSessionRegistry registry) {
            lock (SyncRoot) {
                _sessionRegistry = registry;
                foreach (var timer in removeTimers) {
                    var monotonicElapsed = Stopwatch.GetElapsedTime(
                        CreatedAtMonotonicTimestamp,
                        Stopwatch.GetTimestamp());
                    var wallElapsed = _timeProvider.GetLocalNow().DateTime.Subtract(CreateTime);
                    var elapsedSinceCreation = wallElapsed > monotonicElapsed
                        ? wallElapsed
                        : monotonicElapsed;
                    if (elapsedSinceCreation < TimeSpan.Zero) {
                        elapsedSinceCreation = TimeSpan.Zero;
                    }
                    switch (timer) {
                        case PackageRemoveTimer packageRemoveTimer:
                            packageRemoveTimer.PackageRemovalTimer = PackageTimerScheduler.Schedule(
                                GetRemainingDueTime(
                                    packageRemoveTimer.RemovalTimeSpan,
                                    elapsedSinceCreation),
                                () => RemoveFromCollection(new Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageRemoveTimer>(packageInfos, packageRemoveTimer)));
                            (_packageRemoveTimers ??= []).Add(packageRemoveTimer);
                            break;

                        case PackCompletedTimer packCompletedTimer:
                            packCompletedTimer.CompletTimer = PackageTimerScheduler.Schedule(
                                GetRemainingDueTime(
                                    packCompletedTimer.CompletTimeSpan,
                                    elapsedSinceCreation),
                                () => CompletedCollection(new Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackCompletedTimer>(packageInfos, packCompletedTimer)));
                            (_packCompletedTimers ??= []).Add(packCompletedTimer);
                            break;

                        case PackageAssignmentTimer packageAssignmentTimer:
                            packageAssignmentTimer.AssignmentTimer = PackageTimerScheduler.Schedule(
                                GetRemainingDueTime(
                                    packageAssignmentTimer.AssignmentTimeSpan,
                                    elapsedSinceCreation),
                                () => AssignmentCollection(new Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageAssignmentTimer>(packageInfos, packageAssignmentTimer)));
                            (_packageAssignmentTimers ??= []).Add(packageAssignmentTimer);
                            break;
                    }
                }
            }
        }

        /// <summary>扣除包裹在进入会话前已经消耗的时间，保证到期时间以机械创建时刻为准。</summary>
        private static TimeSpan GetRemainingDueTime(
            TimeSpan configuredDueTime,
            TimeSpan elapsedSinceCreation) =>
            configuredDueTime > elapsedSinceCreation
                ? configuredDueTime - elapsedSinceCreation
                : TimeSpan.Zero;

        /// <summary>原子转移图像所有权。</summary>
        public ImageHandle? TakeImage() {
            lock (SyncRoot) {
                var image = Image;
                Image = null;
                return image;
            }
        }

        /// <summary>停止并释放当前包裹拥有的全部计时器。</summary>
        public void DisposeTimers() {
            lock (SyncRoot) {
                if (_packageRemoveTimers is { } removeTimers) {
                    foreach (var timer in removeTimers) {
                        timer.PackageRemovalTimer?.Dispose();
                    }
                    removeTimers.Clear();
                }
                if (_packCompletedTimers is { } completedTimers) {
                    foreach (var timer in completedTimers) {
                        timer.CompletTimer?.Dispose();
                    }
                    completedTimers.Clear();
                }
                if (_packageAssignmentTimers is { } assignmentTimers) {
                    foreach (var timer in assignmentTimers) {
                        timer.AssignmentTimer?.Dispose();
                    }
                    assignmentTimers.Clear();
                }
            }
        }

        /// <summary>处理移除计时器回调并在聚合锁内重新校验条件。</summary>
        private void RemoveFromCollection(object? state) {
            if (state is not null) {
                var (packageInfos, removeTimer) = (Tuple<ConcurrentDictionary<DateTime, PackageInfo>, PackageRemoveTimer>)state;
                /// <summary>在串行队列中重新校验并执行包裹移除。</summary>
                void RemovePackage() {
                    if (packageInfos.TryGetValue(CreateTime, out _)) {
                        _sessionRegistry?.RemovePackage(
                            CreateTime,
                            removeTimer.Description,
                            removeTimer.Predicate);
                    }

                    removeTimer.PackageRemovalTimer?.Dispose();
                }

                if (removeTimer.TryDispatch is not null &&
                    removeTimer.TryDispatch(RemovePackage)) {
                    return;
                }

                RemovePackage();
            }
        }

        /// <summary>处理完成计时器回调。</summary>
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

                    _sessionRegistry?.CompletedPackage(pair => pair.Key.Equals(CreateTime));
                    removeTimer.CompletTimer?.Dispose();
                }
            }
        }

        /// <summary>处理延迟赋值计时器回调。</summary>
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
                                    assignmentTimer.AssignmentTimeSpan);
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

}
