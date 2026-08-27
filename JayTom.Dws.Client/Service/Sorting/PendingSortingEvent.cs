using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Legacy.Contracts.Model;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;

namespace JayTom.Dws.Client.Service.Sorting;

/// <summary>保存分拣启动窗口内按到达顺序等待处理的事件。</summary>
internal sealed record PendingSortingEvent(
    PackageInfo? CompletedPackage,
    ApiResponseReceived? ApiResponse,
    PackageOcrInfo? OcrInfo = null);
