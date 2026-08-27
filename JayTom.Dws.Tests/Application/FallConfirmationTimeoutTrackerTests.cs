using JayTom.Dws.Application.Events;
using JayTom.Dws.Application.PackageExits;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Tests.Testing;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证下位机落格确认超时的正常完成、取消与去重规则。</summary>
public sealed class FallConfirmationTimeoutTrackerTests
{
    /// <summary>验证未满六十秒时不会提前生成落格事件。</summary>
    [Fact]
    public void TakeExpired_BeforeSixtySeconds_DoesNotCreateFallEvent()
    {
        var time = new ManualTimeProvider();
        var tracker = new FallConfirmationTimeoutTracker(time);
        tracker.Register(CreateTheoreticalExit(1001, "54", 54));

        time.Advance(
            FallConfirmationTimeoutTracker.ConfirmationTimeout -
            TimeSpan.FromTicks(1));

        Assert.Empty(tracker.TakeExpired(new DateTime(2026, 8, 27, 12, 0, 0)));
    }

    /// <summary>验证满六十秒时生成与真实回复相同类型的正常落格事件。</summary>
    [Fact]
    public void TakeExpired_AtSixtySeconds_CreatesNormalSignalCallbackFallEvent()
    {
        var time = new ManualTimeProvider();
        var tracker = new FallConfirmationTimeoutTracker(time);
        tracker.Register(CreateTheoreticalExit(1002, "55", 55));
        var confirmationTime = new DateTime(2026, 8, 27, 12, 0, 0);

        time.Advance(TimeSpan.FromSeconds(60));
        var fallEvents = tracker.TakeExpired(confirmationTime);
        var fallEvent = GetOnlyItem(fallEvents);

        Assert.Equal(1002, fallEvent.Timestamp);
        Assert.Equal("55", fallEvent.ExitName);
        Assert.Equal(55, fallEvent.ExitId);
        Assert.Equal(SortingExitType.PhysicalExit, fallEvent.ExitType);
        Assert.Equal(InstructionType.SignalCallback, fallEvent.InstructionType);
        Assert.Equal(PackageAbnormalSortingType.None,
            fallEvent.PackageAbnormalSortingType);
        Assert.Equal(ExitType.PackageExit, fallEvent.Type);
        var instruction = GetOnlyItem(fallEvent.InstructionInfos!);
        Assert.Equal(InstructionType.SignalCallback,
            instruction.InstructionType);
        Assert.Equal(confirmationTime,
            instruction.InstructionGeneratedTime);
        Assert.Empty(tracker.TakeExpired(confirmationTime.AddSeconds(1)));
    }

    /// <summary>验证六十秒内收到真实回复时取消超时落格。</summary>
    [Fact]
    public void TryConfirm_BeforeTimeout_CancelsFallbackFallEvent()
    {
        var time = new ManualTimeProvider();
        var tracker = new FallConfirmationTimeoutTracker(time);
        tracker.Register(CreateTheoreticalExit(1003, "08", 8));

        time.Advance(TimeSpan.FromSeconds(20));
        Assert.True(tracker.TryConfirm(1003));
        time.Advance(TimeSpan.FromSeconds(60));

        Assert.Empty(tracker.TakeExpired(DateTime.Now));
    }

    /// <summary>验证超时落格完成后拒绝迟到的下位机重复回复。</summary>
    [Fact]
    public void TryConfirm_AfterTimeout_RejectsLateLowerMachineReply()
    {
        var time = new ManualTimeProvider();
        var tracker = new FallConfirmationTimeoutTracker(time);
        tracker.Register(CreateTheoreticalExit(1004, "09", 9));

        time.Advance(TimeSpan.FromSeconds(60));
        GetOnlyItem(tracker.TakeExpired(DateTime.Now));

        Assert.False(tracker.TryConfirm(1004));
    }

    /// <summary>验证再次发送格口会刷新时限并使用最新目标格口。</summary>
    [Fact]
    public void Register_Again_RestartsTimeoutAndUsesLatestExit()
    {
        var time = new ManualTimeProvider();
        var tracker = new FallConfirmationTimeoutTracker(time);
        tracker.Register(CreateTheoreticalExit(1005, "10", 10));
        time.Advance(TimeSpan.FromSeconds(50));
        tracker.Register(CreateTheoreticalExit(1005, "11", 11));
        time.Advance(TimeSpan.FromSeconds(10));

        Assert.Empty(tracker.TakeExpired(DateTime.Now));

        time.Advance(TimeSpan.FromSeconds(50));
        var fallEvents = tracker.TakeExpired(DateTime.Now);
        var fallEvent = GetOnlyItem(fallEvents);
        Assert.Equal("11", fallEvent.ExitName);
        Assert.Equal(11, fallEvent.ExitId);
    }

    private static PackageExitUpdateEvent CreateTheoreticalExit(
        long timestamp,
        string exitName,
        long exitId) =>
        new()
        {
            CreateTime = new DateTime(2026, 8, 27, 11, 0, 0),
            Timestamp = timestamp,
            ExitName = exitName,
            ExitId = exitId,
            ExitType = SortingExitType.TheoreticalExit,
            PackageAbnormalSortingType = PackageAbnormalSortingType.None,
            InstructionInfos =
            [
                new InstructionInfoModel
                {
                    InstructionContent = "send",
                    InstructionGeneratedTime =
                        new DateTime(2026, 8, 27, 11, 0, 1),
                    InstructionType = InstructionType.SendSorting
                }
            ],
            InstructionType = InstructionType.SendSorting,
            Type = ExitType.PackageExit
        };

    private static T GetOnlyItem<T>(IReadOnlyList<T> items)
    {
        Assert.True(items.Count == 1);
        return items[0];
    }
}
