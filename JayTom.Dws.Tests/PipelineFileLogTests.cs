using System.Text.Json;
using System.Xml;
using JayTom.Dws.Client.Observability;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Legacy.Contracts.Packages;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace JayTom.Dws.Tests;

/// <summary>使用生产日志配置验证文件证据、数据库故障隔离和突发积压时的非阻塞行为。</summary>
public sealed class PipelineFileLogTests
{
    /// <summary>使用独立日志工厂和临时目录，实际写出生产 JSON 行格式。</summary>
    private sealed class FileFixture : IDisposable
    {
        /// <summary>当前测试的临时日志目录。</summary>
        internal DirectoryInfo Directory { get; } = System.IO.Directory.CreateTempSubdirectory("jaytom-pipeline-");
        /// <summary>隔离测试的日志工厂。</summary>
        internal LogFactory Factory { get; } = new() { ThrowConfigExceptions = true };
        /// <summary>被验证的生产记录器。</summary>
        internal PipelineFileLog Recorder { get; }
        /// <summary>可选的慢磁盘模拟目标。</summary>
        internal GatedTarget? Gate { get; }
        /// <summary>慢磁盘期间由于达到固定队列上限而舍弃的诊断日志数量。</summary>
        internal long Dropped => Recorder.PipelineDroppedTotal;

        /// <summary>读取生产配置，仅把日志根目录重定向到临时位置。</summary>
        internal FileFixture(bool pause = false)
        {
            var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Nlog.config"))
                .Replace("${basedir}", Directory.FullName.Replace('\\', '/'));
            using var reader = XmlReader.Create(new StringReader(xml));
            var configuration = new XmlLoggingConfiguration(reader, null, Factory);
            var target = configuration.FindTargetByName<AsyncTargetWrapper>("AsyncPipelineLogFile");
            Assert.NotNull(target);
            Assert.Equal(AsyncTargetWrapperOverflowAction.Discard, target.OverflowAction);
            if (pause)
            {
                Gate = new GatedTarget { Name = "SlowDisk", WrappedTarget = target.WrappedTarget };
                target.WrappedTarget = Gate;
            }
            Factory.Configuration = configuration;
            Recorder = new PipelineFileLog(Factory.GetLogger(PipelineFileLog.LoggerName), Factory);
        }

        /// <summary>等待后台写出并读取全部实际文件行。</summary>
        internal string[] ReadLines()
        {
            Factory.Flush(TimeSpan.FromSeconds(15));
            return System.IO.Directory.GetFiles(Path.Combine(Directory.FullName, "logs", "Pipeline"), "*.jsonl")
                .SelectMany(ReadSharedLines).ToArray();
        }

        /// <summary>读取期望恰好一条的实际文件证据。</summary>
        internal string ReadOnlyLine()
        {
            var lines = ReadLines();
            Assert.Collection(lines, line => Assert.False(string.IsNullOrWhiteSpace(line)));
            return lines[0];
        }

        /// <summary>读取独立异常文件，其容量不被普通链路目标占用。</summary>
        internal string[] ReadAnomalies()
        {
            Factory.Flush(TimeSpan.FromSeconds(15));
            var folder = Path.Combine(Directory.FullName, "logs", "Anomalies");
            return System.IO.Directory.Exists(folder)
                ? System.IO.Directory.GetFiles(folder, "*.jsonl").SelectMany(ReadSharedLines).ToArray() : [];
        }

        /// <summary>允许后台持有写句柄，同时只读取刷新后完整的日志行。</summary>
        private static string[] ReadSharedLines(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>关闭异步目标后清理测试目录。</summary>
        public void Dispose()
        {
            Gate?.Release();
            Recorder.Dispose();
            Factory.Dispose();
            Gate?.Dispose();
            Directory.Delete(true);
        }
    }

    /// <summary>阻止后台目标写出，验证设备侧生产者不需要等待磁盘。</summary>
    private sealed class GatedTarget : WrapperTargetBase
    {
        /// <summary>模拟磁盘恢复的闸门。</summary>
        private readonly ManualResetEvent _ready = new(false);
        /// <summary>表示后台已经开始一次写出。</summary>
        internal ManualResetEvent Started { get; } = new(false);
        /// <summary>恢复后台写出。</summary>
        internal void Release() => _ready.Set();
        /// <summary>等待恢复后把原始日志交给生产文件目标。</summary>
        protected override void Write(AsyncLogEventInfo logEvent)
        {
            Started.Set();
            _ready.WaitOne(TimeSpan.FromSeconds(20));
            WrappedTarget.WriteAsyncLogEvent(logEvent);
        }

