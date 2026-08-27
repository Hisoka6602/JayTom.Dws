using JayTom.Dws.Application.UseCases;

namespace JayTom.Dws.Application.Configuration;

/// <summary>将配置架构原子迁移到目标版本的应用命令。</summary>
public sealed record MigrateConfigurationCommand(int TargetVersion) :
    ITransactionalApplicationCommand<ConfigurationMigrationReceipt>;
