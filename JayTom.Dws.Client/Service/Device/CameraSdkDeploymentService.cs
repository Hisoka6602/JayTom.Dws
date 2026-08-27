using JayTom.Dws.Camera;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Client.Models.Cameras;
using System;
using System.Collections.Generic;
using System.IO;

namespace JayTom.Dws.Client.Service.Device;

/// <summary>集中处理相机 SDK 的目录映射、文件部署与启用策略。</summary>
public sealed class CameraSdkDeploymentService : ICameraSdkDeploymentService
{
    /// <summary>SDK 选择器与应用内相对目录的映射。</summary>
    private static readonly IReadOnlyDictionary<string, string> SdkDirectories =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["IsUseHikvisionSmartCameraSdk"] = Path.Combine("Cameras", "SmartCamera", "Hikvision", "Dll"),
            ["IsUseHikvisionIndustrialCameraSdk"] = Path.Combine("Cameras", "IndustrialCamera", "Hikvision", "Dll"),
            ["IsUseHikvisionVolumeCameraSdk"] = Path.Combine("Cameras", "VolumeCamera", "Hikvision", "Dll"),
            ["IsUseDaHuaSmartCameraSdk"] = Path.Combine("Cameras", "SmartCamera", "Irayple", "Dll"),
            ["IsUseDaHuaVolumeCameraSdk"] = Path.Combine("Cameras", "VolumeCamera", "Irayple", "Dll"),
            ["IsUseDaHuaSecurityCameraSdk"] = Path.Combine("Cameras", "SecurityCamera", "DaHuatech", "Dll"),
            ["IsUseWayzimSmartCameraSdk"] = Path.Combine("Cameras", "SmartCamera", "Wayzim", "Dll"),
            ["IsUseWayzimIndustrialCameraSdk"] = Path.Combine("Cameras", "IndustrialCamera", "Wayzim", "Dll"),
            ["IsUseDimensionVolumeCameraSdk"] = Path.Combine("Cameras", "VolumeCamera", "Dimension", "Dll")
        };

    /// <summary>将所选 SDK 的运行时文件部署到应用目录。</summary>
    public void DeploySelectedSdk(string? selectorName)
    {
        if (string.IsNullOrWhiteSpace(selectorName) ||
            !SdkDirectories.TryGetValue(selectorName, out string? relativeDirectory))
        {
            return;
        }

        string destinationDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string sourceDirectory = Path.Combine(destinationDirectory, relativeDirectory);
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        foreach (string sourcePath in Directory.EnumerateFiles(sourceDirectory))
        {
            string destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(sourcePath));
            if (!File.Exists(destinationPath))
            {
                File.Copy(sourcePath, destinationPath, overwrite: false);
            }
        }
    }

    /// <summary>判断相机所需 SDK 是否已在设置中启用。</summary>
    public bool IsSelected(CameraFinderItemInfoModel camera, CameraSdkSelectorInfoModel selection)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(selection);

        bool isHikvision = camera.Brand.Contains("Hikrobot", StringComparison.OrdinalIgnoreCase) ||
                           camera.Brand.Contains("Hikvision", StringComparison.OrdinalIgnoreCase);
        bool isDahua = camera.Brand.Contains("Dahua", StringComparison.OrdinalIgnoreCase) ||
                       camera.Brand.Contains("Huaray", StringComparison.OrdinalIgnoreCase);
        bool isWayzim = camera.Brand.Contains("Wayzim", StringComparison.OrdinalIgnoreCase);

        return (isHikvision, isDahua, isWayzim, camera.CameraType) switch
        {
            (true, _, _, CameraType.IndustrialCamera) => selection.IsUseHikvisionIndustrialCameraSdk,
            (true, _, _, CameraType.SmartCamera) => selection.IsUseHikvisionSmartCameraSdk,
            (true, _, _, CameraType.ThreeDCamera) => selection.IsUseHikvisionVolumeCameraSdk,
            (_, true, _, CameraType.SmartCamera) => selection.IsUseDaHuaSmartCameraSdk,
            (_, true, _, CameraType.VideoCamera) => selection.IsUseDaHuaSecurityCameraSdk,
            (_, true, _, CameraType.ThreeDCamera) => selection.IsUseDaHuaVolumeCameraSdk,
            (_, _, true, CameraType.SmartCamera) => selection.IsUseWayzimSmartCameraSdk,
            (_, _, true, CameraType.IndustrialCamera) => selection.IsUseWayzimIndustrialCameraSdk,
            _ => true
        };
    }
}
