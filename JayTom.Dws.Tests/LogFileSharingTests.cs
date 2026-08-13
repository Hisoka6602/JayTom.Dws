using NLog;
using NLog.Config;
using NLog.Targets;

namespace JayTom.Dws.Tests;

/// <summary>验证运行期日志文件保持高性能长开句柄时仍允许复制。</summary>
public sealed class LogFileSharingTests
{
    /// <summary>日志目标持续打开后，另一个读取方仍应能复制当前活动文件。</summary>
    [Fact]
    public void FileTarget_AllowsCopyWhileActive()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"JayTom-Dws-LogShare-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var sourcePath = Path.Combine(directory, "active.log");
        var copyPath = Path.Combine(directory, "copy.log");
        LogFactory? factory = null;

        try
        {
            var target = new FileTarget("shareable-file")
            {
                FileName = sourcePath,
                KeepFileOpen = true,
                ConcurrentWrites = true
            };
            factory = new LogFactory();
            var configuration = new LoggingConfiguration(factory);
            configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, target);
            factory.Configuration = configuration;
            factory.GetLogger("copy-probe").Info("copy while active");
            factory.Flush();

            File.Copy(sourcePath, copyPath);

            Assert.Contains("copy while active", File.ReadAllText(copyPath));
        }
        finally
        {
            factory?.Dispose();
            Directory.Delete(directory, recursive: true);
        }
    }
}
