using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Camera.Concurrency;
using MvCodeReaderSDKNet;

namespace JayTom.Dws.Tests;

/// <summary>验证海康同一次硬件触发的多帧结果只产生一次最终 NoRead。</summary>
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

    /// <summary>实际原生回调入口和顺序调度队列在排空后只发布一次事件。</summary>
    [Fact]
    public async Task Native_boundary_callback_and_frame_queue_publish_one_result_after_drain() {
        var camera = new HikvisionSmartCamera { IsOriginalImageOut = false };
        SetField(camera, "_triggerCallbacksRegistered", true);
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

    /// <summary>将模拟的 SDK 空帧传入适配器实际读码处理方法。</summary>
    private static void DeliverEmptyFrame(HikvisionSmartCamera camera, uint trigger, uint frame) {
        var method = typeof(HikvisionSmartCamera).GetMethod("HandleBarcodeReading", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var info = new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 { nTriggerIndex = trigger, nFrameNum = frame };
        var result = new MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2();
        method.Invoke(camera, [null, info, result, DateTime.Now, (long)frame, (long)frame, null]);
    }
}
