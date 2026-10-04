extern alias ClientRuntime;

using System.Collections.Concurrent;
using System.Drawing;
using System.Reflection;
using JayTom.Dws.Abstractions.Integrations;
using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.CameraConfigurations;
using JayTom.Dws.Application.Messaging;
using JayTom.Dws.Application.Packages;
using JayTom.Dws.Application.PackageHistory;
using JayTom.Dws.Application.Workflows;
using JayTom.Dws.Camera;
using JayTom.Dws.Integrations.Contracts;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Model;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CameraConfig;
using JayTom.Dws.Legacy.Contracts.Services.ImageService;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Models.LocalConf.CameraConfig;
using DeviceService = ClientRuntime::JayTom.Dws.Client.Service.Device.IDeviceService;
using ExternalDataService = ClientRuntime::JayTom.Dws.Client.Service.ExternalDataService.IExternalDataService;
using PackageInstruction = ClientRuntime::JayTom.Dws.Client.Service.Sorting.PackageInstructionEventArgs;
using SortingService = ClientRuntime::JayTom.Dws.Client.Service.Sorting.ISortingService;
using Workflow = ClientRuntime::JayTom.Dws.Client.Service.ProcessingServices.YunShanPackageBackgroundService;
using InstructionReceived = ClientRuntime::JayTom.Dws.Client.Service.Sorting.InstructionReceived;
using PersistenceWorkflow = ClientRuntime::JayTom.Dws.Client.Service.BackgroundService.DataProcessingBackgroundService;
using UploadWorkflow = ClientRuntime::JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;
using UploadItem = ClientRuntime::JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService.SubmitItemInfo;
using StableWeight = ClientRuntime::JayTom.Dws.Client.Service.Device.StableWeightEventArgs;
using JayTom.Dws.Application.Events;
using Microsoft.Extensions.Caching.Memory;

namespace JayTom.Dws.Tests.Application;

/// <summary>独立验证实际客户端后台队列的小车消费、完成通知及创建指令，不启动窗口或设备。</summary>
public sealed class LowerMachineNoReadWorkflowTests
{
    /// <summary>开启和关闭空读时，落盘证据都能准确说明原触发归属和业务结果。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoRead_decisions_write_original_cart_and_trigger_evidence(bool useNoRead)
    {
        using var evidence = new PipelineEvidence();
        await using var fixture = new Fixture(useNoRead, cameras: 1);
        fixture.CreateCart(8101, 0);
        fixture.CreateCart(8102, 300);
        await fixture.CheckCriticalState(() => { });
        var original = fixture.Store.GetPackageById(8101)!;
        var following = fixture.Store.GetPackageById(8102)!;
        fixture.Missed("camera-1", 200);
        fixture.Scanned("camera-1", "JT-EVIDENCE-8102", 500);
        await fixture.Drain();
        var originalRows = evidence.ForPackage(original.Timestamp);
        var paired = originalRows.First(row => row.GetProperty("stage").GetString() == "camera.paired");
        Assert.Equal(8101, paired.GetProperty("cartId").GetInt64());
        Assert.Equal(1, paired.GetProperty("cameraOrdinal").GetInt64());
        Assert.Equal(1, paired.GetProperty("triggerSequence").GetInt64());
        Assert.Equal(original.CreateTime.Ticks, paired.GetProperty("creationTimeTicks").GetInt64());
        if (useNoRead)
        {
            var completed = originalRows.First(row => row.GetProperty("stage").GetString() == "package.completed");
            Assert.Equal("NoRead", completed.GetProperty("barcode").GetString());
            Assert.DoesNotContain(originalRows, row => row.GetProperty("stage").GetString() == "package.discarded");
        }
        else
        {
            var discarded = originalRows.First(row => row.GetProperty("stage").GetString() == "package.discarded");
            Assert.Equal("NoReadDisabled", discarded.GetProperty("reasonCode").GetString());
            Assert.True(discarded.GetProperty("resultConsumed").GetBoolean());
            Assert.DoesNotContain(originalRows, row => row.GetProperty("stage").GetString() == "package.completed");
        }
        var selected = evidence.ForPackage(following.Timestamp)
            .First(row => row.GetProperty("stage").GetString() == "barcode.assignment.selected");
        Assert.Equal(8102, selected.GetProperty("cartId").GetInt64());
        Assert.Equal(2, selected.GetProperty("cameraOrdinal").GetInt64());
        fixture.AssertNoErrors();
    }

    /// <summary>超过五百毫秒的空读或有效码均记录原车拒绝原因，后车的归属保持独立。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Late_original_result_logs_the_exact_window_rejection_without_shifting(bool noRead)
    {
        using var evidence = new PipelineEvidence();
        await using var fixture = new Fixture(useNoRead: true, cameras: 1, window: true);
        fixture.CreateCart(8201, 0);
        fixture.CreateCart(8202, 300);
        await fixture.CheckCriticalState(() => { });
        var original = fixture.Store.GetPackageById(8201)!;
        var following = fixture.Store.GetPackageById(8202)!;
        if (noRead) fixture.Missed("camera-1", 600);
        else fixture.Scanned("camera-1", "JT-LATE-8201", 600);
        fixture.Scanned("camera-1", "JT-FOLLOWING-8202", 650);
        await fixture.Drain();
        var rejected = evidence.ForPackage(original.Timestamp)
            .First(row => row.GetProperty("stage").GetString() == "camera.pair.rejected");
        Assert.Equal("AboveMaximumInterval", rejected.GetProperty("reasonCode").GetString());
        Assert.Equal(600m, rejected.GetProperty("observedAgeMilliseconds").GetDecimal());
        Assert.Equal(8201, rejected.GetProperty("cartId").GetInt64());
        Assert.Equal(500, rejected.GetProperty("maximumMilliseconds").GetInt32());
        var selected = evidence.ForPackage(following.Timestamp)
            .First(row => row.GetProperty("stage").GetString() == "barcode.assignment.selected");
        Assert.Equal("JT-FOLLOWING-8202", selected.GetProperty("barcode").GetString());
        Assert.Equal(8202, selected.GetProperty("cartId").GetInt64());
        fixture.AssertNoErrors();
    }
    /// <summary>重复回执无论在清理前还是清理后到达，原车只能记录一次；小车循环复用后仍接收新回执。</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Duplicate_fall_callbacks_are_idempotent_and_reused_cart_accepts_new_callback(bool useNoRead, bool lowerRemoval)
    {
        await using var fixture = new Fixture(useNoRead, cameras: 1, lowerRemoval: lowerRemoval);
        fixture.CreateCart(101, 0);
        if (useNoRead) fixture.Missed("camera-1", 200);
        else fixture.Scanned("camera-1", "JT-FIRST", 200);
        await fixture.CheckCriticalState(() =>
        {
            var package = fixture.Store.GetPackageById(101)!;
            package.MarkSortingInstructionSent();
            package.MarkImageSaveRequested();
        });
        fixture.CallbackCart(101);
        fixture.CallbackCart(101);
        await fixture.CheckCriticalState(() =>
        {
            fixture.CleanupCompletedPackages();
            Assert.Null(fixture.Store.GetPackageById(101));
        });
        fixture.CallbackCart(101);
        fixture.CreateCart(101, 300);
        fixture.Scanned("camera-1", "JT-SECOND", 500);
        fixture.CallbackCart(101);
        fixture.CallbackCart(101);
        await fixture.Drain();
        var callbacks = fixture.Bus.Events.OfType<InstructionReceived>()
            .Where(item => item.InstructionInfos?.Any(info => info.InstructionType == InstructionType.SignalCallback) == true).ToArray();
        Assert.Equal(2, callbacks.Length);
        Assert.Equal(2, callbacks.Select(item => item.Timestamp).Distinct().Count());
        Assert.Equal([101L, 101L], fixture.CompletedIds());
        fixture.AssertNoErrors();
    }

