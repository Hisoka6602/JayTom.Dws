/// <summary>
/// 表示一条十六进制格式守卫自检样例。
/// </summary>
internal sealed class HexFormattingGuardSample {
    /// <summary>
    /// 使用样例源码和期望违规数量初始化自检样例。
    /// </summary>
    /// <param name="source">用于语法分析的源码。</param>
    /// <param name="expectedViolationCount">期望检出的违规数量。</param>
    public HexFormattingGuardSample(
        string source,
        int expectedViolationCount) {
        Source = source;
        ExpectedViolationCount = expectedViolationCount;
    }

    /// <summary>
    /// 获取用于语法分析的源码。
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// 获取期望检出的违规数量。
    /// </summary>
    public int ExpectedViolationCount { get; }

    /// <summary>
    /// 将样例拆分为源码和期望违规数量。
    /// </summary>
    /// <param name="source">返回样例源码。</param>
    /// <param name="expectedViolationCount">返回期望违规数量。</param>
    public void Deconstruct(
        out string source,
        out int expectedViolationCount) {
        source = Source;
        expectedViolationCount = ExpectedViolationCount;
    }
}