        /// <summary>测试结束时释放模拟磁盘闸门。</summary>
        protected override void CloseTarget()
        {
            _ready.Set();
            base.CloseTarget();
            _ready.Dispose();
            Started.Dispose();
        }
    }

    /// <summary>首次事件立即保留文件证据，数据库无限重试也不会重复同一快照。</summary>
    [Fact]
    public void EvidenceSurvivesDatabaseRetry()
    {
        using var fixture = new FileFixture();
        var queue = new BoundedLogQueue<OutputLogInfoModel>(1);
        queue.SetCaptureCallback(fixture.Recorder.Capture);
        var model = new OutputLogInfoModel
        {
            Message = "格口输出", Destination = "PLC", OutputContent = "FC 12 00 2C", Type = LogType.Information
        };
        queue.Enqueue(model);
        Assert.True(queue.TryDequeue(out var failedBatchItem));
        for (var attempt = 0; attempt < 5; attempt++) queue.EnqueueForRetry(failedBatchItem);
        model.OutputContent = "后续修改不得改变原始证据";

        using var evidence = JsonDocument.Parse(fixture.ReadOnlyLine());
        Assert.Equal("FC 12 00 2C", evidence.RootElement.GetProperty("rawContent").GetString());
        Assert.Equal("PLC", evidence.RootElement.GetProperty("connection").GetString());
        Assert.False(queue.IsEmpty);
    }

    /// <summary>原先只保存到数据库的所有日志分类均写入独立链路文件。</summary>
    [Fact]
    public void EveryCategoryWritesKeyFields()
    {
        using var fixture = new FileFixture();
        BaseLogInfoModel[] models =
        [
            new AppLogInfoModel(), new CameraLogInfoModel { CameraSerialNumber = "CAM-01" },
            new SortingLogInfoModel { Destination = "PLC" }, new WeighingLogInfoModel { FormatWeight = 1.234m },
            new VolumeLogInfoModel(), new ApiLogInfoModel { RequestContent = "query", ResponseContent = "44" },
            new InputLogInfoModel { InputContent = "JT5531859332128" },
            new OutputLogInfoModel { OutputContent = "FC 12 00 2C" },
            new OcrLogInfoModel { BarCode = "JT5531859332128" }, new FtpLogInfoModel(),
            new LogCleaningLogInfoModel(), new ExceptionLogInfoModel { Message = "协议异常" }
        ];
        foreach (var model in models) fixture.Recorder.Capture(model);
        var rows = fixture.ReadLines().Select(line => JsonDocument.Parse(line)).ToArray();
        try
        {
            Assert.Equal(models.Length, rows.Length);
            Assert.Equal(models.Select(model => model.GetType().Name).Order(),
                rows.Select(row => row.RootElement.GetProperty("stage").GetString()).Order());
            // 上面的类别计数已经校验唯一性，避免 Single 标识符被质量守卫误识别为浮点类型。
            var api = rows.First(row => row.RootElement.GetProperty("stage").GetString() == nameof(ApiLogInfoModel));
            Assert.Equal("query", api.RootElement.GetProperty("request").GetString());
            Assert.Equal("44", api.RootElement.GetProperty("response").GetString());
        }
        finally { foreach (var row in rows) row.Dispose(); }
    }

    /// <summary>不同阶段保留相同包裹身份、原始时间和条码，包含中文与换行时仍可逐行解析。</summary>
    [Fact]
    public void StagesRemainCorrelated()
    {
        using var fixture = new FileFixture();
        var observedAt = new DateTime(2026, 10, 3, 9, 46, 47, 457, DateTimeKind.Local);
        foreach (var stage in new[] { "barcode.read", "package.BarCodeSetValueAfter", "sorting.selected",
                     "transport.Send", "sorting.PhysicalExit", "image.saved" })
            fixture.Recorder.Write(stage, "测试\n原始协议", packageTimestamp: 638950240000000000,
                barcode: "JT5531859332128", sequence: "38", occurredAt: observedAt,
                fields: [("exitName", "44"), ("weight", 1.234m)]);
        var lines = fixture.ReadLines();
        Assert.Equal(6, lines.Length);
        foreach (var line in lines)
        {
            using var row = JsonDocument.Parse(line);
            Assert.Equal(638950240000000000, row.RootElement.GetProperty("packageTimestamp").GetInt64());
            Assert.Equal("JT5531859332128", row.RootElement.GetProperty("barcode").GetString());
            Assert.Equal(observedAt.ToString("O"), row.RootElement.GetProperty("eventTime").GetString());
            Assert.Equal(1.234m, row.RootElement.GetProperty("weight").GetDecimal());
        }
    }

