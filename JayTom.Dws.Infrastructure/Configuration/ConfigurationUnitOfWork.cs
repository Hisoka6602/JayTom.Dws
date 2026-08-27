using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Models.LocalConf;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using Microsoft.EntityFrameworkCore;

namespace JayTom.Dws.Infrastructure.Configuration;

/// <summary>使用单一 EF 上下文和事务实现配置快照工作单元。</summary>
internal sealed class ConfigurationUnitOfWork : IConfigurationUnitOfWork
{
    /// <summary>配置数据库上下文工厂。</summary>
    private readonly IDbContextFactory<SqliteConfContext> _contextFactory;

    /// <summary>提交后需要刷新的配置缓存。</summary>
    private readonly IConfigRepository _repository;

    /// <summary>创建配置工作单元。</summary>
    public ConfigurationUnitOfWork(
        IDbContextFactory<SqliteConfContext> contextFactory,
        IConfigRepository repository)
    {
        _contextFactory = contextFactory;
        _repository = repository;
    }

    /// <summary>在明确事务中删除旧快照、写入新快照、提交并刷新缓存。</summary>
    public async Task<bool> ReplaceSnapshotAsync(
        IReadOnlyDictionary<string, string> snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using SqliteConfContext context = await _contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            await context.Set<ConfigInfoModel>()
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
            context.Set<ConfigInfoModel>().AddRange(snapshot.Select(pair => new ConfigInfoModel
            {
                ConfigName = pair.Key,
                Value = pair.Value
            }));
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await _repository.UpdateMemoryCache(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }
}
