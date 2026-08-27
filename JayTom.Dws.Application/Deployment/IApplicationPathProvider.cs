namespace JayTom.Dws.Application.Deployment;

/// <summary>集中提供数据库、配置、模型、日志和厂商 SDK 的绝对路径。</summary>
public interface IApplicationPathProvider {
    /// <summary>获取应用数据目录。</summary>
    string DataDirectory { get; }

    /// <summary>获取配置目录。</summary>
    string ConfigurationDirectory { get; }

    /// <summary>获取日志目录。</summary>
    string LogDirectory { get; }

    /// <summary>获取模型包目录。</summary>
    string ModelDirectory { get; }

    /// <summary>获取可选厂商 SDK 目录。</summary>
    string AdapterPackDirectory { get; }

    /// <summary>解析数据库文件路径并防止越出数据目录。</summary>
    string GetDatabasePath(string databaseName);
}
