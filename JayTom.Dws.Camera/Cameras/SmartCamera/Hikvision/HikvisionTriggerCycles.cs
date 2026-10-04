namespace JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;

/// <summary>在相机顺序处理线程中将同一次 SDK 触发的全部帧收束为一个最终业务结果。</summary>
internal sealed class HikvisionTriggerCycles : IDisposable {
    private const int RetainedCompletedCycles = 256;
    private const int MaximumPendingCycles = 64;
    private readonly Dictionary<uint, Cycle> _cycles = new();
    private readonly Queue<uint> _completed = new();

    /// <summary>记录真实 SDK 触发边界；结束消息可以先于拉帧线程读出最后一帧。</summary>
    public void OnTrigger(uint triggerIndex, bool started, long sequence) {
        var cycle = GetCycle(triggerIndex);
        if (cycle.Completed) {
            return;
        }
        if (!started) {
            cycle.EndSequence ??= sequence;
        }
    }

    /// <summary>在条码过滤前记录 SDK 读码成功，取消同周期已暂存的 NoRead。</summary>
    public void ObserveFrame(uint triggerIndex, bool hasBarcode) {
        var cycle = GetCycle(triggerIndex);
        cycle.HasFrame = true;
        if (hasBarcode) {
            cycle.HasBarcode = true;
            Release(cycle.PendingNoRead);
            cycle.PendingNoRead = null;
        }
    }

    /// <summary>暂存同一触发的全部不同条码；相同条码跨帧重复出现时仅保留一次。</summary>
    public void HoldBarcode(uint triggerIndex, BarcodeTriggeredEventArgs candidate) {
        Cycle cycle;
        try {
            cycle = GetCycle(triggerIndex);
        }
        catch {
            Release(candidate);
            throw;
        }

        cycle.HasFrame = true;
        cycle.HasBarcode = true;
        Release(cycle.PendingNoRead);
        cycle.PendingNoRead = null;
        if (cycle.Completed) {
            Release(candidate);
            return;
        }
        if (candidate.IsFiltered && cycle.HasAcceptedBarcode) {
            Release(candidate);
            return;
        }
        if (!candidate.IsFiltered && !cycle.HasAcceptedBarcode) {
            // 原始有码但被过滤的帧只占位；同周期后续有效码必须取代占位。
            foreach (var filtered in cycle.PendingBarcodes) {
                Release(filtered);
                cycle.Barcodes.Remove(filtered.Barcode);
            }
            cycle.PendingBarcodes.Clear();
            cycle.ScanTime = null;
            cycle.HasAcceptedBarcode = true;
        }
        if (!cycle.Barcodes.Add(candidate.Barcode)) {
            Release(candidate);
            return;
        }
        cycle.ScanTime ??= candidate.ScanTime;
        candidate.ScanTime = cycle.ScanTime.Value;
        cycle.PendingBarcodes.Add(candidate);
    }

    /// <summary>仅保留同周期第一张空帧的图片；被替换或取消的图片在这里释放。</summary>
    public void HoldNoRead(uint triggerIndex, BarcodeReadEventArgs candidate) {
        Cycle cycle;
        try {
            cycle = GetCycle(triggerIndex);
        }
        catch {
            Release(candidate);
            throw;
        }
        cycle.HasFrame = true;
        if (cycle.Completed || cycle.HasBarcode || cycle.PendingNoRead is not null) {
            Release(candidate);
            return;
        }
        cycle.PendingNoRead = candidate;
    }

    /// <summary>仅完成在本次 SDK 空队列探测开始前已经结束的触发，避免跨线程结束通知抢跑。</summary>
    public IReadOnlyList<BarcodeReadEventArgs> CompleteDrained(long throughSequence) {
        var result = new List<BarcodeReadEventArgs>();
        foreach (var pair in _cycles) {
            var cycle = pair.Value;
            if (cycle.Completed || !cycle.HasFrame || cycle.EndSequence is not { } end || end > throughSequence) {
                continue;
            }
            cycle.Completed = true;
            if (cycle.PendingBarcodes.Count > 0) {
                result.AddRange(cycle.PendingBarcodes);
            }
            else if (!cycle.HasBarcode && cycle.PendingNoRead is { } noRead) {
                result.Add(noRead);
            }
            else {
                Release(cycle.PendingNoRead);
            }
            cycle.PendingBarcodes.Clear();
            cycle.PendingNoRead = null;
            _completed.Enqueue(pair.Key);
        }
        while (_completed.Count > RetainedCompletedCycles) {
            _cycles.Remove(_completed.Dequeue());
        }
        return result;
    }

    /// <summary>停止或重新启动时释放未完成周期，不将中断的采集伪造成 NoRead。</summary>
    public void Dispose() {
        foreach (var cycle in _cycles.Values) {
            Release(cycle.PendingNoRead);
            foreach (var barcode in cycle.PendingBarcodes) {
                Release(barcode);
            }
        }
        _cycles.Clear();
        _completed.Clear();
    }

    private Cycle GetCycle(uint triggerIndex) {
        if (!_cycles.TryGetValue(triggerIndex, out var cycle)) {
            if (_cycles.Count(pair => !pair.Value.Completed) >= MaximumPendingCycles) {
                Dispose();
                throw new InvalidOperationException("海康相机存在过多未完成触发周期，请检查触发结束回调及相机固件。");
            }
            cycle = new Cycle();
            _cycles.Add(triggerIndex, cycle);
        }
        return cycle;
    }

    private static void Release(BarcodeReadEventArgs? args) {
        args?.Image?.Dispose();
        if (args is not null && !ReferenceEquals(args.Image, args.ThumbImage)) {
            args.ThumbImage?.Dispose();
        }
    }

    private sealed class Cycle {
        public bool HasFrame { get; set; }
        public bool HasBarcode { get; set; }
        /// <summary>表示该周期已有通过过滤的有效码，优先于过滤占位。</summary>
        public bool HasAcceptedBarcode { get; set; }
        public bool Completed { get; set; }
        public long? EndSequence { get; set; }
        public DateTime? ScanTime { get; set; }
        public BarcodeReadEventArgs? PendingNoRead { get; set; }
        public HashSet<string> Barcodes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<BarcodeTriggeredEventArgs> PendingBarcodes { get; } = [];
    }
}
