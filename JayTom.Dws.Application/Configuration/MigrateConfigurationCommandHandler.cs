using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Application.UseCases;
using JayTom.Dws.Abstractions.Observability;
using System.Diagnostics;

namespace JayTom.Dws.Application.Configuration;

/// <summary>处理配置迁移命令，并把原子提交委托给配置事务边界。</summary>
public sealed class MigrateConfigurationCommandHandler :
    IApplicationCommandHandler<MigrateConfigurationCommand, ConfigurationMigrationReceipt>
{
    /// <summary>配置迁移运行器。</summary>
    private readonly ConfigurationMigrationRunner _runner;
    /// <summary>命令输入校验器。</summary>
    private readonly IApplicationRequestValidator<MigrateConfigurationCommand> _validator;

    /// <summary>创建处理器。</summary>
    public MigrateConfigurationCommandHandler(
        ConfigurationMigrationRunner runner,
        IApplicationRequestValidator<MigrateConfigurationCommand> validator)
    {
        _runner = runner;
        _validator = validator;
    }

    /// <summary>校验并执行配置迁移命令。</summary>
    public async Task<OperationResult<ConfigurationMigrationReceipt>> HandleAsync(
        MigrateConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        using CorrelationScope correlation = CorrelationContext.Begin(CorrelationContext.CurrentValueText);
        using Activity? activity = DwsDiagnostics.StartActivity("configuration.migrate");
        long started = Stopwatch.GetTimestamp();
        try
        {
            IReadOnlyList<Error> errors = _validator.Validate(command);
            OperationResult<ConfigurationMigrationReceipt> result = errors.Count == 0
                ? await _runner.MigrateAsync(command.TargetVersion, cancellationToken).ConfigureAwait(false)
                : OperationResult<ConfigurationMigrationReceipt>.Failure(errors[0]);
            activity?.SetStatus(result.IsSuccess ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
            DwsDiagnostics.RecordOperation(
                "configuration.migrate",
                result.IsSuccess,
                Stopwatch.GetElapsedTime(started));
            return result;
        }
        catch
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            DwsDiagnostics.RecordOperation(
                "configuration.migrate",
                false,
                Stopwatch.GetElapsedTime(started));
            throw;
        }
    }
}
