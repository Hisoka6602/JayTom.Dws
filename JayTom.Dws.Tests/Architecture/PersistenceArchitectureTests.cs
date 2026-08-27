using System.Reflection;
using JayTom.Dws.Legacy.Contracts.Repositories;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定仓储契约、EF 映射、查询策略和数据库演进边界。</summary>
public sealed class PersistenceArchitectureTests
{
    /// <summary>仓库根目录。</summary>
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    /// <summary>日志用例仓储必须聚合读写和维护能力，具体类型不得复制维护 SQL。</summary>
    [Fact]
    public void Log_repositories_converge_on_use_case_boundary()
    {
        string contract = Read("JayTom.Dws.Legacy.Contracts", "Repository", "LocalLog", "ILogMaintenanceRepository.cs");
        string implementation = Read(
            "JayTom.Dws.Infrastructure",
            "Repository",
            "LocalLog",
            "LogMaintenanceRepositoryBase.cs");

        Assert.Contains("IRepository<TLog>", contract, StringComparison.Ordinal);
        Assert.Contains("CancellationToken", contract, StringComparison.Ordinal);
        Assert.Contains("ExecuteDeleteAsync(cancellationToken)", implementation, StringComparison.Ordinal);
        Assert.True(Directory.EnumerateFiles(
                Path.Combine(RepositoryRoot, "JayTom.Dws.Infrastructure", "Repository", "LocalLog"),
                "*LogRepository.cs")
            .All(path => !File.ReadAllText(path).Contains("ExecuteDeleteAsync", StringComparison.Ordinal)));
    }

