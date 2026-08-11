using System;
using JayTom.Dws.Camera;
using JayTom.Dws.Data.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.Service.Device;

/// <summary>表示全景相机配置已修改。</summary>
public sealed class PanoramaCameraParametersModifiedEventArgs : CameraParametersModifiedEventArgs {
    /// <summary>初始化全景相机配置变更。</summary>
    public PanoramaCameraParametersModifiedEventArgs(PanoramaCameraConfigInfoModel parameters)
        : base(CameraBindingType.PanoramaCamera) {
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    /// <summary>获取强类型全景相机配置。</summary>
    public PanoramaCameraConfigInfoModel Parameters { get; }
}
