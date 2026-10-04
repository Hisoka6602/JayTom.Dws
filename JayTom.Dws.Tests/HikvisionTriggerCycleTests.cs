using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Camera.Concurrency;
using MvCodeReaderSDKNet;

namespace JayTom.Dws.Tests;

/// <summary>验证海康同一次硬件触发的全部帧只产生一个 NoRead 或一组去重后的有效条码。</summary>
public sealed class HikvisionTriggerCycleTests {
    /// <summary>五张空帧须等到触发结束和队列排空后才发布一个结果。</summary>
    [Fact]
    public void Empty_frames_wait_for_end_and_sdk_drain_then_publish_once() {
        using var cycles = new HikvisionTriggerCycles();
        cycles.OnTrigger(42, true, 1);
        var first = Candidate(42, 1);
        cycles.HoldNoRead(42, first);
        for (uint frame = 2; frame <= 5; frame++) {
            cycles.HoldNoRead(42, Candidate(42, frame));
        }

        Assert.Empty(cycles.CompleteDrained(1));
        cycles.OnTrigger(42, false, 2);
        Assert.Empty(cycles.CompleteDrained(1));
        Assert.Same(first, OnlyResult(cycles.CompleteDrained(2)));
        cycles.OnTrigger(42, false, 3);
        cycles.HoldNoRead(42, Candidate(42, 6));
        Assert.Empty(cycles.CompleteDrained(3));
    }

    /// <summary>成功帧无论在结束回调之前或之后到达，都能取消尚未发布的空帧结果。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Barcode_before_drain_cancels_noread_even_when_end_arrives_first(bool endBeforeBarcode) {
        using var cycles = new HikvisionTriggerCycles();
        cycles.OnTrigger(7, true, 1);
        var image = new Bitmap(2, 2);
        var thumbnail = new Bitmap(1, 1);
        cycles.HoldNoRead(7, new BarcodeReadEventArgs { Image = image, ThumbImage = thumbnail });
        if (endBeforeBarcode) {
            cycles.OnTrigger(7, false, 2);
        }
        cycles.ObserveFrame(7, true);
        cycles.OnTrigger(7, false, 3);
        cycles.HoldNoRead(7, Candidate(7, 3));

