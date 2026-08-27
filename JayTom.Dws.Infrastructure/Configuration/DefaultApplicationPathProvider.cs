using JayTom.Dws.Application.Deployment;

namespace JayTom.Dws.Infrastructure.Configuration;

/// <summary>使用显式选项解析和约束全部运行时路径。</summary>
internal sealed class DefaultApplicationPathProvider : IApplicationPathProvider {
    private readonly string _databaseDirectory;

    /// <summary>创建路径提供器并规范化绝对路径。</summary>
    public DefaultApplicationPathProvider(ApplicationPathOptions options) {
        DataDirectory = Normalize(options.DataDirectory);
        _databaseDirectory = Normalize(options.DatabaseDirectory);
        ConfigurationDirectory = Normalize(options.ConfigurationDirectory);
        LogDirectory = Normalize(options.LogDirectory);
        ModelDirectory = Normalize(options.ModelDirectory);
        AdapterPackDirectory = Normalize(options.AdapterPackDirectory);
    }

    /// <inheritdoc />
    public string DataDirectory { get; }

    /// <inheritdoc />
    public string ConfigurationDirectory { get; }

    /// <inheritdoc />
    public string LogDirectory { get; }

    /// <inheritdoc />
    public string ModelDirectory { get; }

    /// <inheritdoc />
    public string AdapterPackDirectory { get; }

    /// <inheritdoc />
    public string GetDatabasePath(string databaseName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        if (databaseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            !string.Equals(databaseName, Path.GetFileName(databaseName), StringComparison.Ordinal)) {
            throw new ArgumentException("数据库名称必须是安全文件名。", nameof(databaseName));
        }

        return Path.Combine(_databaseDirectory, databaseName);
    }

    private static string Normalize(string path) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }
}
