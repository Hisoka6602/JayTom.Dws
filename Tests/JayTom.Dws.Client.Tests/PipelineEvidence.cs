using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace JayTom.Dws.Tests.Application;

/// <summary>为真实客户端流程临时添加独立异步文件目标，验证决策确实生成可关联的落盘证据。</summary>
internal sealed class PipelineEvidence : IDisposable
{
    /// <summary>保护测试目标的增删，允许不同测试并行写入各自的证据文件。</summary>
    private static readonly object ConfigurationGate = new();
    /// <summary>临时目录只包含本测试生成的文件。</summary>
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("dws-client-evidence-");
    /// <summary>通过固定名称的生产记录器采集每个流程阶段。</summary>
    private readonly LoggingRule _rule;
    /// <summary>避免设备事件等待文件目标。</summary>
    private readonly AsyncTargetWrapper _target;

    /// <summary>保留已有规则并添加临时异步目标。</summary>
    internal PipelineEvidence()
    {
        _target = new AsyncTargetWrapper(new FileTarget
        {
            FileName = Path.Combine(_directory.FullName, "evidence.jsonl"),
            KeepFileOpen = false,
            Layout = new JsonLayout { IncludeEventProperties = true }
        }, 20_000, AsyncTargetWrapperOverflowAction.Discard) { Name = "evidence-" + Guid.NewGuid().ToString("N") };
        _rule = new LoggingRule("Dws.Pipeline", LogLevel.Trace, _target);
        lock (ConfigurationGate)
        {
            var configuration = LogManager.Configuration ?? new LoggingConfiguration();
            configuration.AddTarget(_target);
            configuration.LoggingRules.Insert(0, _rule);
            LogManager.Configuration = configuration;
        }
    }

    /// <summary>读取实际文件中与指定原包裹匹配的标量证据。</summary>
    internal JsonElement[] ForPackage(long timestamp)
    {
        LogManager.Flush(TimeSpan.FromSeconds(10));
        var path = Path.Combine(_directory.FullName, "evidence.jsonl");
        if (!File.Exists(path)) return [];
        var rows = new System.Collections.Generic.List<JsonElement>();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                var row = document.RootElement;
                if (row.TryGetProperty("packageTimestamp", out var id) && id.GetInt64() == timestamp)
                    rows.Add(row.Clone());
            }
            catch (JsonException) when (reader.EndOfStream) { }
        }
        return rows.ToArray();
    }

    /// <summary>撤销本测试规则并清理已关闭的临时文件，不改变其他测试目标。</summary>
    public void Dispose()
    {
        LogManager.Flush(TimeSpan.FromSeconds(10));
        lock (ConfigurationGate)
        {
            var configuration = LogManager.Configuration;
            configuration?.LoggingRules.Remove(_rule);
            configuration?.RemoveTarget(_target.Name);
            LogManager.Configuration = configuration;
        }
        _target.Dispose();
        _directory.Delete(true);
    }
}