        Assert.Empty(cycles.CompleteDrained(3));
        Assert.Throws<ArgumentException>(() => image.GetPixel(0, 0));
        Assert.Throws<ArgumentException>(() => thumbnail.GetPixel(0, 0));
    }

    /// <summary>触发结束回调抢先到达时仍接收后续图像。</summary>
    [Fact]
    public void End_before_any_frame_does_not_discard_the_later_frame() {
        using var cycles = new HikvisionTriggerCycles();
        cycles.OnTrigger(8, false, 1);
        Assert.Empty(cycles.CompleteDrained(1));
        cycles.HoldNoRead(8, Candidate(8, 1));
        Assert.Equal((uint)8, OnlyResult(cycles.CompleteDrained(1)).TriggerIndex);
    }

    /// <summary>全过滤触发仍有一个内部最终结果，防止下位机消费位置少一位。</summary>
    [Fact]
    public void Fully_filtered_trigger_retains_one_internal_consumption_result() {
        using var cycles = new HikvisionTriggerCycles();
        cycles.OnTrigger(42, true, 1);
        cycles.ObserveFrame(42, true);
        cycles.HoldBarcode(42, new BarcodeTriggeredEventArgs { Barcode = "filtered", IsFiltered = true, TriggerIndex = 42 });
        cycles.HoldBarcode(42, new BarcodeTriggeredEventArgs { Barcode = "filtered", IsFiltered = true, TriggerIndex = 42 });
        cycles.OnTrigger(42, false, 2);

        var result = OnlyResult(cycles.CompleteDrained(2));
        Assert.True(result.IsFiltered);
        Assert.Equal((uint)42, result.TriggerIndex);
        Assert.Empty(cycles.CompleteDrained(2));
    }

    /// <summary>同周期有效码替换过滤占位，过滤帧到达顺序不改变最终结果。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Accepted_barcode_wins_over_filtered_placeholder(bool filteredFirst) {
        using var cycles = new HikvisionTriggerCycles();
        var filtered = new BarcodeTriggeredEventArgs { Barcode = "filtered", IsFiltered = true };
        var valid = new BarcodeTriggeredEventArgs { Barcode = "JT-42" };
        cycles.OnTrigger(42, true, 1);
        cycles.HoldBarcode(42, filteredFirst ? filtered : valid);
        cycles.HoldBarcode(42, filteredFirst ? valid : filtered);
        cycles.OnTrigger(42, false, 2);

        Assert.Same(valid, OnlyResult(cycles.CompleteDrained(2)));
    }

    /// <summary>短时间内相邻的不同触发各自保留一个结果。</summary>
    [Fact]
    public void Distinct_fast_triggers_are_not_merged_by_a_time_window() {
        using var cycles = new HikvisionTriggerCycles();
        cycles.OnTrigger(10, true, 1);
        cycles.HoldNoRead(10, Candidate(10, 1));
        cycles.OnTrigger(10, false, 2);
        cycles.OnTrigger(11, true, 3);
        cycles.HoldNoRead(11, Candidate(11, 2));
        cycles.OnTrigger(11, false, 4);

        Assert.Equal(new uint?[] { 10, 11 }, cycles.CompleteDrained(4).Select(item => item.TriggerIndex));
    }

    /// <summary>电平触发号为零仍然有效，重启后可以重新从零计数。</summary>
    [Fact]
    public void Level_trigger_zero_can_be_reused_after_restart() {
        using var cycles = new HikvisionTriggerCycles();
        cycles.OnTrigger(0, true, 1);
        cycles.HoldNoRead(0, Candidate(0, 1));
        cycles.OnTrigger(0, false, 2);
        OnlyResult(cycles.CompleteDrained(2));
        cycles.Dispose();
        cycles.OnTrigger(0, true, 3);
        cycles.HoldNoRead(0, Candidate(0, 2));
        cycles.OnTrigger(0, false, 4);
        OnlyResult(cycles.CompleteDrained(4));
    }

    /// <summary>重复空帧及中断的周期及时释放原图和缩略图。</summary>
    [Fact]
    public void Repeated_empty_images_and_aborted_cycles_release_their_bitmaps() {
        using var cycles = new HikvisionTriggerCycles();
        var first = new Bitmap(2, 2);
        var repeated = new Bitmap(2, 2);
        cycles.HoldNoRead(12, new BarcodeReadEventArgs { Image = first, ThumbImage = first });
        cycles.HoldNoRead(12, new BarcodeReadEventArgs { Image = repeated, ThumbImage = repeated });
        Assert.Throws<ArgumentException>(() => repeated.GetPixel(0, 0));
        cycles.Dispose();
        Assert.Throws<ArgumentException>(() => first.GetPixel(0, 0));
        Assert.Empty(cycles.CompleteDrained(long.MaxValue));
    }

    /// <summary>固件缺失结束通知时，待完成周期不能无限保留图片。</summary>
    [Fact]
    public void Missing_end_callbacks_cannot_retain_unlimited_images() {
        using var cycles = new HikvisionTriggerCycles();
        var held = new Bitmap(1, 1);
        cycles.HoldNoRead(1, new BarcodeReadEventArgs { Image = held });
        for (uint id = 2; id <= 64; id++) {
            cycles.OnTrigger(id, true, id);
        }
        Assert.Throws<InvalidOperationException>(() => cycles.OnTrigger(65, true, 65));
        Assert.Throws<ArgumentException>(() => held.GetPixel(0, 0));
    }

    /// <summary>关闭触发或隐藏未读码结果时不产生空帧事件。</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Adapter_does_not_publish_noread_when_disabled_or_hidden(bool triggerEnabled, bool hideNoRead) {
        var camera = new HikvisionSmartCamera {
            IsUseTriggerMode = triggerEnabled,
            TriggerMode = TriggerMode.Hardware,
            IsHideNoRead = hideNoRead,
            IsOriginalImageOut = false
        };
        var count = 0;
        camera.NotBarcodeHitEvent += (_, _) => count++;
        for (uint frame = 1; frame <= 5; frame++) {
            DeliverEmptyFrame(camera, 42, frame);
        }
        Assert.Equal(0, count);
    }

    /// <summary>相机适配器将同一触发的五张空帧交给周期管理器暂存。</summary>
    [Fact]
    public void Adapter_buffers_five_frames_for_one_real_trigger() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(HikvisionSmartCamera).GetField("_triggerCallbacksRegistered", flags)!.SetValue(camera, true);
        typeof(HikvisionSmartCamera).GetField("_triggerBoundaryObserved", flags)!.SetValue(camera, 1);
        typeof(HikvisionSmartCamera).GetField("_triggerCorrelationMode", flags)!.SetValue(camera, 1);
        using var cycles = (HikvisionTriggerCycles)typeof(HikvisionSmartCamera).GetField("_triggerCycles", flags)!.GetValue(camera)!;
        cycles.OnTrigger(42, true, 1);
        var count = 0;
        camera.NotBarcodeHitEvent += (_, _) => count++;
        for (uint frame = 1; frame <= 5; frame++) {
            DeliverEmptyFrame(camera, 42, frame);
        }

        Assert.Equal(0, count);
        cycles.OnTrigger(42, false, 2);
        var completed = OnlyResult(cycles.CompleteDrained(2));
        Assert.Equal((uint)42, completed.TriggerIndex);
        Assert.Equal((uint)1, completed.SdkFrameNo);
    }

    /// <summary>边沿模式的无效零触发号不得把两个采集结果合并。</summary>
    [Fact]
    public void Edge_frames_with_zero_trigger_number_are_not_merged() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        var received = new List<BarcodeReadEventArgs>();
        camera.NotBarcodeHitEvent += (_, args) => received.Add(args);
        DeliverEmptyFrame(camera, 0, 1);
        DeliverEmptyFrame(camera, 0, 2);
        Assert.Equal(new uint?[] { 1, 2 }, received.Select(args => args.SdkFrameNo));
        Assert.All(received, args => Assert.Null(args.TriggerIndex));
    }

    /// <summary>TriggerActivation 读取失败不能再被当成边沿触发，仍须尝试注册 SDK 周期回调。</summary>
    [Fact]
    public void Unknown_trigger_activation_requests_sdk_boundary_callback() {
        var camera = new HikvisionSmartCamera {
            IsUseTriggerMode = true,
            TriggerMode = TriggerMode.Hardware,
            BindingType = CameraBindingType.ScannerCamera
        };
        SetField(camera, "_triggerActivationKnown", false);
        SetField(camera, "_isLevelTrigger", false);

        Assert.True((bool)Invoke(camera, "ShouldRegisterTriggerCycleCallback")!);

        SetField(camera, "_triggerActivationKnown", true);
        Assert.False((bool)Invoke(camera, "ShouldRegisterTriggerCycleCallback")!);
    }

    /// <summary>SDK 仅返回注册成功但固件不送边界时，降级按帧内触发号发布一个 NoRead。</summary>
    [Fact]
    public void Missing_boundary_uses_frame_trigger_index_instead_of_dropping_result() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        SetField(camera, "_triggerCallbacksRegistered", true);
        var received = new List<BarcodeReadEventArgs>();
        camera.NotBarcodeHitEvent += (_, args) => received.Add(args);

        ActivateFrameTriggerFallback(camera, 42, 1, false);
        DeliverEmptyFrame(camera, 42, 1);

        Assert.Equal((uint)42, OnlyResult(received).TriggerIndex);
        Assert.False((bool)Invoke(camera, "get_HasReliableTriggerBoundary")!);
        Assert.True((bool)Invoke(camera, "get_UsesFrameTriggerIndex")!);
    }

    /// <summary>同一触发的重复条码帧只保留第一条，并在周期排空后发布一次。</summary>
    [Fact]
    public void Repeated_barcode_frames_publish_one_final_barcode() {
        using var cycles = new HikvisionTriggerCycles();
        cycles.OnTrigger(42, true, 1);
        var first = BarcodeCandidate(42, 1, "JT001");
        var duplicateImage = new Bitmap(1, 1);
        var duplicate = BarcodeCandidate(42, 2, "JT001", duplicateImage);

        cycles.HoldBarcode(42, first);
        cycles.HoldBarcode(42, duplicate);
        Assert.Empty(cycles.CompleteDrained(1));
        cycles.OnTrigger(42, false, 2);

        var completed = Assert.IsType<BarcodeTriggeredEventArgs>(OnlyResult(cycles.CompleteDrained(2)));
        Assert.Same(first, completed);
        Assert.Equal("JT001", completed.Barcode);
        Assert.Throws<ArgumentException>(() => duplicateImage.GetPixel(0, 0));
    }

    /// <summary>同一触发识别出不同条码时保留全部唯一值，并统一分组时间交给业务多条码规则。</summary>
    [Fact]
    public void Distinct_barcodes_are_kept_in_the_same_trigger_group() {
        using var cycles = new HikvisionTriggerCycles();
        var firstScanTime = new DateTime(2026, 9, 20, 10, 0, 0, 100);
        var secondScanTime = firstScanTime.AddMilliseconds(60);
        cycles.OnTrigger(7, true, 1);
        var first = BarcodeCandidate(7, 1, "JT-A");
        first.ScanTime = firstScanTime;
        cycles.HoldBarcode(7, first);

        var second = BarcodeCandidate(7, 2, "JT-B");
        second.ScanTime = secondScanTime;
        cycles.HoldBarcode(7, second);
        cycles.OnTrigger(7, false, 2);

        var completed = cycles.CompleteDrained(2).Cast<BarcodeTriggeredEventArgs>().ToList();
        Assert.Equal(new[] { "JT-A", "JT-B" }, completed.Select(item => item.Barcode));
        Assert.All(completed, item => Assert.Equal(firstScanTime, item.ScanTime));
    }

    /// <summary>回调虽注册但固件不送真实边界时，帧内触发号仍让有效条码正常进入业务队列。</summary>
    [Fact]
    public void Barcode_is_published_once_through_frame_trigger_fallback() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        SetField(camera, "_triggerCallbacksRegistered", true);
        var received = new List<BarcodeReadEventArgs>();
        camera.BarcodeReadTriggered += (_, args) => received.Add(args);
        camera.NotBarcodeHitEvent += (_, args) => received.Add(args);

        ActivateFrameTriggerFallback(camera, 42, 1, true);
        DeliverBarcodeFrame(camera, 42, 1, "JT001");
        ActivateFrameTriggerFallback(camera, 42, 2, true);
        DeliverBarcodeFrame(camera, 42, 2, "JT001");
        ActivateFrameTriggerFallback(camera, 42, 3, false);
        DeliverEmptyFrame(camera, 42, 3);

        var result = Assert.IsType<BarcodeTriggeredEventArgs>(OnlyResult(received));
        Assert.Equal("JT001", result.Barcode);
        Assert.Equal((uint)42, result.TriggerIndex);
    }

    /// <summary>帧触发号变化后必须允许下一包裹发布，不能把整个采集会话只保留一个结果。</summary>
    [Fact]
    public void Frame_trigger_fallback_accepts_one_result_for_each_trigger_index() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        SetField(camera, "_triggerCallbacksRegistered", true);
        var received = new List<BarcodeReadEventArgs>();
        camera.BarcodeReadTriggered += (_, args) => received.Add(args);

        ActivateFrameTriggerFallback(camera, 41, 1, true);
        DeliverBarcodeFrame(camera, 41, 1, "JT041");
        ActivateFrameTriggerFallback(camera, 42, 2, true);
        DeliverBarcodeFrame(camera, 42, 2, "JT042");

        Assert.Equal(new[] { "JT041", "JT042" }, received.Select(args => args.Barcode));
        Assert.Equal(new uint?[] { 41, 42 }, received.Select(args => args.TriggerIndex));
    }

    /// <summary>同一帧触发号中的不同条码必须全部进入业务层，并使用同一分组时间触发多条码规则。</summary>
    [Fact]
    public void Frame_trigger_fallback_publishes_distinct_barcodes_as_one_group() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        SetField(camera, "_triggerCallbacksRegistered", true);
        var received = new List<BarcodeReadEventArgs>();
        camera.BarcodeReadTriggered += (_, args) => received.Add(args);

        ActivateFrameTriggerFallback(camera, 42, 1, true);
        DeliverBarcodeFrame(camera, 42, 1, "JT-A", "JT-B");

        Assert.Equal(new[] { "JT-A", "JT-B" }, received.Select(args => args.Barcode));
        Assert.Equal(received[0].ScanTime, received[1].ScanTime);
        Assert.All(received, args => Assert.Equal((uint)42, args.TriggerIndex));
    }

    /// <summary>同一触发跨帧识别到的不同条码也必须共用分组时间，避免被误配到后续包裹。</summary>
    [Fact]
    public void Frame_trigger_fallback_groups_distinct_barcodes_across_frames() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        SetField(camera, "_triggerCallbacksRegistered", true);
        var received = new List<BarcodeReadEventArgs>();
        camera.BarcodeReadTriggered += (_, args) => received.Add(args);

        ActivateFrameTriggerFallback(camera, 42, 1, true);
        DeliverBarcodeFrame(camera, 42, 1, "JT-A");
        ActivateFrameTriggerFallback(camera, 42, 2, true);
        DeliverBarcodeFrame(camera, 42, 2, "JT-B");

        Assert.Equal(new[] { "JT-A", "JT-B" }, received.Select(args => args.Barcode));
        Assert.Equal(received[0].ScanTime, received[1].ScanTime);
    }

    /// <summary>周期排空入口根据最终结果类型只发布一个条码事件。</summary>
    [Fact]
    public async Task Drained_trigger_publishes_one_buffered_barcode_event() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        SetField(camera, "_triggerCallbacksRegistered", true);
        SetField(camera, "_triggerBoundaryObserved", 1);
        SetField(camera, "_triggerCorrelationMode", 1);
        SetField(camera, "_acceptFrames", 1);
        Invoke(camera, "EnsureFrameDispatcher");
        using var dispatcher = GetField<LosslessOrderedDispatcher<Action>>(camera, "_frameDispatcher");
        var cycles = GetField<HikvisionTriggerCycles>(camera, "_triggerCycles");
        var received = new List<BarcodeReadEventArgs>();
        camera.BarcodeReadTriggered += (_, args) => received.Add(args);
        cycles.OnTrigger(88, true, 1);
        cycles.HoldBarcode(88, BarcodeCandidate(88, 1, "JT088"));
        cycles.HoldBarcode(88, BarcodeCandidate(88, 2, "JT088"));
        cycles.OnTrigger(88, false, 2);

        Invoke(camera, "QueueDrainedTriggerCycles", 2L, 0);
        await Flush(dispatcher);

        Assert.Equal("JT088", OnlyResult(received).Barcode);
        await (Task)Invoke(camera, "ResetTriggerCyclesAsync")!;
    }

    /// <summary>实际原生回调入口和顺序调度队列在排空后只发布一次事件。</summary>
    [Fact]
    public async Task Native_boundary_callback_and_frame_queue_publish_one_result_after_drain() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        SetField(camera, "_triggerCallbacksRegistered", true);
        SetField(camera, "_triggerBoundaryObserved", 1);
        SetField(camera, "_triggerCorrelationMode", 1);
        SetField(camera, "_acceptFrames", 1);
        Invoke(camera, "EnsureFrameDispatcher");
        using var dispatcher = GetField<LosslessOrderedDispatcher<Action>>(camera, "_frameDispatcher");
        var received = new List<BarcodeReadEventArgs>();
        camera.NotBarcodeHitEvent += (_, args) => received.Add(args);
        DeliverBoundary(camera, 42, true);
        for (uint frame = 1; frame <= 5; frame++) {
            var captured = frame;
            dispatcher.TryEnqueue(() => DeliverEmptyFrame(camera, 42, captured));
        }
        await Flush(dispatcher);
        Assert.Empty(received);
        DeliverBoundary(camera, 42, false);
        Invoke(camera, "QueueDrainedTriggerCycles", 2L, 0);
        Invoke(camera, "QueueDrainedTriggerCycles", 2L, 0);
        await Flush(dispatcher);
        Assert.Equal((uint)42, OnlyResult(received).TriggerIndex);
        await (Task)Invoke(camera, "ResetTriggerCyclesAsync")!;
    }

    /// <summary>会话切换清理图片，并拒绝旧会话的排空通知完成新触发。</summary>
    [Fact]
    public async Task Reset_releases_pending_image_and_rejects_old_generation_drain() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        SetField(camera, "_triggerCallbacksRegistered", true);
        SetField(camera, "_triggerBoundaryObserved", 1);
        SetField(camera, "_triggerCorrelationMode", 1);
        SetField(camera, "_acceptFrames", 1);
        Invoke(camera, "EnsureFrameDispatcher");
        using var dispatcher = GetField<LosslessOrderedDispatcher<Action>>(camera, "_frameDispatcher");
        var cycles = GetField<HikvisionTriggerCycles>(camera, "_triggerCycles");
        var image = new Bitmap(2, 2);
        var received = new List<BarcodeReadEventArgs>();
        camera.NotBarcodeHitEvent += (_, args) => received.Add(args);
        dispatcher.TryEnqueue(() => {
            cycles.HoldNoRead(0, new BarcodeReadEventArgs { Image = image });
            cycles.OnTrigger(0, false, 1);
        });
        SetField(camera, "_acceptFrames", 0);
        SetField(camera, "_captureGeneration", 1);
        await (Task)Invoke(camera, "ResetTriggerCyclesAsync")!;
        Assert.Throws<ArgumentException>(() => image.GetPixel(0, 0));
        SetField(camera, "_acceptFrames", 1);
        dispatcher.TryEnqueue(() => {
            cycles.HoldNoRead(0, Candidate(0, 10));
            cycles.OnTrigger(0, false, 2);
        });
        Invoke(camera, "QueueDrainedTriggerCycles", 2L, 0);
        await Flush(dispatcher);
        Assert.Empty(received);
        Invoke(camera, "QueueDrainedTriggerCycles", 2L, 1);
        await Flush(dispatcher);
        Assert.Equal((uint)10, OnlyResult(received).SdkFrameNo);
        await (Task)Invoke(camera, "ResetTriggerCyclesAsync")!;
    }

    /// <summary>即使没有成功事件订阅者，帧处理入口仍取消已读码周期的空帧结果。</summary>
    [Fact]
    public void Sdk_success_cancels_empty_cycle_even_without_a_barcode_subscriber() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        SetField(camera, "_triggerCallbacksRegistered", true);
        SetField(camera, "_triggerBoundaryObserved", 1);
        SetField(camera, "_triggerCorrelationMode", 1);
        using var cycles = GetField<HikvisionTriggerCycles>(camera, "_triggerCycles");
        var image = new Bitmap(2, 2);
        cycles.HoldNoRead(42, new BarcodeReadEventArgs { Image = image });
        cycles.OnTrigger(42, false, 1);
        using var buffer = PooledFrameBuffer.CopyFrom(new byte[4], 4);
        var frame = new HikvisionCapturedFrame(buffer,
            new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 { nTriggerIndex = 42 },
            new MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2 { nCodeNum = 1 }, DateTime.Now, 0, 1);
        Invoke(camera, "ProcessCapturedFrame", frame);
        Assert.Empty(cycles.CompleteDrained(1));
        Assert.Throws<ArgumentException>(() => image.GetPixel(0, 0));
        Assert.Throws<ObjectDisposedException>(() => buffer.Buffer);
    }

    /// <summary>真实帧处理入口对全部过滤的元数据保留无图片消费结果。</summary>
    [Fact]
    public void Adapter_retains_fully_filtered_frame_as_internal_result_without_bitmap() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        SetField(camera, "_triggerCallbacksRegistered", true);
        SetField(camera, "_triggerBoundaryObserved", 1);
        SetField(camera, "_triggerCorrelationMode", 1);
        using var cycles = GetField<HikvisionTriggerCycles>(camera, "_triggerCycles");
        camera.BarcodeReadTriggered += (_, _) => Assert.Fail("Filtered metadata must not publish a business barcode.");
        camera.FilteredBarcodeReturned += (_, _) => { };
        using var buffer = PooledFrameBuffer.CopyFrom(new byte[4], 4);
        var frame = new HikvisionCapturedFrame(buffer,
            new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 { nTriggerIndex = 42 },
            new MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2 { nCodeNum = 1 }, DateTime.Now, 0, 1);

        Invoke(camera, "ProcessCapturedFrame", frame);
        cycles.OnTrigger(42, false, 1);
        var result = OnlyResult(cycles.CompleteDrained(1));

        Assert.True(result.IsFiltered);
        Assert.Null(result.Image);
        Assert.Equal((uint)42, result.TriggerIndex);
        Assert.Throws<ObjectDisposedException>(() => buffer.Buffer);
    }

    /// <summary>在不连接物理相机的情况下调用适配器内部处理入口。</summary>
    private static object? Invoke(HikvisionSmartCamera camera, string method, params object?[] arguments) =>
        typeof(HikvisionSmartCamera).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(camera, arguments);

    /// <summary>读取适配器内部状态以核对资源和事件行为。</summary>
    private static T GetField<T>(HikvisionSmartCamera camera, string name) =>
        (T)typeof(HikvisionSmartCamera).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(camera)!;

    /// <summary>设置无需物理设备的采集会话测试条件。</summary>
    private static void SetField(HikvisionSmartCamera camera, string name, object value) =>
        typeof(HikvisionSmartCamera).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(camera, value);

    /// <summary>等待队列中的已有工作项完成，并限制测试等待时长。</summary>
    private static async Task Flush(LosslessOrderedDispatcher<Action> dispatcher) {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.True(dispatcher.TryEnqueue(() => completion.SetResult()));
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>构造与原生触发回调一致的结构体和指针生命周期。</summary>
    private static void DeliverBoundary(HikvisionSmartCamera camera, uint trigger, bool started) {
        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<MvCodeReader.MV_CODEREADER_TRIGGER_INFO_DATA>());
        try {
            Marshal.StructureToPtr(new MvCodeReader.MV_CODEREADER_TRIGGER_INFO_DATA {
                nTriggerIndex = trigger,
                nTriggerFlag = started ? 1u : 0u
            }, pointer, false);
            Invoke(camera, "TriggerCallback", pointer, IntPtr.Zero);
        }
        finally {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>断言结果集中只有一个元素并返回它。</summary>
    private static BarcodeReadEventArgs OnlyResult(IReadOnlyList<BarcodeReadEventArgs> results) {
        Assert.Collection(results, result => Assert.NotNull(result));
        return results[0];
    }

    /// <summary>构造不带图片的空帧候选结果。</summary>
    private static BarcodeReadEventArgs Candidate(uint trigger, uint frame) => new() {
        TriggerIndex = trigger,
        SdkFrameNo = frame,
        Barcode = "NoRead"
    };

    /// <summary>构造可由周期管理器取得所有权的条码候选。</summary>
    private static BarcodeTriggeredEventArgs BarcodeCandidate(
        uint trigger,
        uint frame,
        string barcode,
        Bitmap? image = null) => new() {
        TriggerIndex = trigger,
        SdkFrameNo = frame,
        Barcode = barcode,
        Image = image,
        ThumbImage = image
    };

    /// <summary>模拟生产处理入口在过滤前锁定帧触发号降级并记录 SDK 原始读码状态。</summary>
    private static void ActivateFrameTriggerFallback(
        HikvisionSmartCamera camera,
        uint trigger,
        uint frame,
        bool hasBarcode) {
        var info = new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 {
            nTriggerIndex = trigger,
            nFrameNum = frame
        };
        Invoke(camera, "EnsureTriggerCorrelationMode", info);
        GetField<HikvisionFrameTriggerResults>(camera, "_frameTriggerResults")
            .ObserveFrame(trigger, hasBarcode);
    }

    /// <summary>将模拟的 SDK 空帧传入适配器实际读码处理方法。</summary>
    private static void DeliverEmptyFrame(HikvisionSmartCamera camera, uint trigger, uint frame) {
        var method = typeof(HikvisionSmartCamera).GetMethod("HandleBarcodeReading", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var info = new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 { nTriggerIndex = trigger, nFrameNum = frame };
        var result = new MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2();
        method.Invoke(camera, [null, info, result, DateTime.Now, (long)frame, (long)frame, null]);
    }

    /// <summary>绕过原生内存构造一张已经完成过滤的条码帧。</summary>
    private static void DeliverBarcodeFrame(
        HikvisionSmartCamera camera,
        uint trigger,
        uint frame,
        params string[] barcodes) {
        var method = typeof(HikvisionSmartCamera).GetMethod(
            "HandleBarcodeReading",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var info = new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 {
            nTriggerIndex = trigger,
            nFrameNum = frame
        };
        var result = new MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2 { nCodeNum = (uint)barcodes.Length };
        var scanTime = DateTime.Now;
        var filtered = new List<BarcodeTriggeredEventArgs>(barcodes.Length);
        foreach (var barcode in barcodes) {
            var candidate = BarcodeCandidate(trigger, frame, barcode);
            candidate.ScanTime = scanTime;
            filtered.Add(candidate);
        }
        method.Invoke(camera, [null, info, result, scanTime, (long)frame, (long)frame, filtered]);
    }
}
