using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Application.Configuration;

/// <summary>将配置存储的异常与布尔返回值转换为稳定的应用结果。</summary>
public sealed class SettingsAccess : ISettingsAccess
{
    /// <summary>底层配置存储。</summary>
    private readonly ISettingsStore _store;

    /// <summary>应用层配置校验注册表。</summary>
    private readonly ConfigurationValidationRegistry _validationRegistry;

    /// <summary>创建配置访问服务。</summary>
    /// <param name="store">底层配置存储。</param>
    public SettingsAccess(
        ISettingsStore store,
        ConfigurationValidationRegistry? validationRegistry = null)
    {
        _store = store;
        _validationRegistry = validationRegistry ?? new ConfigurationValidationRegistry([]);
    }

    /// <summary>读取配置并返回显式结果。</summary>
    public async Task<OperationResult<TSettings?>> ReadAsync<TSettings>(
        string key,
        CancellationToken cancellationToken = default)
        where TSettings : class
    {
        try
        {
            TSettings? settings = await _store.GetAsync<TSettings>(key, cancellationToken);
            return OperationResult<TSettings?>.Success(settings);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return OperationResult<TSettings?>.Failure(Error.Cancelled);
        }
        catch (Exception exception)
        {
            return OperationResult<TSettings?>.Failure(
                new Error("configuration.read_failed", exception.Message));
        }
    }

    /// <summary>保存配置并返回显式结果。</summary>
    public async Task<Result> SaveAsync<TSettings>(
        string key,
        TSettings settings,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> validationErrors = _validationRegistry.Validate(settings!);
        if (validationErrors.Count > 0)
        {
            return Result.Failure(new Error(
                "configuration.validation_failed",
                string.Join(" ", validationErrors)));
        }
        try
        {
            bool saved = await _store.SaveAsync(key, settings, cancellationToken);
            return saved
                ? Result.Success()
                : Result.Failure(new Error("configuration.save_failed", "配置存储未接受本次写入。"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(Error.Cancelled);
        }
        catch (Exception exception)
        {
            return Result.Failure(new Error("configuration.save_failed", exception.Message));
        }
    }
}
