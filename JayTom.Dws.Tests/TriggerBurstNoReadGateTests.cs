using System.Drawing;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Concurrency;

namespace JayTom.Dws.Tests;

/// <summary>验证没有原生触发结束回调时，连续帧的条码优先和单次 NoRead 语义。</summary>
public sealed class TriggerBurstNoReadGateTests {
    /// <summary>最后一帧后达到静默窗口时自动发布整轮唯一的 NoRead。</summary>
    [Fact]
    public async Task Quiet_period_automatically_completes_empty_burst() {
        var completion = new TaskCompletionSource<BarcodeReadEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var gate = new TriggerBurstNoReadGate(
            TimeSpan.FromMilliseconds(20),
            (_, args) => completion.TrySetResult(args));
        gate.Observe(1, false, Candidate().Args);

        var result = await completion.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal("NoRead", result.Barcode);
        Release(result);
    }

    /// <summary>同一帧组任意一帧有码时，释放已暂存候选且不发布 NoRead。</summary>
    [Fact]
    public void Barcode_cancels_all_noread_candidates_in_same_burst() {
        var published = new List<BarcodeReadEventArgs>();
        using var gate = new TriggerBurstNoReadGate(
            TimeSpan.FromMinutes(1),
            (_, args) => published.Add(args));
        var first = Candidate();
        var duplicate = Candidate();

        gate.Observe(3, false, first.Args);
        gate.Observe(3, false, duplicate.Args);
        AssertDisposed(duplicate.Image);
        gate.Observe(3, true, null);
        gate.Complete();

        Assert.Empty(published);
        AssertDisposed(first.Image);
    }

    /// <summary>同一帧组连续多张无码图只发布第一张候选一次。</summary>
    [Fact]
    public void Multiple_empty_frames_publish_one_noread() {
        var published = new List<BarcodeReadEventArgs>();
        using var gate = new TriggerBurstNoReadGate(
            TimeSpan.FromMinutes(1),
            (_, args) => published.Add(args));
        var first = Candidate();
        var duplicate = Candidate();

        gate.Observe(5, false, first.Args);
        gate.Observe(5, false, duplicate.Args);
        gate.Complete();

        BarcodeReadEventArgs? result = null;
        Assert.Collection(published, item => result = item);
        Assert.Same(first.Args, result);
        AssertDisposed(duplicate.Image);
        Release(result!);
    }

    /// <summary>停止重启后的新会话不会发布上一采集会话暂存的 NoRead。</summary>
    [Fact]
    public void New_generation_discards_interrupted_burst() {
        var published = new List<BarcodeReadEventArgs>();
        using var gate = new TriggerBurstNoReadGate(
            TimeSpan.FromMinutes(1),
            (_, args) => published.Add(args));
        var interrupted = Candidate();
        var current = Candidate();

        gate.Observe(8, false, interrupted.Args);
        gate.Observe(9, false, current.Args);
        gate.Complete();

        AssertDisposed(interrupted.Image);
        BarcodeReadEventArgs? result = null;
        Assert.Collection(published, item => result = item);
        Assert.Same(current.Args, result);
        Release(result!);
    }

    /// <summary>创建带独立原图和缩略图的 NoRead 候选。</summary>
    private static (BarcodeReadEventArgs Args, Bitmap Image) Candidate() {
        var image = new Bitmap(2, 2);
        return (new BarcodeReadEventArgs {
            Barcode = "NoRead",
            Image = image,
            ThumbImage = new Bitmap(1, 1)
        }, image);
    }

    /// <summary>确认门控器已经释放不再使用的位图。</summary>
    private static void AssertDisposed(Bitmap image) =>
        Assert.Throws<ArgumentException>(() => image.GetPixel(0, 0));

    /// <summary>模拟业务订阅者释放已发布事件的图像。</summary>
    private static void Release(BarcodeReadEventArgs args) {
        args.Image?.Dispose();
        args.ThumbImage?.Dispose();
    }
}