    /// <summary>已丢弃的空读位置收到落格回执时，不落库也不误报找不到包裹。</summary>
    [Fact]
    public async Task Callback_for_consumed_disabled_NoRead_is_ignored_without_affecting_next_cart()
    {
        await using var fixture = new Fixture(useNoRead: false, cameras: 1);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        fixture.Missed("camera-1", 200);
        fixture.Scanned("camera-1", "JT-102", 500);
        fixture.CallbackCart(101);
        fixture.CallbackCart(101);
        fixture.CallbackCart(102);
        fixture.CallbackCart(102);
        await fixture.Drain();
        Assert.Equal([102L], fixture.CompletedIds());
        Assert.Collection(fixture.Bus.Events.OfType<InstructionReceived>()
                .Where(item => item.InstructionInfos?.Any(info => info.InstructionType == InstructionType.SignalCallback) == true),
            item => Assert.Equal("102", item.SortingCode));
        fixture.AssertNoErrors();
    }

    /// <summary>TCP 粘包中的多个小车接收时刻相同，时间键和落库标识都必须唯一。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Coalesced_creation_times_keep_all_cart_positions_and_unique_business_ids(bool useNoRead)
    {
        const int count = 512;
        await using var fixture = new Fixture(useNoRead, cameras: 1, businessConsumers: true);
        for (var id = 1; id <= count; id++) fixture.CreateCart(id, 0);
        for (var id = 1; id <= count; id++)
        {
            if (id % 3 == 0) fixture.Missed("camera-1", 650 + id);
            else fixture.Scanned("camera-1", $"JT-{id}", 650 + id);
        }
        await fixture.Drain();
        var expected = Enumerable.Range(1, count).Where(id => useNoRead || id % 3 != 0).Select(id => (long)id).ToArray();
        Assert.Equal(expected, fixture.CompletedIds());
        var packages = fixture.Bus.PackageEvents.OfType<PackageInfo>().ToArray();
        Assert.Equal(expected.Length, packages.Select(item => item.Timestamp).Distinct().Count());
        Assert.Equal(expected.Length, packages.Select(item => item.CreateTime).Distinct().Count());
        Assert.Equal(packages.Select(item => item.Timestamp), fixture.TakePersistenceInput().Select(item => item.PackageTimestamped));
        Assert.Equal(packages.Select(item => item.Timestamp), fixture.TakeUploadInput().Select(item => item.Timestamp));
        Assert.Equal(packages.Select(item => item.Timestamp), fixture.Bus.Events.OfType<InstructionReceived>().Select(item => item.Timestamp));
        fixture.AssertNoErrors();
    }

    /// <summary>空读发出格口指令后保留原车，真实回执或既有超时补记释放清理等待。</summary>
    [Fact]
    public async Task Completed_NoRead_keeps_original_cart_until_physical_confirmation()
    {
        await using var fixture = new Fixture(useNoRead: true, cameras: 1);
        fixture.CreateCart(101, 0);
        fixture.Missed("camera-1", 200);
        await fixture.CheckCriticalState(() =>
        {
            var package = Assert.IsType<PackageInfo>(fixture.Store.GetPackageById(101));
            package.MarkSortingInstructionExpected();
            package.MarkSortingInstructionSent();
            package.MarkImageSaveRequested();
            fixture.CleanupCompletedPackages();
            Assert.Same(package, fixture.Store.GetPackageById(101));
            Assert.True(package.IsWaitingForFallConfirmation);
            fixture.Bus.Publish(new PackageExitUpdateEvent
            {
                Timestamp = package.Timestamp, ExitType = SortingExitType.PhysicalExit,
                InstructionType = InstructionType.SignalCallback
            });
            Assert.False(package.IsWaitingForFallConfirmation);
            fixture.CleanupCompletedPackages();
            Assert.Null(fixture.Store.GetPackageById(101));
        });
        await fixture.Drain();
        fixture.AssertNoErrors();
    }

    /// <summary>密集小车创建不受相机防抖限流，空读不进入业务完成或普通移除通知。</summary>
    [Fact]
    public async Task Disabled_NoRead_consumes_cart_without_business_publication()
    {
        await using var fixture = new Fixture(useNoRead: false, cameras: 1);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 5);
        fixture.Missed("camera-1", 650);
        fixture.Scanned("camera-1", "JT-102", 660);

        await fixture.Drain();

