using JayTom.Dws.Models.LocalData;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;

namespace JayTom.Dws.Application.Audio;

/// <summary>
/// 使用声音仓储实现音频输出配置用例。
/// </summary>
public sealed class SoundCatalog : ISoundCatalog {
    /// <summary>持有声音配置的持久化边界。</summary>
    private readonly ISoundRepository _repository;

    /// <summary>创建音频输出配置目录。</summary>
    public SoundCatalog(ISoundRepository repository) {
        _repository = repository;
    }

    /// <summary>统计已配置音频的数量。</summary>
    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _repository.Total(item => item.Id > 0, cancellationToken);

    /// <summary>按稳定顺序读取全部音频配置。</summary>
    public async Task<IReadOnlyList<SoundInfoModel>> ListAsync(CancellationToken cancellationToken = default) =>
        await _repository.Select(item => item.Id > 0, item => item.Id, cancellationToken)
            .ConfigureAwait(false);

    /// <summary>保存或更新一个音频配置。</summary>
    public Task<bool> SaveAsync(SoundInfoModel sound, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(sound);
        return _repository.InsertOrUpdate(sound, cancellationToken);
    }
}
