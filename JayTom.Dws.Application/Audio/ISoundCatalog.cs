using JayTom.Dws.Models.LocalData;

namespace JayTom.Dws.Application.Audio;

/// <summary>
/// 提供音频输出配置的应用层读写边界。
/// </summary>
public interface ISoundCatalog {
    /// <summary>统计已配置音频的数量。</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>按稳定顺序读取全部音频配置。</summary>
    Task<IReadOnlyList<SoundInfoModel>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>保存或更新一个音频配置。</summary>
    Task<bool> SaveAsync(SoundInfoModel sound, CancellationToken cancellationToken = default);
}

