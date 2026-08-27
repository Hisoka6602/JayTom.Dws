namespace JayTom.Dws.Application.Configuration;

/// <summary>定义配置对象所属的业务模块。</summary>
public enum ConfigurationModule
{
    /// <summary>设备采集配置。</summary>
    Device,

    /// <summary>分拣流程配置。</summary>
    Sorting,

    /// <summary>结果与文件输出配置。</summary>
    Output,

    /// <summary>外部系统集成配置。</summary>
    Integration,

    /// <summary>运行维护配置。</summary>
    Maintenance
}