        Assert.Equal([102L], fixture.CompletedIds());
        Assert.Equal([102L], fixture.DisplayedIds());
        Assert.Null(fixture.Store.GetPackageById(101));
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        Assert.Collection(fixture.Bus.Events.OfType<InstructionReceived>(),
            item => Assert.Equal("102", item.SortingCode));
        Assert.DoesNotContain(fixture.Bus.Events.OfType<TriggerPositionEvent>(),
            item => item.TriggerPosition == TriggerPositionEnum.RemovePackageAfter && item.PackageInfo?.Guid == 101);
        fixture.AssertNoErrors();
    }

    /// <summary>实际有序后台队列积压大量创建和识别事件时，交错空车不造成编号偏移。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dense_queued_lower_machine_results_keep_every_valid_cart_id(bool useNoRead)
    {
        const int count = 4000;
        await using var fixture = new Fixture(useNoRead, cameras: 1);
        for (var index = 0; index < count; index++)
        {
            fixture.CreateCart(index + 1, index * 5);
        }
        for (var index = 0; index < count; index++)
        {
            if (index % 2 == 0)
            {
                fixture.Missed("camera-1", index * 5 + 650);
            }
            else
            {
                fixture.Scanned("camera-1", $"JT-{index + 1}", index * 5 + 650);
            }
        }

        await fixture.Drain();

        var expectedIds = Enumerable.Range(0, count).Where(index => useNoRead || index % 2 != 0)
            .Select(index => index + 1L).ToArray();
        Assert.Equal(expectedIds, fixture.CompletedIds());
        Assert.Equal(expectedIds, fixture.DisplayedIds());
        Assert.Equal(expectedIds.Length, fixture.Store.GetPackageCount());
        Assert.Equal(expectedIds.Length, fixture.Bus.Events.OfType<InstructionReceived>().Count());
        foreach (var id in expectedIds)
        {
            Assert.Equal(id % 2 == 1 ? "NoRead" : $"JT-{id}",
                fixture.Store.GetPackageById(id)?.BarCodeInfo?.Barcode);
        }
        Assert.False(fixture.Store.HasUnassignedPackage());
        fixture.AssertNoErrors();
    }

    /// <summary>条码过滤器通过扫码事件送出的占位字符串也必须消费对应的小车。</summary>
    [Theory]
    [InlineData("NoRead")]
    [InlineData("noread")]
    [InlineData("filtered")]
    [InlineData("")]
    public async Task Filtered_placeholder_on_scanned_event_also_consumes_cart(string placeholder)
    {
        await using var fixture = new Fixture(useNoRead: false, cameras: 1);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 5);
        fixture.Scanned("camera-1", placeholder, 650);
        fixture.Scanned("camera-1", "JT-102", 660);

        await fixture.Drain();

        Assert.Equal([102L], fixture.CompletedIds());
        Assert.Null(fixture.Store.GetPackageById(101));
        fixture.AssertNoErrors();
    }

    /// <summary>多相机先完成融合：全空读按开关处理，任一相机有有效码则保留有效包裹。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Multiple_cameras_merge_NoRead_before_deciding_business_output(bool useNoRead)
    {
        await using var fixture = new Fixture(useNoRead, cameras: 2);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 5);
        fixture.Missed("camera-1", 650);
        fixture.Missed("camera-2", 650);
        fixture.Missed("camera-1", 660);
        fixture.Scanned("camera-2", "JT-102", 660);

        await fixture.Drain();

        Assert.Equal(useNoRead ? [101L, 102L] : [102L], fixture.CompletedIds());
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        Assert.Equal(useNoRead ? 2 : 1, fixture.Bus.Events.OfType<InstructionReceived>().Count());
        fixture.AssertNoErrors();
    }

    /// <summary>空读输出过滤仍消费第二辆空车，让第三辆小车的有效码保持正确编号。</summary>
    [Fact]
    public async Task NoRead_filter_suppresses_output_after_consuming_the_cart()
    {
        await using var fixture = new Fixture(useNoRead: true, cameras: 1, filterNoRead: true);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 5);
        fixture.CreateCart(103, 10);
        fixture.Missed("camera-1", 650);
        fixture.Missed("camera-1", 660);
        fixture.Scanned("camera-1", "JT-103", 670);

        await fixture.Drain();

        Assert.Equal([101L, 103L], fixture.CompletedIds());
        Assert.Null(fixture.Store.GetPackageById(102));
        Assert.Equal("JT-103", fixture.Store.GetPackageById(103)?.BarCodeInfo?.Barcode);
        Assert.Equal(2, fixture.Bus.Events.OfType<InstructionReceived>().Count());
        fixture.AssertNoErrors();
    }

    /// <summary>一次 600ms 超时只丢弃本车，后续有效码按原小车编号继续处理。</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Window_timeout_discards_one_pair_and_following_carts_remain_aligned(bool useNoRead, bool missed)
    {
        await using var fixture = new Fixture(useNoRead, cameras: 1, window: true);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        fixture.CreateCart(103, 600);
        if (missed)
        {
            fixture.Missed("camera-1", 600);
        }
        else
        {
            fixture.Scanned("camera-1", "JT-101", 600);
        }
        fixture.Scanned("camera-1", "JT-102", 650);
        fixture.Scanned("camera-1", "JT-103", 850);

        await fixture.Drain();

        Assert.Equal([102L, 103L], fixture.CompletedIds());
        Assert.Null(fixture.Store.GetPackageById(101));
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        Assert.Equal("JT-103", fixture.Store.GetPackageById(103)?.BarCodeInfo?.Barcode);
        Assert.Equal(2, fixture.Bus.Events.OfType<InstructionReceived>().Count());
        fixture.AssertNoErrors();
    }

    /// <summary>过期回调已移除原包裹，其保留消费位置接住迟到结果。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Removed_cart_keeps_consumption_position_for_its_late_result(bool useNoRead)
    {
        await using var fixture = new Fixture(useNoRead, cameras: 1, window: true);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        await fixture.RemoveCart(101);
        if (useNoRead)
        {
            fixture.Missed("camera-1", 600);
        }
        else
        {
            fixture.Scanned("camera-1", "JT-101", 600);
        }
        fixture.Scanned("camera-1", "JT-102", 650);

        await fixture.Drain();

        Assert.Equal([102L], fixture.CompletedIds());
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>相机结果先于创建指令到达时暂存，后续事件不产生一位偏移。</summary>
    [Fact]
    public async Task Camera_result_arriving_before_creation_instruction_waits_for_its_own_cart()
    {
        await using var fixture = new Fixture(useNoRead: false, cameras: 1);
        fixture.Scanned("camera-1", "JT-101", 200);
        fixture.CreateCart(101, 0);
        fixture.Scanned("camera-1", "JT-102", 500);
        fixture.CreateCart(102, 300);

        await fixture.Drain();

        Assert.Equal([101L, 102L], fixture.CompletedIds());
        Assert.Equal("JT-101", fixture.Store.GetPackageById(101)?.BarCodeInfo?.Barcode);
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>创建指令的时间戳晚于相机观察时间时，关闭窗口仍按同一触发绑定。</summary>
    [Fact]
    public async Task Device_timestamp_differences_do_not_change_trigger_ownership_when_window_is_disabled()
    {
        await using var fixture = new Fixture(useNoRead: false, cameras: 1);
        fixture.Scanned("camera-1", "JT-101", 200);
        fixture.CreateCart(101, 500);
        fixture.Scanned("camera-1", "JT-102", 500);
        fixture.CreateCart(102, 800);

        await fixture.Drain();

        Assert.Equal([101L, 102L], fixture.CompletedIds());
        Assert.Equal("JT-101", fixture.Store.GetPackageById(101)?.BarCodeInfo?.Barcode);
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>下位机物理触发按正序消费，不因旧的倒序设置把相邻两辆车交换。</summary>
    [Fact]
    public async Task Lower_machine_trigger_order_is_preserved_with_descending_queue_setting()
    {
        await using var fixture = new Fixture(useNoRead: true, cameras: 1, descending: true);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        fixture.Scanned("camera-1", "JT-101", 600);
        fixture.Scanned("camera-1", "JT-102", 650);

        await fixture.Drain();

        Assert.Equal([101L, 102L], fixture.CompletedIds());
        Assert.Equal("JT-101", fixture.Store.GetPackageById(101)?.BarCodeInfo?.Barcode);
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>连续多次超时逐车丢弃，恢复正常的下一次识别立即继续输出。</summary>
    [Fact]
    public async Task Repeated_timeouts_do_not_open_a_global_circuit_breaker()
    {
        await using var fixture = new Fixture(useNoRead: false, cameras: 1, window: true);
        for (var index = 0; index < 32; index++)
        {
            fixture.CreateCart(index + 1, index * 300);
        }
        for (var index = 0; index < 31; index++)
        {
            fixture.Scanned("camera-1", $"JT-{index + 1}", index * 300 + 600);
        }
        fixture.Scanned("camera-1", "JT-32", 31 * 300 + 450);

        await fixture.Drain();

        Assert.Equal([32L], fixture.CompletedIds());
        Assert.Equal("JT-32", fixture.Store.GetPackageById(32)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>多相机响应相差一个机械周期时，仍按各相机第几次触发融合。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Multiple_camera_latency_differences_do_not_merge_adjacent_carts(bool useNoRead)
    {
        await using var fixture = new Fixture(useNoRead, cameras: 2);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        if (useNoRead)
        {
            fixture.Missed("camera-1", 200);
            fixture.Missed("camera-1", 500);
            fixture.Missed("camera-2", 510);
        }
        else
        {
            fixture.Scanned("camera-1", "JT-101", 200);
            fixture.Scanned("camera-1", "JT-102", 500);
            fixture.Scanned("camera-2", "JT-101", 510);
        }
        fixture.Scanned("camera-2", "JT-102", 810);

        await fixture.Drain();

        Assert.Equal([101L, 102L], fixture.CompletedIds());
        Assert.Equal(useNoRead ? "NoRead" : "JT-101", fixture.Store.GetPackageById(101)?.BarCodeInfo?.Barcode);
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>多相机组超时后，慢相机的原车结果不重新消费下一辆车。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Late_camera_after_fusion_timeout_does_not_reopen_or_consume_next_cart(bool useNoRead)
    {
        await using var fixture = new Fixture(useNoRead, cameras: 2);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        if (useNoRead)
        {
            fixture.Missed("camera-1", 200);
        }
        else
        {
            fixture.Scanned("camera-1", "JT-101", 200);
        }
        await fixture.ExpireFirstFusion();
        if (useNoRead)
        {
            fixture.Missed("camera-1", 500);
            fixture.Missed("camera-2", 510);
            fixture.Missed("camera-2", 810);
        }
        else
        {
            fixture.Scanned("camera-1", "JT-102", 500);
            fixture.Scanned("camera-2", "JT-101", 510);
            fixture.Scanned("camera-2", "JT-102", 810);
        }

        await fixture.Drain();

        Assert.Equal([102L], fixture.CompletedIds());
        Assert.Null(fixture.Store.GetPackageById(101));
        Assert.Equal(useNoRead ? "NoRead" : "JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        Assert.Equal([102L], fixture.DisplayedIds());
        fixture.AssertNoErrors();
    }

    /// <summary>相机丢弃过滤也保留内部最终结果，NoRead 开关不影响下一辆车的对应关系。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Filtered_trigger_consumes_its_cart_without_business_output(bool useNoRead)
    {
        await using var fixture = new Fixture(useNoRead, cameras: 1, window: true);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        fixture.Filtered("camera-1", 200);
        fixture.Scanned("camera-1", "JT-102", 500);

        await fixture.Drain();

        Assert.Equal([102L], fixture.CompletedIds());
        Assert.Null(fixture.Store.GetPackageById(101));
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>高密度回调的时间戳可相同，只按实际帧号识别重复结果。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Distinct_final_frames_with_identical_timestamps_each_consume_their_own_cart(bool useNoRead)
    {
        await using var fixture = new Fixture(useNoRead, cameras: 1);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 5);
        fixture.ScannedFrame("camera-1", useNoRead ? "NoRead" : "JT-101", 200, 1);
        fixture.ScannedFrame("camera-1", "JT-102", 200, 2);

        await fixture.Drain();

        Assert.Equal([101L, 102L], fixture.CompletedIds());
        Assert.Equal(useNoRead ? "NoRead" : "JT-101", fixture.Store.GetPackageById(101)?.BarCodeInfo?.Barcode);
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>显式停止结束旧触发消费次序，再启动的小车不会填到旧标记。</summary>
    [Fact]
    public async Task Explicit_stop_resets_interrupted_consumption_positions_before_next_run()
    {
        await using var fixture = new Fixture(useNoRead: false, cameras: 1);
        fixture.CreateCart(101, 0);
        await fixture.StopDeviceSession();
        fixture.CreateCart(102, 1000);
        fixture.Scanned("camera-1", "JT-102", 1200);

        await fixture.Drain();

        Assert.Equal([102L], fixture.CompletedIds());
        Assert.Null(fixture.Store.GetPackageById(101));
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>同次触发的额外条码帧不消费下一辆小车。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Duplicate_frame_after_success_does_not_consume_the_following_cart(bool useNoRead)
    {
        await using var fixture = new Fixture(useNoRead, cameras: 1);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        fixture.ScannedFrame("camera-1", useNoRead ? "NoRead" : "JT-101", 200, 1);
        fixture.ScannedFrame("camera-1", useNoRead ? "NoRead" : "JT-EXTRA", 200, 1);
        fixture.ScannedFrame("camera-1", "JT-102", 500, 2);

        await fixture.Drain();

        Assert.Equal([101L, 102L], fixture.CompletedIds());
        Assert.Equal(useNoRead ? "NoRead" : "JT-101", fixture.Store.GetPackageById(101)?.BarCodeInfo?.Barcode);
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>创建失败的触发也接住本次结果，不把它绑定到下一辆合法小车。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_creation_consumes_its_final_result_without_spilling_into_next_cart(bool useNoRead)
    {
        await using var fixture = new Fixture(useNoRead, cameras: 1);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(101, 300);
        fixture.CreateCart(102, 600);
        if (useNoRead)
        {
            fixture.Missed("camera-1", 200);
            fixture.Missed("camera-1", 500);
        }
        else
        {
            fixture.Scanned("camera-1", "JT-101", 200);
            fixture.Scanned("camera-1", "JT-REJECTED", 500);
        }
        fixture.Scanned("camera-1", "JT-102", 800);

        await fixture.Drain();

        Assert.Equal([101L, 102L], fixture.CompletedIds());
        Assert.Equal(useNoRead ? "NoRead" : "JT-101", fixture.Store.GetPackageById(101)?.BarCodeInfo?.Barcode);
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>一万次密集创建和交错空读中，一次迟到只丢弃原车，不引起持续偏移。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dense_results_with_one_late_barcode_keep_all_following_cart_ids(bool useNoRead)
    {
        const int count = 10_000;
        const int lateIndex = 193;
        await using var fixture = new Fixture(useNoRead, cameras: 1, window: true);
        for (var index = 0; index < count; index++)
        {
            fixture.CreateCart(index + 1, index * 300);
        }
        for (var index = 0; index < count; index++)
        {
            var offset = index * 300 + (index == lateIndex ? 600 : 450);
            if (index % 3 == 0)
            {
                fixture.Missed("camera-1", offset);
            }
            else
            {
                fixture.Scanned("camera-1", $"JT-{index + 1}", offset);
            }
        }

        await fixture.Drain();

        var expectedIds = Enumerable.Range(0, count).Where(index => (useNoRead || index % 3 != 0) && index != lateIndex)
            .Select(index => index + 1L).ToArray();
        Assert.Equal(expectedIds, fixture.CompletedIds());
        Assert.Equal(expectedIds, fixture.DisplayedIds());
        Assert.Equal(expectedIds.Length, fixture.Bus.Events.OfType<InstructionReceived>().Count());
        foreach (var id in expectedIds)
        {
            Assert.Equal((id - 1) % 3 == 0 ? "NoRead" : $"JT-{id}",
                fixture.Store.GetPackageById(id)?.BarCodeInfo?.Barcode);
        }
        Assert.Null(fixture.Store.GetPackageById(lateIndex + 1));
        Assert.False(fixture.Store.HasUnassignedPackage());
        fixture.AssertNoErrors();
    }

    /// <summary>开启空读后，实际界面通知、持久化入口和上传入口都保留原车及完整相机信息。</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Enabled_NoRead_reaches_ui_persistence_and_upload_with_original_cart_metadata(int cameras)
    {
        await using var fixture = new Fixture(useNoRead: true, cameras, businessConsumers: true);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        for (var camera = 1; camera <= cameras; camera++)
        {
            fixture.Missed($"camera-{camera}", 600 + camera * 100);
            fixture.Scanned($"camera-{camera}", "JT-102", 650 + camera * 100);
        }

        await fixture.Drain();

        Assert.Equal([101L, 102L], fixture.CompletedIds());
        Assert.Equal([101L, 102L], fixture.DisplayedIds());
        var package = Assert.IsType<PackageInfo>(fixture.Store.GetPackageById(101));
        Assert.True(package.IsCompleted);
        Assert.True(package.IsCreatedByLowerMachine);
        Assert.Equal("create:101", package.PackageCreationInstruction);
        Assert.NotNull(package.WeightInfo);
        Assert.NotNull(package.VolumeInfo);
        var barcode = Assert.IsType<BarCodeInfoModel>(package.BarCodeInfo);
        Assert.Equal("NoRead", barcode.Barcode);
        Assert.Equal("camera-1", barcode.SerialNumber);
        Assert.Equal(barcode.SerialNumber, barcode.DisplayIdentifier);
        Assert.Equal(SourceType.Camera, barcode.Source);
        Assert.Equal(package.CreateTime.AddMilliseconds(700), barcode.ScanTime);
        Assert.False(fixture.Store.HasUnassignedPackage());

        Assert.Collection(fixture.TakePersistenceInput(),
            item =>
            {
                Assert.Equal(package.Timestamp, item.PackageTimestamped);
                Assert.Equal(package.CreateTime, item.PackageCreateTime);
                Assert.Same(barcode, item.BarCodeInfo);
                Assert.Same(package.WeightInfo, item.WeightInfo);
                Assert.Same(package.VolumeInfo, item.VolumeInfo);
            },
            item => Assert.Equal("JT-102", item.BarCodeInfo?.Barcode));
        Assert.Collection(fixture.TakeUploadInput(),
            item =>
            {
                Assert.Equal(101L, item.Guid);
                Assert.Equal("NoRead", item.Barcode);
                Assert.True(item.IsCreatedByLowerMachine);
                Assert.Equal(package.Timestamp, item.Timestamp);
                Assert.Equal(package.CreateTime, item.PackageCreationTime);
                Assert.Equal("create:101", item.PackageCreationInstruction);
                Assert.Equal(barcode.SerialNumber, item.CameraSerialNumber);
                Assert.Equal(barcode.ScanTime, item.ScanTime);
            },
            item =>
            {
                Assert.Equal(102L, item.Guid);
                Assert.Equal("JT-102", item.Barcode);
            });
        Assert.Collection(fixture.Bus.Events.OfType<InstructionReceived>(),
            item =>
            {
                Assert.Equal("101", item.SortingCode);
                Assert.True(item.IsCreatedByLowerMachine);
                Assert.Equal(package.Timestamp, item.Timestamp);
                Assert.Equal("test-machine", item.ConnectionName);
                Assert.Collection(item.InstructionInfos!, instruction =>
                {
                    Assert.Equal(InstructionType.CreatePackage, instruction.InstructionType);
                    Assert.Equal("create:101", instruction.InstructionContent);
                    Assert.Equal(package.CreateTime, instruction.InstructionGeneratedTime);
                });
            },
            item => Assert.Equal("102", item.SortingCode));
        fixture.AssertNoErrors();
    }

    /// <summary>通过扫码事件返回的空读占位与空读事件一样保留业务输出，沿用原始条码文本。</summary>
    [Theory]
    [InlineData("NoRead")]
    [InlineData("noread")]
    [InlineData("filtered")]
    [InlineData("")]
    public async Task Enabled_NoRead_on_scanned_event_keeps_output_and_next_cart_ownership(string placeholder)
    {
        await using var fixture = new Fixture(useNoRead: true, cameras: 1);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        fixture.Scanned("camera-1", placeholder, 600);
        fixture.Scanned("camera-1", "JT-102", 650);

        await fixture.Drain();

        Assert.Equal([101L, 102L], fixture.CompletedIds());
        Assert.Equal([101L, 102L], fixture.DisplayedIds());
        Assert.Equal(placeholder, fixture.Store.GetPackageById(101)?.BarCodeInfo?.Barcode);
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>使用称重时空读仍等待数据齐全，两个到达顺序均只发布一次完成结果。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Enabled_NoRead_waits_for_weight_then_completes_exactly_once(bool weightFirst)
    {
        await using var fixture = new Fixture(useNoRead: true, cameras: 1,
            weightMode: WeightMode.Static, businessConsumers: true);
        fixture.CreateCart(101, 0);
        if (weightFirst)
        {
            fixture.Weighed(2.75m);
        }
        else
        {
            fixture.Missed("camera-1", 200);
        }
        await fixture.CheckCriticalState(() =>
        {
            var pending = Assert.IsType<PackageInfo>(fixture.Store.GetPackageById(101));
            Assert.False(pending.IsCompleted);
            Assert.Equal(weightFirst, pending.WeightInfo is not null);
            Assert.Equal(!weightFirst, pending.BarCodeInfo is not null);
            Assert.Empty(fixture.Bus.PackageEvents);
            Assert.Empty(fixture.Bus.Events.OfType<PackageInfo>());
            Assert.Empty(fixture.Bus.Events.OfType<InstructionReceived>());
        });
        if (weightFirst)
        {
            fixture.Missed("camera-1", 200);
        }
        else
        {
            fixture.Weighed(2.75m);
        }
        fixture.Weighed(9m);
        fixture.Missed("camera-1", 200);

        await fixture.Drain();

        Assert.Equal([101L], fixture.CompletedIds());
        Assert.Equal([101L], fixture.DisplayedIds());
        Assert.Equal("NoRead", fixture.Store.GetPackageById(101)?.BarCodeInfo?.Barcode);
        Assert.Equal(2.75m, fixture.Store.GetPackageById(101)?.WeightInfo?.FormattedWeight);
        Assert.Collection(fixture.TakePersistenceInput(),
            item => Assert.Equal(2.75m, item.WeightInfo?.FormattedWeight));
        Assert.Collection(fixture.TakeUploadInput(), item =>
        {
            Assert.Equal("NoRead", item.Barcode);
            Assert.Equal(2.75m, item.Weight);
            Assert.Equal(101L, item.Guid);
        });
        Assert.Collection(fixture.Bus.Events.OfType<InstructionReceived>(),
            item => Assert.Equal("101", item.SortingCode));
        fixture.AssertNoErrors();
    }

    /// <summary>空读遵守相机窗口两端的包含规则，窗口外结果不会传入界面、持久化或上传。</summary>
    [Theory]
    [InlineData(49, false)]
    [InlineData(50, true)]
    [InlineData(500, true)]
    [InlineData(501, false)]
    [InlineData(600, false)]
    public async Task Enabled_NoRead_respects_window_boundaries_without_shifting_the_next_cart(int delay, bool accepted)
    {
        await using var fixture = new Fixture(useNoRead: true, cameras: 1, window: true, businessConsumers: true);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        fixture.Missed("camera-1", delay);
        fixture.Scanned("camera-1", "JT-102", 650);

        await fixture.Drain();

        Assert.Equal(accepted ? [101L, 102L] : [102L], fixture.CompletedIds());
        Assert.Equal(accepted ? [101L, 102L] : [102L], fixture.DisplayedIds());
        Assert.Equal(accepted, fixture.Store.GetPackageById(101) is not null);
        Assert.Equal("JT-102", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        var expected = accepted ? new[] { "NoRead", "JT-102" } : ["JT-102"];
        Assert.Equal(expected, fixture.TakePersistenceInput().Select(item => item.BarCodeInfo?.Barcode));
        Assert.Equal(expected, fixture.TakeUploadInput().Select(item => item.Barcode));
        Assert.Equal(expected.Length, fixture.Bus.Events.OfType<InstructionReceived>().Count());
        Assert.False(fixture.Store.HasUnassignedPackage());
        fixture.AssertNoErrors();
    }

    /// <summary>空读过滤只抑制间隔内的输出，达到配置边界恢复输出，过滤结果不延长下一次间隔。</summary>
    [Fact]
    public async Task Enabled_NoRead_filter_accepts_exact_interval_after_discarding_an_intermediate_cart()
    {
        await using var fixture = new Fixture(useNoRead: true, cameras: 1, filterNoRead: true, businessConsumers: true);
        fixture.CreateCart(101, 0);
        fixture.CreateCart(102, 300);
        fixture.CreateCart(103, 600);
        fixture.CreateCart(104, 900);
        fixture.Missed("camera-1", 650);
        fixture.Missed("camera-1", 1649);
        fixture.Missed("camera-1", 1650);
        fixture.Scanned("camera-1", "JT-104", 1850);

        await fixture.Drain();

        Assert.Equal([101L, 103L, 104L], fixture.CompletedIds());
        Assert.Equal([101L, 103L, 104L], fixture.DisplayedIds());
        Assert.Null(fixture.Store.GetPackageById(102));
        Assert.Equal("NoRead", fixture.Store.GetPackageById(103)?.BarCodeInfo?.Barcode);
        Assert.Equal("JT-104", fixture.Store.GetPackageById(104)?.BarCodeInfo?.Barcode);
        Assert.Equal(["NoRead", "NoRead", "JT-104"],
            fixture.TakePersistenceInput().Select(item => item.BarCodeInfo?.Barcode));
        Assert.Equal([101L, 103L, 104L], fixture.TakeUploadInput().Select(item => item.Guid));
        fixture.AssertNoErrors();
    }

    /// <summary>空读结果提前到达创建指令时等待本车，关闭窗口允许设备时间戳存在差异。</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    public async Task Enabled_NoRead_before_creation_waits_for_its_own_cart(int creationDelay)
    {
        await using var fixture = new Fixture(useNoRead: true, cameras: 1);
        fixture.Missed("camera-1", 200);
        await fixture.CheckCriticalState(() =>
        {
            Assert.Equal(0, fixture.Store.GetPackageCount());
            Assert.Empty(fixture.Bus.PackageEvents);
        });
        fixture.CreateCart(101, creationDelay);
        fixture.Missed("camera-1", 500);
        fixture.CreateCart(102, 300 + creationDelay);
        fixture.Scanned("camera-1", "JT-103", 800);
        fixture.CreateCart(103, 600 + creationDelay);

        await fixture.Drain();

        Assert.Equal([101L, 102L, 103L], fixture.CompletedIds());
        Assert.Equal([101L, 102L, 103L], fixture.DisplayedIds());
        Assert.Equal("NoRead", fixture.Store.GetPackageById(101)?.BarCodeInfo?.Barcode);
        Assert.Equal("NoRead", fixture.Store.GetPackageById(102)?.BarCodeInfo?.Barcode);
        Assert.Equal("JT-103", fixture.Store.GetPackageById(103)?.BarCodeInfo?.Barcode);
        fixture.AssertNoErrors();
    }

    /// <summary>连续密集空读全部完成，一段全空车不会关闭流水线或只保留最后一个结果。</summary>
    [Fact]
    public async Task Enabled_NoRead_keeps_every_result_in_a_dense_all_NoRead_run()
    {
        const int count = 4000;
        await using var fixture = new Fixture(useNoRead: true, cameras: 1);
        for (var index = 0; index < count; index++)
        {
            fixture.CreateCart(index + 1, index * 5);
        }
        for (var index = 0; index < count; index++)
        {
            fixture.Missed("camera-1", 100_000 + index);
        }

        await fixture.Drain();

        var expected = Enumerable.Range(1, count).Select(id => (long)id).ToArray();
        Assert.Equal(expected, fixture.CompletedIds());
        Assert.Equal(expected, fixture.DisplayedIds());
        Assert.Equal(count, fixture.Store.GetPackageCount());
        Assert.Equal(count, fixture.Bus.Events.OfType<InstructionReceived>().Count());
        Assert.All(fixture.Bus.PackageEvents.OfType<PackageInfo>(), package =>
        {
            Assert.True(package.IsCompleted);
            Assert.Equal("NoRead", package.BarCodeInfo?.Barcode);
        });
        Assert.False(fixture.Store.HasUnassignedPackage());
        fixture.AssertNoErrors();
    }

    /// <summary>开启空读时保留原图并转交存图通知，窗口外空读释放原图且不产生存图或完成通知。</summary>
    [Theory]
    [InlineData(50, true)]
    [InlineData(501, false)]
    public async Task Enabled_NoRead_image_ownership_follows_original_cart_acceptance(int delay, bool accepted)
    {
        await using var fixture = new Fixture(useNoRead: true, cameras: 1, window: true);
        using var image = new Bitmap(2, 2);
        image.SetPixel(0, 0, Color.Red);
        fixture.CreateCart(101, 0);
        var frame = fixture.MissedImage("camera-1", delay, image);

        await fixture.Drain();

        Assert.Null(frame.Image);
        if (accepted)
        {
            var package = Assert.IsType<PackageInfo>(fixture.Store.GetPackageById(101));
            Assert.True(package.IsImageSaveRequested);
            Assert.Null(package.Image);
            Assert.Collection(fixture.Bus.Events.OfType<ImageMessageInfo>(), message =>
            {
                Assert.Equal("NoRead", message.BarCode);
                Assert.Equal("camera-1", message.CameraSerialNumber);
                Assert.Equal(package.Timestamp, message.PackageTimestamped);
                Assert.Equal(frame.ScanTime, message.ScanTime);
                Assert.Same(image, message.Image!.As<Bitmap>());
                Assert.Equal(Color.Red.ToArgb(), image.GetPixel(0, 0).ToArgb());
            });
            Assert.Equal([101L], fixture.CompletedIds());
        }
        else
        {
            Assert.Empty(fixture.Bus.Events.OfType<ImageMessageInfo>());
            Assert.Empty(fixture.CompletedIds());
            Assert.Null(fixture.Store.GetPackageById(101));
            Assert.Throws<ArgumentException>(() => image.GetPixel(0, 0));
        }
        fixture.AssertNoErrors();
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly Workflow _workflow;
        private readonly DeviceService _device;
        private readonly SortingService _sorting;
        /// <summary>可选接入实际持久化服务，只验证入口队列，不启动数据库消费者。</summary>
        private readonly PersistenceWorkflow? _persistence;
        /// <summary>可选接入实际上传服务，只验证入口队列，不启动外部请求。</summary>
        private readonly UploadWorkflow? _uploads;
        /// <summary>实际上传服务需要的测试内存缓存。</summary>
        private readonly MemoryCache? _uploadCache;
        private readonly DateTime _origin = DateTime.Now.AddSeconds(-2);
        private bool _drained;
        public PackageSessionStore Store { get; } = new();
        public RecordingBus Bus { get; } = new();

        public Fixture(bool useNoRead, int cameras, bool filterNoRead = false, bool window = false,
            bool descending = false, WeightMode weightMode = WeightMode.None, bool businessConsumers = false, bool lowerRemoval = false)
        {
            _device = DispatchProxy.Create<DeviceService, RuntimeProxy>();
            _sorting = DispatchProxy.Create<SortingService, RuntimeProxy>();
            var images = DispatchProxy.Create<IImageStorageService, RuntimeProxy>();
            var settings = DispatchProxy.Create<ISettingsStore, RuntimeProxy>();
            var cameraConfigurations = DispatchProxy.Create<ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel>, RuntimeProxy>();
            _workflow = new Workflow(Store, _device, images, settings, _sorting, cameraConfigurations,
                DispatchProxy.Create<ExternalDataService, RuntimeProxy>(), Bus);
            if (businessConsumers)
            {
                _persistence = new PersistenceWorkflow(
                    DispatchProxy.Create<IPackageProcessingPersistence, RuntimeProxy>(), images, cameraConfigurations, Bus);
                _uploadCache = new MemoryCache(new MemoryCacheOptions());
                _uploads = new UploadWorkflow(DispatchProxy.Create<IProviderRegistry<IDataUploader>, RuntimeProxy>(),
                    settings, images, _uploadCache, Bus);
            }
            SetField("_createPackageSettingsDto", new CreatePackageSettingsDto
            {
                PackageCreationMethods = PackageCreationMethodsEnum.LowerMachineCreation,
                PackageRemoveMethods = lowerRemoval ? PackageRemoveMethodsEnum.LowerMachineRemoval : PackageRemoveMethodsEnum.FillInformation,
                IsUseNoRead = useNoRead,
                IsUseNoReadFilter = filterNoRead,
                FilterInterval = 1000,
                PackageCreationInterval = 150,
                BarcodeQueueOrder = descending ? BarcodeQueueOrderEnum.TimeDescending : BarcodeQueueOrderEnum.TimeAscending,
                IsUseBarcodeAssignmentInterval = window,
                MinimumAssignmentTime = 50,
                MaximumAssignmentTime = 500,
                ClearPackageQueueOnStop = true
            });
            SetField("_barcodeFilterSettingsDto", new BarcodeFilterSettingsDto { MergeTimeout = 60_000 });
            SetField("_weightSettingsDto", new WeightSettingsDto { Mode = weightMode });
            SetField("_scannerCameraCount", cameras);
        }

        public void CreateCart(long id, int offset) => ((RuntimeProxy)(object)_sorting).Raise(
            "CreatePackageEvent", new PackageInstruction
            {
                Keyword = id.ToString(),
                Instruction = $"create:{id}",
                ConnectionName = "test-machine",
                InstructionTime = _origin.AddMilliseconds(offset)
            });

        public void CallbackCart(long id) => ((RuntimeProxy)(object)_sorting).Raise(
            "RemovePackageEvent", new PackageInstruction
            {
                Keyword = id.ToString(), Instruction = $"fall:{id}",
                ConnectionName = "test-machine", InstructionTime = DateTime.Now
            });

        public void Missed(string serialNumber, int offset) => ((RuntimeProxy)(object)_device).Raise(
            "BarcodeMissed", Frame(serialNumber, "NoRead", offset));

        public void Scanned(string serialNumber, string barcode, int offset) => ((RuntimeProxy)(object)_device).Raise(
            "BarcodeScanned", Frame(serialNumber, barcode, offset));

        /// <summary>模拟实际磅秤返回稳定重量，验证空读不会提前完成或跳过称重。</summary>
        public void Weighed(decimal weight) => ((RuntimeProxy)(object)_device).Raise(
            "StableWeight", new StableWeight { Weight = weight });

        /// <summary>把真实位图交给空读事件，观察原图转交和拒绝释放。</summary>
        public BarcodeReadEventArgs MissedImage(string serialNumber, int offset, Bitmap image)
        {
            var frame = Frame(serialNumber, "NoRead", offset);
            frame.Image = image;
            ((RuntimeProxy)(object)_device).Raise("BarcodeMissed", frame);
            return frame;
        }

        /// <summary>等待先前关键事件处理完成，在同一线程中验证尚未完成的包裹状态。</summary>
        public Task CheckCriticalState(Action assertion) => QueueCriticalAction(assertion);

        /// <summary>在关键队列上执行真实周期清理，验证回执生命周期。</summary>
        public void CleanupCompletedPackages() => typeof(Workflow).GetMethod(
            "RemoveCompletedPackagesReadyForCleanup", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_workflow, null);

        /// <summary>读取实际持久化服务收到的主记录工作项；测试不会连接生产数据库。</summary>
        public PackageInfoModel[] TakePersistenceInput() => TakeQueuedInput<PackageInfoModel>(_persistence!, "_insertItems");

        /// <summary>读取实际上传服务收到的请求工作项；测试不会调用外部接口。</summary>
        public UploadItem[] TakeUploadInput() => TakeQueuedInput<UploadItem>(_uploads!, "_submitItems");

        /// <summary>在后台事件已经排空后，直接读取真实业务消费者的无损入口队列。</summary>
        private static T[] TakeQueuedInput<T>(object consumer, string fieldName)
        {
            var queue = (LosslessWorkQueue<T>)consumer.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(consumer)!;
            var items = new List<T>();
            while (queue.TryDequeue(out var item))
            {
                items.Add(item);
            }
            return [.. items];
        }

        /// <summary>模拟相机内部过滤全部条码，但仍转交该触发的消费结果。</summary>
        public void Filtered(string serialNumber, int offset) => ((RuntimeProxy)(object)_device).Raise(
            "BarcodeFiltered", Frame(serialNumber, "filtered", offset));

        /// <summary>独立控制帧号与观察时间，覆盖时间戳碰撞时的密集回调。</summary>
        public void ScannedFrame(string serialNumber, string barcode, int offset, long frameNo)
        {
            var frame = Frame(serialNumber, barcode, offset);
            frame.FrameNo = frameNo;
            // 固件 SDK 帧号可能无效地固定为零，不能用它压掉不同的实际最终结果。
            frame.SdkFrameNo = 0;
            ((RuntimeProxy)(object)_device).Raise("BarcodeScanned", frame);
        }

        private BarcodeReadEventArgs Frame(string serialNumber, string barcode, int offset) => new()
        {
            Barcode = barcode,
            CameraSerialNumber = serialNumber,
            ScanTime = _origin.AddMilliseconds(offset),
            FrameNo = offset,
            Timestamp = new DateTimeOffset(_origin.AddMilliseconds(offset)).ToUnixTimeMilliseconds()
        };

        public long[] CompletedIds() => Bus.PackageEvents.OfType<PackageInfo>().Select(package => package.Guid).ToArray();
        public long[] DisplayedIds() => Bus.Events.OfType<PackageInfo>().Select(package => package.Guid).ToArray();

        public void AssertNoErrors() => Assert.DoesNotContain(Bus.Events.OfType<AppLogInfoModel>(),
            item => item.Type == LogType.Exception);

        /// <summary>模拟真实过期回调先移除原车，保持工作器中的消费标记。</summary>
        public async Task RemoveCart(long id)
        {
            await QueueCriticalAction(() => Store.RemovePackage(Store.GetPackageById(id)!.CreateTime, "测试过期"));
        }

        /// <summary>模拟设备已经停止后的应用事件，并等待有序队列完成消费状态重置。</summary>
        public async Task StopDeviceSession()
        {
            Bus.Publish(new ApplicationStatusChanged { Status = ApplicationStatus.Stop });
            await QueueCriticalAction(() => { });
        }

        /// <summary>确定性地结束首个多相机组，不依赖测试机线程调度或等待计时器。</summary>
        public async Task ExpireFirstFusion() => await QueueCriticalAction(() =>
        {
            var groups = (System.Collections.IDictionary)typeof(Workflow)
                .GetField("_lowerMachineCameraGroups", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_workflow)!;
            var group = groups[1L]!;
            group.GetType().GetProperty("IsReady")!.SetValue(group, true);
            typeof(Workflow).GetMethod("DrainLowerMachineCameraGroups", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(_workflow, null);
        });

        private async Task QueueCriticalAction(Action action)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Action work = () =>
            {
                try { action(); completion.TrySetResult(); }
                catch (Exception exception) { completion.TrySetException(exception); }
            };
            var accepted = (bool)typeof(Workflow)
                .GetMethod("TryQueuePackageEvent", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(_workflow, [work, "测试屏障"])!;
            Assert.True(accepted);
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        public async Task Drain()
        {
            await _workflow.StopAsync(CancellationToken.None);
            _drained = true;
        }

        private void SetField(string name, object value) =>
            typeof(Workflow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_workflow, value);

        public async ValueTask DisposeAsync()
        {
            if (!_drained)
            {
                await Drain();
            }
            if (_uploads is not null)
            {
                await _uploads.StopAsync(CancellationToken.None);
                _uploads.Dispose();
            }
            _persistence?.Dispose();
            _uploadCache?.Dispose();
            foreach (var message in Bus.Events.OfType<ImageMessageInfo>())
            {
                message.Image?.Dispose();
            }
            Store.ClearAllPackages();
            _workflow.Dispose();
        }
    }

    /// <summary>只保存事件订阅的接口代理，不调用相机、下位机或持久化实现。</summary>
    public class RuntimeProxy : DispatchProxy
    {
        private readonly Dictionary<string, Delegate> _handlers = [];

        /// <summary>模拟事件订阅和无副作用的默认返回值。</summary>
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var method = targetMethod!;
            if (method.Name.StartsWith("add_", StringComparison.Ordinal))
            {
                var eventName = method.Name[4..];
                var handler = (Delegate)args![0]!;
                _handlers[eventName] = _handlers.TryGetValue(eventName, out var existing)
                    ? Delegate.Combine(existing, handler)
                    : handler;
                return null;
            }
            if (method.Name.StartsWith("remove_", StringComparison.Ordinal))
            {
                return null;
            }
            return method.ReturnType == typeof(void) ? null :
                method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
        }

        /// <summary>向真实后台流程调用已经注册的事件处理程序。</summary>
        public void Raise(string name, object args) => _handlers[name].DynamicInvoke(this, args);
    }

    private sealed class RecordingBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, Delegate> _subscriptions = new();
        /// <summary>保存实际持久化和上传入口的包裹通道订阅，分别于普通界面通知。</summary>
        private readonly ConcurrentDictionary<Type, Delegate> _packageSubscriptions = new();
        public ConcurrentQueue<object> Events { get; } = new();
        public ConcurrentQueue<object> PackageEvents { get; } = new();
        public void Publish<TEvent>(TEvent eventData)
        {
            Events.Enqueue(eventData!);
            if (_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
            {
                ((Action<TEvent>)handlers)(eventData);
            }
        }
        /// <summary>记录完成事件并转交实际业务消费者的同步入口。</summary>
        public void PublishPackage<TEvent>(TEvent eventData)
        {
            PackageEvents.Enqueue(eventData!);
            if (_packageSubscriptions.TryGetValue(typeof(TEvent), out var handlers))
            {
                ((Action<TEvent>)handlers)(eventData);
            }
        }
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            _subscriptions.AddOrUpdate(typeof(TEvent), handler, (_, existing) => Delegate.Combine(existing, handler));
            return new Subscription();
        }
        public IDisposable SubscribeAsync<TEvent>(Func<TEvent, Task> handler) => new Subscription();
        /// <summary>接入实际业务服务，测试其包裹完成后的转换与入队逻辑。</summary>
        public IDisposable SubscribePackage<TEvent>(Action<TEvent> handler)
        {
            _packageSubscriptions.AddOrUpdate(typeof(TEvent), handler, (_, existing) => Delegate.Combine(existing, handler));
            return new Subscription();
        }
        public IDisposable SubscribePackageAsync<TEvent>(Func<TEvent, Task> handler) => new Subscription();
        public void Unsubscribe<TEvent>(Action<TEvent> handler) { }
        public void UnsubscribePackage<TEvent>(Action<TEvent> handler) { }

        private sealed class Subscription : IDisposable
        {
            public void Dispose() { }
        }
    }
}
