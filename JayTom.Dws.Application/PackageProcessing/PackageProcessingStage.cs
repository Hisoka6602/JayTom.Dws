namespace JayTom.Dws.Application.PackageProcessing;

/// <summary>包裹处理流水线的固定业务阶段。</summary>
public enum PackageProcessingStage
{
    /// <summary>确认采集记录有效。</summary>
    Acquisition = 1,

    /// <summary>确认包裹身份已匹配。</summary>
    Matching = 2,

    /// <summary>确认完成输出所需数据齐全。</summary>
    Completion = 3
}
