namespace JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;

/// <summary>
/// 固件不送达触发开始/结束回调时，使用电平触发帧自带的触发序号对同一触发的结果分组去重。
/// </summary>
internal sealed class HikvisionFrameTriggerResults : IDisposable {
    private const int RetainedTriggers = 256;
    private readonly Dictionary<uint, TriggerResult> _triggers = new();
    private readonly Queue<uint> _order = new();

    /// <summary>单次触发结果认领状态。</summary>
    public enum Claim {
        Accepted,
        Duplicate,
        Suppressed
    }

    /// <summary>在过滤前记录 SDK 原始读码状态，避免被过滤的有码帧随后被空帧伪造成 NoRead。</summary>
    public void ObserveFrame(uint triggerIndex, bool hasBarcode) {
        var state = GetOrAdd(triggerIndex);
        state.HasRawBarcode |= hasBarcode;
    }

    /// <summary>尝试认领该触发唯一的业务结果；未接受时调用方必须释放候选图像。</summary>
    public Claim TryClaim(
        uint triggerIndex,
        string result,
        bool isNoRead,
        DateTime scanTime,
        out DateTime groupedScanTime) {
        var state = GetOrAdd(triggerIndex);
        state.ScanTime ??= scanTime;
        groupedScanTime = state.ScanTime.Value;
        if (isNoRead && state.HasRawBarcode) {
            return Claim.Suppressed;
        }
        if (state.Results.Add(result)) {
            return Claim.Accepted;
        }
        return Claim.Duplicate;
    }

    public void Dispose() {
        _triggers.Clear();
        _order.Clear();
    }

    private TriggerResult GetOrAdd(uint triggerIndex) {
        if (_triggers.TryGetValue(triggerIndex, out var state)) {
            return state;
        }
        state = new TriggerResult();
        _triggers.Add(triggerIndex, state);
        _order.Enqueue(triggerIndex);
        while (_order.Count > RetainedTriggers) {
            _triggers.Remove(_order.Dequeue());
        }
        return state;
    }

    private sealed class TriggerResult {
        public bool HasRawBarcode { get; set; }
        public DateTime? ScanTime { get; set; }
        public HashSet<string> Results { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
