using JayTom.Dws.Legacy.Contracts.Dto;

namespace JayTom.Dws.Application.Configuration;

/// <summary>集中校验图像存储和 FTP 配置。</summary>
public sealed class ImageSettingsValidator : IConfigurationValidator
{
    /// <summary>获取图像配置类型。</summary>
    public Type SettingsType => typeof(ImageSettingsDto);

    /// <summary>校验启用功能所需的目录、端口和超时。</summary>
    public IReadOnlyList<string> Validate(object settings)
    {
        var image = (ImageSettingsDto)settings;
        var errors = new List<string>();
        bool savesImages = image.IsSaveBarcodeImage || image.IsSavePanoramaImage ||
                           image.IsSaveVolumeImage || image.IsSaveOriginalImage;
        if (savesImages && string.IsNullOrWhiteSpace(image.ImageRootDirectory))
        {
            errors.Add("启用存图时必须配置根目录。");
        }
        if (image.IsFtpUploadEnabled)
        {
            if (string.IsNullOrWhiteSpace(image.FtpInfo.IpAddress))
            {
                errors.Add("启用 FTP 时必须配置服务器地址。");
            }
            if (image.FtpInfo.Port is < 1 or > 65535)
            {
                errors.Add("FTP 端口必须位于 1 到 65535 之间。");
            }
            if (image.FtpInfo.Timeout <= 0)
            {
                errors.Add("FTP 超时必须大于零。");
            }
        }
        return errors.AsReadOnly();
    }
}