    /// <summary>兼容仓储必须显式组合只读和写入端口。</summary>
    [Fact]
    public void Repository_contract_separates_reads_and_writes()
    {
        string repository = Read("JayTom.Dws.Legacy.Contracts", "Repository", "IRepository.cs");

        Assert.Contains("IReadRepository<T>, IWriteRepository<T>", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("Task<", repository, StringComparison.Ordinal);
    }

    /// <summary>所有仓储异步方法必须把取消令牌作为契约的一部分。</summary>
    [Fact]
    public void Repository_async_contracts_accept_cancellation()
    {
        MethodInfo[] methods = typeof(IRepository<>).Assembly.GetTypes()
            .Where(type => type.IsInterface &&
                           type.Namespace?.StartsWith("JayTom.Dws.Legacy.Contracts.Repositories", StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType))
            .ToArray();

        string[] violations = methods
            .Where(method => method.GetParameters().All(parameter => parameter.ParameterType != typeof(CancellationToken)))
            .Select(method => $"{method.DeclaringType?.FullName}.{method.Name}")
            .ToArray();
        Assert.Empty(violations);
    }

    /// <summary>数据库演进不得再回退到运行时 EnsureCreated。</summary>
    [Fact]
    public void Database_evolution_uses_bootstrap_plus_versioned_migrations()
    {
        string initializer = Read("JayTom.Dws.Infrastructure", "SqliteDatabaseInitializer.cs");
        string migrator = Read("JayTom.Dws.Infrastructure", "Migrations", "SqliteSchemaMigrator.cs");

        Assert.DoesNotContain("EnsureCreated(", initializer, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureCreated(", migrator, StringComparison.Ordinal);
        Assert.Contains("CreateTables()", migrator, StringComparison.Ordinal);
        Assert.Contains("Database.Migrate()", migrator, StringComparison.Ordinal);
    }

    /// <summary>DbContext 只负责编排，包裹和日志映射由模块化 Fluent Configuration 拥有。</summary>
    [Fact]
    public void DbContexts_delegate_entity_mapping_to_modules()
    {
        string dataContext = Read("JayTom.Dws.Infrastructure", "SqliteContext.cs");
        string logContext = Read("JayTom.Dws.Infrastructure", "SqliteLogsContext.cs");
        string packageConfiguration = Read(
            "JayTom.Dws.Infrastructure", "Persistence", "ModelConfigurations", "Packages",
            "PackageDataModelConfiguration.cs");
        string logConfiguration = Read(
            "JayTom.Dws.Infrastructure", "Persistence", "ModelConfigurations", "Logs",
            "LogEntityConfiguration.cs");

        Assert.Contains("PackageModelConfigurations.Apply", dataContext, StringComparison.Ordinal);
        Assert.Contains("LogModelConfigurations.Apply", logContext, StringComparison.Ordinal);
        Assert.DoesNotContain("modelBuilder.Entity<", dataContext, StringComparison.Ordinal);
        Assert.DoesNotContain("modelBuilder.Entity<", logContext, StringComparison.Ordinal);
        Assert.Contains("IEntityTypeConfiguration<PackageInfoModel>", packageConfiguration, StringComparison.Ordinal);
        Assert.Contains("IEntityTypeConfiguration<TLog>", logConfiguration, StringComparison.Ordinal);
    }

    /// <summary>默认查询不跟踪，热点查询必须投影并限制结果集。</summary>
    [Fact]
    public void Queries_default_to_no_tracking_projection_and_limits()
    {
        string localBase = Read("JayTom.Dws.Infrastructure", "Repository", "LocalRepositoryBase.cs");
        string remoteBase = Read("JayTom.Dws.Infrastructure", "Repository", "RepositoryBase.cs");
        string package = Read(
            "JayTom.Dws.Infrastructure", "Repository", "LocalData", "PackageRepository.cs");
        string logs = Read(
            "JayTom.Dws.Infrastructure", "Repository", "LocalLog", "LogMaintenanceRepositoryBase.cs");

        Assert.Contains("AsNoTracking()", localBase, StringComparison.Ordinal);
        Assert.Contains("AsNoTracking()", remoteBase, StringComparison.Ordinal);
        Assert.Contains("Math.Clamp(pageSize, 1, 1000)", package, StringComparison.Ordinal);
        Assert.Contains(".Take(pageSize)", package, StringComparison.Ordinal);
        Assert.Contains(".Select(log => (DateTime?)log.CreateTime)", logs, StringComparison.Ordinal);
    }

    /// <summary>批量写入必须由显式事务保护且有 SQLite 兼容回归测试。</summary>
    [Fact]
    public void Batch_writes_are_transactional_and_compatibility_tested()
    {
        string localBase = Read("JayTom.Dws.Infrastructure", "Repository", "LocalRepositoryBase.cs");
        string remoteBase = Read("JayTom.Dws.Infrastructure", "Repository", "RepositoryBase.cs");
        string compatibility = Read(
            "JayTom.Dws.Tests", "Persistence", "SqliteCompatibilityTests.cs");

        Assert.Contains("BeginTransactionAsync(token)", localBase, StringComparison.Ordinal);
        Assert.Contains("CommitAsync(token)", localBase, StringComparison.Ordinal);
        Assert.Contains("BeginTransactionAsync(token)", remoteBase, StringComparison.Ordinal);
        Assert.Contains("LegacyDatabase_RemainsStructurallyCompatible", compatibility, StringComparison.Ordinal);
        Assert.Contains("EmptyDatabase_BootstrapsSchemaBeforeApplyingMigrations", compatibility, StringComparison.Ordinal);
    }

    /// <summary>配置快照替换必须通过显式工作单元控制事务、回滚和提交后的缓存刷新。</summary>
    [Fact]
    public void Configuration_snapshot_uses_explicit_unit_of_work()
    {
        string settingsStore = Read(
            "JayTom.Dws.Infrastructure", "Configuration", "SettingsStore.cs");
        string unitOfWork = Read(
            "JayTom.Dws.Infrastructure", "Configuration", "ConfigurationUnitOfWork.cs");

        Assert.Contains("IConfigurationUnitOfWork", settingsStore, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransactionAsync", settingsStore, StringComparison.Ordinal);
        Assert.Contains("BeginTransactionAsync(cancellationToken)", unitOfWork, StringComparison.Ordinal);
        Assert.Contains("CommitAsync(cancellationToken)", unitOfWork, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync(CancellationToken.None)", unitOfWork, StringComparison.Ordinal);
        Assert.Contains("UpdateMemoryCache(cancellationToken)", unitOfWork, StringComparison.Ordinal);
    }

    /// <summary>读取仓库内指定源文件。</summary>
    private static string Read(params string[] segments) =>
        File.ReadAllText(Path.Combine([RepositoryRoot, .. segments]));

    /// <summary>从测试输出目录向上定位仓库根目录。</summary>
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "JayTom.Dws.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName
               ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