    /// <summary>后台磁盘暂停时生产者仍能返回，日志内存保持有界并精确统计舍弃的诊断记录。</summary>
    [Fact]
    public async Task BurstIsBoundedAndNonBlocking()
    {
        using var fixture = new FileFixture(pause: true);
        fixture.Recorder.Write("burst", "first", sequence: "0");
        Assert.True(fixture.Gate!.Started.WaitOne(TimeSpan.FromSeconds(5)));
        const int count = 25000;
        try
        {
            await Task.Run(() => Parallel.For(1, count + 1, new ParallelOptions { MaxDegreeOfParallelism = 4 },
                index => fixture.Recorder.Write("burst", "读码链路", sequence: index.ToString())))
                .WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally { fixture.Gate.Release(); }
        var lines = fixture.ReadLines();
        Assert.True(fixture.Dropped > 0);
        Assert.InRange(lines.Length, 1, 20513);
        Assert.Equal(count + 1, lines.Length + fixture.Dropped);
        Assert.Equal(lines.Length, lines.Select(line =>
        {
            using var row = JsonDocument.Parse(line);
            return row.RootElement.GetProperty("sequence").GetString();
        }).Distinct().Count());
    }

    /// <summary>保留排查所需业务请求和响应，凭据配置、JSON 密钥和 URL 令牌不会明文落盘。</summary>
    [Fact]
    public void CredentialsAreHidden()
    {
        using var fixture = new FileFixture();
        fixture.Recorder.Capture(new ApiLogInfoModel
        {
            ApiParameters = "whole-secret-config", Url = "http://localhost/query?token=url-secret",
            RequestContent = "{\"appSecret\":\"json-secret\",\"appKey\":\"json-key\",\"barcode\":\"JT1\"}",
            ResponseContent = "{\"code\":1,\"sortingPortCode\":\"44\"}"
        });
        var line = fixture.ReadOnlyLine();
        Assert.DoesNotContain("whole-secret-config", line);
        Assert.DoesNotContain("url-secret", line);
        Assert.DoesNotContain("json-secret", line);
        Assert.DoesNotContain("json-key", line);
        using var row = JsonDocument.Parse(line);
        using var request = JsonDocument.Parse(row.RootElement.GetProperty("request").GetString()!);
        Assert.Equal("JT1", request.RootElement.GetProperty("barcode").GetString());
        Assert.Contains("44", row.RootElement.GetProperty("response").GetString());
    }

    /// <summary>生产规则也把现有服务的警告和异常堆栈汇入链路文件。</summary>
    [Fact]
    public void ExistingErrorsIncludeStackEvidence()
    {
        using var fixture = new FileFixture();
        var logger = fixture.Factory.GetLogger("JayTom.Dws.Client.Service.Sorting.DefaultSortingService");
        try { throw new InvalidOperationException("身份复核失败"); }
        catch (Exception error) { logger.Error(error, "禁止发送格口指令"); }
        using var row = JsonDocument.Parse(fixture.ReadOnlyLine());
        Assert.Equal("Error", row.RootElement.GetProperty("level").GetString());
        Assert.Contains("ExistingErrorsIncludeStackEvidence", row.RootElement.GetProperty("exception").GetString());
    }

    /// <summary>普通磁盘队列已满时，独立故障证据仍落盘，并能报告普通日志的缺口。</summary>
    [Fact]
    public async Task Ordinary_flood_preserves_independent_anomaly_evidence_and_loss_totals()
    {
        using var fixture = new FileFixture(pause: true);
        fixture.Recorder.Write("burst", "first");
        Assert.True(fixture.Gate!.Started.WaitOne(TimeSpan.FromSeconds(5)));
        try
        {
            await Task.Run(() => Parallel.For(0, 25_000, index => fixture.Recorder.Write("burst", "normal")))
                .WaitAsync(TimeSpan.FromSeconds(10));
            fixture.Recorder.Write("binding.failed", "原车绑定被拒绝", LogType.Warning, 123456, "JT-123", "38",
                fields: [("reasonCode", "OriginalPackageUnavailable")]);
            fixture.Recorder.ReportHealth();
        }
        finally { fixture.Gate.Release(); }
        var anomalies = fixture.ReadAnomalies().Select(line => JsonDocument.Parse(line)).ToArray();
        try
        {
            var failure = anomalies.First(row => row.RootElement.GetProperty("stage").GetString() == "binding.failed").RootElement;
            Assert.Equal(123456, failure.GetProperty("packageTimestamp").GetInt64());
            Assert.Equal(38, failure.GetProperty("cartId").GetInt64());
            Assert.Equal("OriginalPackageUnavailable", failure.GetProperty("reasonCode").GetString());
            var health = anomalies.First(row => row.RootElement.GetProperty("stage").GetString() == "diagnostics.loss").RootElement;
            Assert.True(health.GetProperty("pipelineDroppedTotal").GetInt64() > 0);
            Assert.Equal(0, health.GetProperty("anomalyDroppedTotal").GetInt64());
        }
        finally { foreach (var row in anomalies) row.Dispose(); }
    }

    /// <summary>外部异常大正文不能扩大队列对象或在截断边界泄漏凭据。</summary>
    [Fact]
    public void Oversized_external_text_is_bounded_and_reports_omission()
    {
        using var fixture = new FileFixture();
        var body = new string('x', PipelineFileLog.MaximumTextLength) + "{\"appSecret\":\"tail-secret\"}";
        fixture.Recorder.Write("api.query.completed", "接口返回超长正文", fields: [("response", body)]);
        var line = fixture.ReadOnlyLine();
        Assert.DoesNotContain("tail-secret", line);
        using var row = JsonDocument.Parse(line);
        Assert.Equal(body.Length, row.RootElement.GetProperty("responseOriginalLength").GetInt32());
        Assert.True(row.RootElement.GetProperty("responseOmitted").GetBoolean());
        Assert.True(line.Length < 1024);
    }

    /// <summary>文件工作器尚未写出时修改原包裹，证据仍保存决策当时身份和齐套状态。</summary>
    [Fact]
    public void Package_snapshot_survives_mutation_and_identifies_the_process_run()
    {
        using var fixture = new FileFixture();
        var package = new PackageInfo { Guid = 38, Timestamp = 1001, CreateTime = DateTime.Now,
            IsCreatedByLowerMachine = true, BarCodeInfo = new() { Barcode = "JT-ORIGINAL", SerialNumber = "camera-1" } };
        fixture.Recorder.Package("package.created", "原车", package);
        package.Guid = 39;
        package.Timestamp = 1002;
        package.BarCodeInfo.Barcode = "CHANGED";
        using var row = JsonDocument.Parse(fixture.ReadOnlyLine());
        Assert.Equal(1001, row.RootElement.GetProperty("packageTimestamp").GetInt64());
        Assert.Equal(38, row.RootElement.GetProperty("cartId").GetInt64());
        Assert.Equal("JT-ORIGINAL", row.RootElement.GetProperty("barcode").GetString());
        Assert.Equal(package.CreateTime.Ticks, row.RootElement.GetProperty("creationTimeTicks").GetInt64());
        Assert.True(Guid.TryParse(row.RootElement.GetProperty("runId").GetString(), out _));
        Assert.Equal(1, row.RootElement.GetProperty("eventSequence").GetInt64());
    }

    /// <summary>日志格式化和目标异常都不能传播到业务事件，并在恢复后报告累计失败。</summary>
    [Fact]
    public void Throwing_target_cannot_interrupt_any_recording_entry_point()
    {
        using var fixture = new FileFixture();
        var broken = new LoggingConfiguration();
        broken.AddRuleForAllLevels(new ThrowingTarget { Name = "PipelineLogFile" });
        fixture.Factory.ThrowExceptions = true;
        fixture.Factory.Configuration = broken;
        Assert.Null(Record.Exception(() => fixture.Recorder.Write("test", "写入失败")));
        Assert.Null(Record.Exception(() => fixture.Recorder.Capture(new AppLogInfoModel())));
        Assert.Null(Record.Exception(() => fixture.Recorder.Package("test", "包裹", new PackageInfo())));
        Assert.Null(Record.Exception(() => fixture.Recorder.Error("test", new InvalidOperationException("异常"))));
        var recovered = new LoggingConfiguration();
        var memory = new MemoryTarget { Name = "recovered", Layout = new NLog.Layouts.JsonLayout { IncludeEventProperties = true } };
        recovered.AddRuleForAllLevels(memory);
        fixture.Factory.Configuration = recovered;
        fixture.Recorder.ReportHealth();
        Assert.Collection(memory.Logs, line => Assert.False(string.IsNullOrEmpty(line)));
        using var row = JsonDocument.Parse(memory.Logs[0]);
        Assert.Equal("diagnostics.loss", row.RootElement.GetProperty("stage").GetString());
        Assert.True(row.RootElement.GetProperty("targetErrorReportsTotal").GetInt64() >= 4);
    }

    /// <summary>模拟格式化或目标写出主动抛异常。</summary>
    private sealed class ThrowingTarget : TargetWithLayout
    {
        /// <summary>故意拒绝当前日志，用于验证业务边界。</summary>
        protected override void Write(LogEventInfo logEvent) => throw new InvalidOperationException("target unavailable");
    }
}
