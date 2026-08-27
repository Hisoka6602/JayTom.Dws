using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Models;

namespace JayTom.Dws.Client.Service.Device;

/// <summary>定义相机 SDK 文件部署与选择校验边界。</summary>
public interface ICameraSdkDeploymentService
{
    /// <summary>将所选 SDK 的运行时文件部署到应用目录。</summary>
    /// <param name="selectorName">SDK 选择器属性名。</param>
    void DeploySelectedSdk(string? selectorName);

    /// <summary>判断相机所需 SDK 是否已在设置中启用。</summary>
    /// <param name="camera">待绑定相机。</param>
    /// <param name="selection">SDK 选择状态。</param>
    /// <returns>所需 SDK 已启用时返回 <see langword="true"/>。</returns>
    bool IsSelected(CameraFinderItemInfoModel camera, CameraSdkSelectorInfoModel selection);
}
