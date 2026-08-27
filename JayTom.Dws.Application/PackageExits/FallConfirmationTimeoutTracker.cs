using System.Collections.Concurrent;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.PackageExits;

/// <summary>
/// 跟踪已经成功发送到下位机、但尚未收到落格回复的包裹。
/// </summary>
public sealed class FallConfirmationTimeoutTracker
{
    /// <summary>发送格口后等待下位机落格回复的固定时长。</summary>
    public static readonly TimeSpan ConfirmationTimeout =
        TimeSpan.FromSeconds(60);

    /// <summary>保留完成标记，抑制超时后迟到或重复的下位机回复。</summary>
    private static readonly TimeSpan CompletedRetention =
        TimeSpan.FromHours(1);

    /// <summary>完成标记的最短清理间隔。</summary>
    private static readonly TimeSpan CleanupInterval =
        TimeSpan.FromMinutes(1);

    private const string TimeoutInstructionContent =
        "落格确认超时60秒";

    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<long, PendingFallConfirmation>
        _pending = new();
    private readonly ConcurrentDictionary<long, long> _completed = new();
    private long _lastCleanupTimestamp;

    /// <summary>创建落格确认跟踪器。</summary>
    public FallConfirmationTimeoutTracker(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _lastCleanupTimestamp = _timeProvider.GetTimestamp();
    }

    /// <summary>
    /// 登记一次已经成功发送的理论格口；同一包裹再次发送时从最后一次发送重新计时。
    /// </summary>
    public void Register(PackageExitUpdateEvent theoreticalExit)
    {
        ArgumentNullException.ThrowIfNull(theoreticalExit);
        if (theoreticalExit.Timestamp <= 0)
        {
            return;
        }

        long now = _timeProvider.GetTimestamp();
        CleanupCompleted(now);
        if (_completed.ContainsKey(theoreticalExit.Timestamp))
        {
            return;
        }

        var pending = new PendingFallConfirmation(
            theoreticalExit.CreateTime,
            theoreticalExit.Timestamp,
            theoreticalExit.ExitName,
            theoreticalExit.ExitId,
            theoreticalExit.Type,
            now);
        _pending.AddOrUpdate(
            theoreticalExit.Timestamp,
            pending,
            (_, _) => pending);

        // 真实回复可能与登记并发；完成状态一旦先落地，不允许重新建立超时任务。
        if (_completed.ContainsKey(theoreticalExit.Timestamp))
        {
            _pending.TryRemove(theoreticalExit.Timestamp, out _);
        }
    }

    /// <summary>
    /// 登记真实落格或异常落格；返回 false 表示同一包裹此前已经完成。
    /// </summary>
    public bool TryConfirm(long packageTimestamp)
    {
        if (packageTimestamp <= 0)
        {
            // 兼容没有关联时间戳的旧协议，不对所有零时间戳事件做全局去重。
            return true;
        }

        long now = _timeProvider.GetTimestamp();
        CleanupCompleted(now);
        if (!_completed.TryAdd(packageTimestamp, now))
        {
            return false;
        }

        _pending.TryRemove(packageTimestamp, out _);
        return true;
    }

    /// <summary>
    /// 取出达到固定等待时长的包裹，并生成与真实下位机回复相同类型的物理落格事件。
    /// </summary>
    public IReadOnlyList<PackageExitUpdateEvent> TakeExpired(
        DateTime confirmationTime)
    {
        long now = _timeProvider.GetTimestamp();
        CleanupCompleted(now);
        List<PackageExitUpdateEvent>? expired = null;
        foreach (var pair in _pending)
        {
            if (_timeProvider.GetElapsedTime(
                    pair.Value.RegisteredAtTimestamp,
                    now) < ConfirmationTimeout)
            {
                continue;
            }

            // 仅删除本次枚举看到的值，避免误删刚刚重新发送并刷新时限的新记录。
            if (!((ICollection<KeyValuePair<long, PendingFallConfirmation>>)
                    _pending).Remove(pair) ||
                !_completed.TryAdd(pair.Key, now))
            {
                continue;
            }

            (expired ??= []).Add(new PackageExitUpdateEvent
            {
                CreateTime = pair.Value.CreateTime,
                Timestamp = pair.Value.Timestamp,
                ExitName = pair.Value.ExitName,
                ExitId = pair.Value.ExitId,
                ExitType = SortingExitType.PhysicalExit,
                PackageAbnormalSortingType =
                    PackageAbnormalSortingType.None,
                InstructionInfos =
                [
                    new InstructionInfoModel
                    {
                        InstructionContent = TimeoutInstructionContent,
                        InstructionGeneratedTime = confirmationTime,
                        InstructionType = InstructionType.SignalCallback
                    }
                ],
                InstructionType = InstructionType.SignalCallback,
                Type = pair.Value.Type
            });
        }

        return expired ?? [];
    }

    /// <summary>按固定周期清理完成标记，保持迟到回复去重集合有界。</summary>
    private void CleanupCompleted(long now)
    {
        long lastCleanup = Volatile.Read(ref _lastCleanupTimestamp);
        if (_timeProvider.GetElapsedTime(lastCleanup, now) < CleanupInterval ||
            Interlocked.CompareExchange(
                ref _lastCleanupTimestamp,
                now,
                lastCleanup) != lastCleanup)
        {
            return;
        }

        foreach (var pair in _completed)
        {
            if (_timeProvider.GetElapsedTime(pair.Value, now) >=
                CompletedRetention)
            {
                ((ICollection<KeyValuePair<long, long>>)_completed)
                    .Remove(pair);
            }
        }
    }

    /// <summary>保存生成正常物理落格事件所需的理论格口快照。</summary>
    private sealed record PendingFallConfirmation(
        DateTime CreateTime,
        long Timestamp,
        string ExitName,
        long ExitId,
        ExitType Type,
        long RegisteredAtTimestamp);
}
