using System;
using JayTom.Dws.Camera;
using JayTom.Dws.Data.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.Service.Device;

/// <summary>表示扫码相机配置已修改。</summary>
public sealed class ScannerCameraParametersModifiedEventArgs : CameraParametersModifiedEventArgs {
    /// <summary>初始化扫码相机配置变更。</summary>
    public ScannerCameraParametersModifiedEventArgs(BarcodeScannerCameraConfigInfoModel parameters)
        : base(CameraBindingType.ScannerCamera) {
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    /// <summary>获取强类型扫码相机配置。</summary>
    public BarcodeScannerCameraConfigInfoModel Parameters { get; }
}
