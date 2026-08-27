using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Application.PackageExits;

/// <summary>为展示层提供稳定的包裹出口只读目录。</summary>
public interface IPackageExitCatalog {
    /// <summary>按创建时间读取全部有效标识的出口定义。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>出口定义只读快照。</returns>
    Task<IReadOnlyList<PackageExitDefinitionInfoModel>> ListAsync(
        CancellationToken cancellationToken = default);
}
