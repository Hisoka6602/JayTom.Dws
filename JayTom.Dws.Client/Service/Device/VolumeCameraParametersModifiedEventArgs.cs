using System;
using JayTom.Dws.Camera;
using JayTom.Dws.Data.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.Service.Device;

/// <summary>表示体积相机配置已修改。</summary>
public sealed class VolumeCameraParametersModifiedEventArgs : CameraParametersModifiedEventArgs {
    /// <summary>初始化体积相机配置变更。</summary>
    public VolumeCameraParametersModifiedEventArgs(VolumeCameraConfigInfoModel parameters)
        : base(CameraBindingType.VolumeCamera) {
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    /// <summary>获取强类型体积相机配置。</summary>
    public VolumeCameraConfigInfoModel Parameters { get; }
}
