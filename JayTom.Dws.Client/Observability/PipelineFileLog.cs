using System;
using System.Threading;
using System.Text.RegularExpressions;
using JayTom.Dws.Abstractions.Observability;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Models.LocalLog;
using NLog;
using NLog.Config;
using NLog.Common;
using NLog.Targets.Wrappers;

namespace JayTom.Dws.Client.Observability;

/// <summary>把读码和分拣证据快照交给独立的异步文件目标，不依赖数据库成功写入。</summary>
internal sealed class PipelineFileLog : IDisposable
{
    /// <summary>链路文件使用的固定日志名称。</summary>
    internal const string LoggerName = "Dws.Pipeline";
    /// <summary>单个外部文本的上限，避免异常响应占满诊断内存。</summary>
    internal const int MaximumTextLength = 16_384;
    /// <summary>生产环境共用的轻量记录入口。</summary>
    internal static PipelineFileLog Instance { get; } = new();
    /// <summary>隐藏 JSON 字符串中可能出现的接口凭据，保留业务响应字段。</summary>
    private static readonly Regex JsonCredentialPattern = new(
        "(?i)(\"[^\"]*(?:password|passwd|secret|token|authorization|api[_-]?key|app[_-]?key|access[_-]?key|signature|credential)[^\"]*\"\\s*:\\s*)\"(?:\\\\.|[^\"\\\\])*\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
    /// <summary>只向异步目标提交不可变字段，不在设备线程执行文件读写。</summary>
    private readonly Logger _logger;
    /// <summary>区分进程重启前后的证据，不能用重复的小车编号单独关联包裹。</summary>
    private readonly string _processRun = Guid.NewGuid().ToString("N");
    /// <summary>持有本记录器所属的工厂，用于配置重载及取消订阅。</summary>
    private readonly LogFactory _logFactory;
    /// <summary>记录日志提交次序；异步文件中的行次序不代表业务发生次序。</summary>
    private long _eventSequence;
    /// <summary>累计记录格式化或提交失败，诊断故障不得传播到分拣流程。</summary>
    private long _failedWrites;
    /// <summary>统计后台文件目标的故障报告，并保存最近一次错误文本。</summary>
    private long _targetErrorReports;
    /// <summary>不在 NLog 内部故障回调中再次记录日志，避免递归写入。</summary>
    private string? _lastTargetError;
    /// <summary>普通链路队列和独立异常队列的累计丢弃数量。</summary>
    private long _pipelineDrops, _anomalyDrops;
    /// <summary>最近绑定的异步目标，配置重载后只订阅一次。</summary>
    private AsyncTargetWrapper? _pipelineTarget, _anomalyTarget;

    /// <summary>创建链路记录器；测试可以使用独立日志工厂，避免修改全局配置。</summary>
    internal PipelineFileLog(Logger? logger = null, LogFactory? factory = null)
    {
        _logger = logger ?? LogManager.GetLogger(LoggerName);
        _logFactory = factory ?? LogManager.LogFactory;
        ObserveTargets(_logFactory.Configuration);
        _logFactory.ConfigurationChanged += OnConfigurationChanged;
        InternalLogger.InternalEventOccurred += OnTargetError;
        if (InternalLogger.LogLevel > LogLevel.Warn) InternalLogger.LogLevel = LogLevel.Warn;
    }

    /// <summary>提供精确累计值，避免为同一异步目标重复订阅 NLog 的丢弃事件。</summary>
    internal long PipelineDroppedTotal => Interlocked.Read(ref _pipelineDrops);

    /// <summary>配置改变后重新绑定目标；生产单例只订阅一次。</summary>
    private void OnConfigurationChanged(object? sender, LoggingConfigurationChangedEventArgs args) =>
        ObserveTargets(_logFactory.Configuration);

    /// <summary>后台写盘失败只计数，不执行磁盘操作或递归日志调用。</summary>
    private void OnTargetError(object? sender, InternalLogEventArgs args)
    {
        try
        {
        if (args.Level < LogLevel.Warn ||
            !(args.SenderName?.Contains("PipelineLogFile", StringComparison.Ordinal) == true ||
              args.SenderName?.Contains("AnomalyLogFile", StringComparison.Ordinal) == true ||
              args.Message.Contains("PipelineLogFile", StringComparison.Ordinal) ||
              args.Message.Contains("AnomalyLogFile", StringComparison.Ordinal))) return;
        Interlocked.Increment(ref _targetErrorReports);
        var detail = args.Exception is null ? args.Message : args.Message + ":" + args.Exception;
        Volatile.Write(ref _lastTargetError, detail.Length <= MaximumTextLength ? detail : "文件目标错误正文过长，已省略");
        }
        catch (Exception) { Interlocked.Increment(ref _failedWrites); }
    }

    /// <summary>释放测试或独立记录器的工厂与内部诊断订阅。</summary>
    public void Dispose()
    {
        _logFactory.ConfigurationChanged -= OnConfigurationChanged;
        InternalLogger.InternalEventOccurred -= OnTargetError;
        if (_pipelineTarget is not null) _pipelineTarget.LogEventDropped -= OnPipelineDropped;
        if (_anomalyTarget is not null) _anomalyTarget.LogEventDropped -= OnAnomalyDropped;
    }

    /// <summary>在事件入队时快照全部已有日志分类的关键字段。</summary>
    internal void Capture(BaseLogInfoModel model)
    {
        try { CaptureCore(model); }
        catch (Exception) { Interlocked.Increment(ref _failedWrites); }
    }

    /// <summary>在受保护边界内生成旧日志模型的不可变快照。</summary>
    private void CaptureCore(BaseLogInfoModel model)
    {
        var entry = Create(model.GetType().Name, model.Message, model.Type, model.CreateTime);
        switch (model)
        {
            case CameraLogInfoModel camera:
                Add(entry, "camera", camera.CameraSerialNumber);
                break;
            case SortingLogInfoModel sorting:
                Add(entry, "connection", sorting.Destination);
                Add(entry, "direction", sorting.CommunicationType);
                Add(entry, "dataFormat", sorting.DataFormatType);
                break;
            case WeighingLogInfoModel weighing:
                Add(entry, "rawContent", weighing.Source);
                Add(entry, "weight", weighing.FormatWeight);
                Add(entry, "direction", weighing.CommunicationType);
                Add(entry, "dataSource", weighing.DataSourceType);
                break;
            case VolumeLogInfoModel volume:
                Add(entry, "dataSource", volume.DataSourceType);
                break;
            case ApiLogInfoModel api:
                Add(entry, "url", api.Url);
                Add(entry, "request", api.RequestContent);
                Add(entry, "response", api.ResponseContent);
                Add(entry, "requestTime", api.RequestTime);
                Add(entry, "responseTime", api.ResponseTime);
                Add(entry, "durationSeconds", api.Duration);
                Add(entry, "error", api.ExceptionMsg);
                break;
            case InputLogInfoModel input:
                Add(entry, "connection", input.Destination);
                Add(entry, "rawContent", input.InputContent);
                Add(entry, "dataSource", input.DataSourceType);
                break;
            case OutputLogInfoModel output:
                Add(entry, "connection", output.Destination);
                Add(entry, "rawContent", output.OutputContent);
                Add(entry, "outputType", output.OutputType);
                break;
            case OcrLogInfoModel ocr:
                Add(entry, "barcode", ocr.BarCode);
                Add(entry, "camera", ocr.CameraSerialNumber);
                Add(entry, "durationMilliseconds", ocr.ElapsedTime);
                Add(entry, "observedAt", ocr.RecognitionTime);
                break;
            case ExceptionLogInfoModel exception:
                Add(entry, "exceptionSource", exception.ExceptionSource);
                break;
        }
        _logger.Log(entry);
    }

    /// <summary>记录一个链路阶段，使用包裹时间戳、流水号和条码连接各阶段证据。</summary>
    internal void Write(
        string stage,
        string message,
        LogType type = LogType.Information,
        long packageTimestamp = 0,
        string? barcode = null,
        string? sequence = null,
        DateTime? occurredAt = null,
        params (string Key, object? Value)[] fields)
    {
        try
        {
        var entry = Create(stage, message, type, occurredAt ?? DateTime.Now);
        Add(entry, "packageTimestamp", packageTimestamp);
        Add(entry, "barcode", barcode);
        Add(entry, "sequence", sequence);
        if (long.TryParse(sequence, out var cartId)) Add(entry, "cartId", cartId);
        foreach (var field in fields)
        {
            Add(entry, field.Key, field.Value);
        }
        _logger.Log(entry);
        }
        catch (Exception) { Interlocked.Increment(ref _failedWrites); }
    }

    /// <summary>快照原包裹身份与齐套状态，不把包裹、图片或可变配置送入文件队列。</summary>
    internal void Package(string stage, string message, PackageInfo package,
        LogType type = LogType.Information, params (string Key, object? Value)[] fields)
    {
        try
        {
            (string Key, object? Value)[] snapshot;
            lock (package.SyncRoot)
            {
                snapshot = [("packageTimestamp", package.Timestamp), ("cartId", package.Guid),
                    ("sequence", package.Guid.ToString()), ("barcode", package.BarCodeInfo?.Barcode),
                    ("camera", package.BarCodeInfo?.SerialNumber), ("createdAt", package.CreateTime),
                    ("rawCreationInstruction", package.PackageCreationInstruction),
                    ("creationTimeTicks", package.CreateTime.Ticks), ("scanTime", package.BarCodeInfo?.ScanTime),
                    ("isLowerMachine", package.IsCreatedByLowerMachine), ("hasBarcode", package.BarCodeInfo is not null),
                    ("hasWeight", package.WeightInfo is not null), ("hasVolume", package.VolumeInfo is not null),
                    ("isCompleted", package.IsCompleted), ("weight", package.WeightInfo?.FormattedWeight)];
            }
            var entry = Create(stage, message, type, DateTime.Now);
            foreach (var field in snapshot) Add(entry, field.Key, field.Value);
            foreach (var field in fields) Add(entry, field.Key, field.Value);
            _logger.Log(entry);
        }
        catch (Exception) { Interlocked.Increment(ref _failedWrites); }
    }

    /// <summary>由已有后台周期报告日志积压，累计数可在磁盘恢复后继续追查证据缺口。</summary>
    internal void ReportHealth()
    {
        var dropped = Interlocked.Read(ref _pipelineDrops);
        var anomalies = Interlocked.Read(ref _anomalyDrops);
        var failed = Interlocked.Read(ref _failedWrites);
        var errors = Interlocked.Read(ref _targetErrorReports);
        if (dropped + anomalies + failed + errors == 0) return;
        Write("diagnostics.loss", "诊断日志出现丢弃或提交失败，累计计数不影响业务队列", LogType.Warning,
            fields: [("reasonCode", "DiagnosticEvidenceLoss"), ("pipelineDroppedTotal", dropped),
                ("anomalyDroppedTotal", anomalies), ("failedWritesTotal", failed),
                ("targetErrorReportsTotal", errors), ("lastTargetError", Volatile.Read(ref _lastTargetError))]);
    }

    /// <summary>配置首次加载和重载时订阅有界目标，只统计丢弃而不在回调内再写日志。</summary>
    private void ObserveTargets(LoggingConfiguration? configuration)
    {
        try
        {
            var pipeline = configuration?.FindTargetByName<AsyncTargetWrapper>("AsyncPipelineLogFile");
            var anomaly = configuration?.FindTargetByName<AsyncTargetWrapper>("AsyncAnomalyLogFile");
            if (!ReferenceEquals(pipeline, _pipelineTarget))
            {
                if (_pipelineTarget is not null) _pipelineTarget.LogEventDropped -= OnPipelineDropped;
                _pipelineTarget = pipeline;
                if (pipeline is not null) pipeline.LogEventDropped += OnPipelineDropped;
            }
            if (!ReferenceEquals(anomaly, _anomalyTarget))
            {
                if (_anomalyTarget is not null) _anomalyTarget.LogEventDropped -= OnAnomalyDropped;
                _anomalyTarget = anomaly;
                if (anomaly is not null) anomaly.LogEventDropped += OnAnomalyDropped;
            }
        }
        catch (Exception) { Interlocked.Increment(ref _failedWrites); }
    }

    /// <summary>普通证据满队列时仅作常数时间计数。</summary>
    private void OnPipelineDropped(object? sender, LogEventDroppedEventArgs args) => Interlocked.Increment(ref _pipelineDrops);
    /// <summary>异常证据满队列时仅作常数时间计数。</summary>
    private void OnAnomalyDropped(object? sender, LogEventDroppedEventArgs args) => Interlocked.Increment(ref _anomalyDrops);

    /// <summary>记录具有堆栈的设备或后台异常，文件写入仍由异步目标完成。</summary>
    internal void Error(string stage, Exception exception)
    {
        try
        {
        Write(stage, exception.Message, LogType.Exception,
            fields: [("error", exception.ToString())]);
        }
        catch (Exception) { Interlocked.Increment(ref _failedWrites); }
    }

    /// <summary>建立一次事件的结构化快照。</summary>
    private LogEventInfo Create(string stage, string message, LogType type, DateTime occurredAt)
    {
        var level = type switch
        {
            LogType.Warning => LogLevel.Warn,
            LogType.Exception => LogLevel.Error,
            _ => LogLevel.Info
        };
        var entry = new LogEventInfo(level, LoggerName, Sanitize(message));
        entry.Properties["stage"] = stage;
        entry.Properties["eventTime"] = occurredAt.ToString("O");
        entry.Properties["runId"] = _processRun;
        entry.Properties["eventSequence"] = Interlocked.Increment(ref _eventSequence);
        entry.Properties["threadId"] = Environment.CurrentManagedThreadId;
        return entry;
    }

    /// <summary>保留数值精度和发生时间，防止引用可变包裹、图像或配置对象。</summary>
    private static void Add(LogEventInfo entry, string key, object? value)
    {
        if (value is null) return;
        if (value is string original && original.Length > MaximumTextLength)
        {
            entry.Properties[key + "OriginalLength"] = original.Length;
            entry.Properties[key + "Omitted"] = true;
        }
        entry.Properties[key] = value switch
        {
            string text => Sanitize(text),
            DateTime time => time.ToString("O"),
            DateTimeOffset time => time.ToString("O"),
            Enum enumeration => enumeration.ToString(),
            _ => value
        };
    }

    /// <summary>隐藏凭据文本；数据库配置对象不进入链路文件。</summary>
    internal static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Length > MaximumTextLength) return $"[文本过长，已省略；原长度={value.Length}]";
        // 两种凭据模式都需要冒号或等号；普通条码、相机名称和固定说明直接返回，减少热路径正则开销。
        if (value.AsSpan().IndexOfAny(':', '=') < 0) return value;
        try
        {
        var safeJson = JsonCredentialPattern.Replace(value,
            match => $"{match.Groups[1].Value}\"{SensitiveDataRedactor.RedactedValue}\"");
        return SensitiveDataRedactor.RedactMessage(safeJson);
        }
        catch (RegexMatchTimeoutException) { return "[文本脱敏超时，已省略]"; }
    }
}
