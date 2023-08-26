using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;

namespace JayTom.Dws.Domain.Dto
{

    public class ImageSettingsDto {

        /// <summary>
        /// 存图根目录
        /// </summary>
        public string ImageRootDirectory { get; set; } = string.Empty;

        /// <summary>
        ///  是否保存扫码图
        /// </summary>
        public bool IsSaveBarcodeImage { get; set; }

        /// <summary>
        /// 是否保存全景图
        /// </summary>
        public bool IsSavePanoramaImage { get; set; }

        /// <summary>
        /// 是否保存体积图
        /// </summary>
        public bool IsSaveVolumeImage { get; set; }

        /// <summary>
        /// 是否保存原图
        /// </summary>
        public bool IsSaveOriginalImage { get; set; }

        /// <summary>
        /// 是否使用水印
        /// </summary>
        public bool IsUseWatermark { get; set; }

        /// <summary>
        /// 水印信息
        /// </summary>
        public WatermarkInfo WatermarkInfo { get; set; } = new();

        /// <summary>
        /// 子路径内容模板
        /// </summary>
        public List<ItemTemplateInfo>? SubDirectoryTemplate { get; set; } = new();

        /// <summary>
        /// 图片命名内容模板
        /// </summary>
        public List<ItemTemplateInfo> ImageNamingTemplate { get; set; } = new();

        /// <summary>
        /// 是否使用Ftp上传
        /// </summary>
        public bool IsFtpUploadEnabled { get; set; }

        /// <summary>
        /// Ftp信息
        /// </summary>
        public FtpInfo FtpInfo { get; set; } = new();
    }

    public class WatermarkInfo {

        /// <summary>
        /// 水印颜色
        /// </summary>
        public Color WatermarkColor { get; set; }

        /// <summary>
        /// 水印字体大小
        /// </summary>
        public int WatermarkFontSize { get; set; }

        /// <summary>
        /// 水印位置
        /// </summary>
        public WatermarkPosition WatermarkPosition { get; set; }

        /// <summary>
        /// 水印内容模板
        /// </summary>
        public List<ItemTemplateInfo> ItemTemplate { get; set; } = new();
    }

    public class FtpInfo {

        /// <summary>
        /// Ip地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 超时时间
        /// </summary>
        public int Timeout { get; set; }
    }

    public enum WatermarkPosition {
        TopLeft = 0,
        BottomLeft = 1,
        TopRight = 2,
        BottomRight = 3
    }

    public enum SaveImageType {

        /// <summary>
        /// 扫码图片
        /// </summary>
        BarcodeImage,

        /// <summary>
        /// 全景图片
        /// </summary>
        PanoramaImage,

        /// <summary>
        /// 体积图片
        /// </summary>
        VolumeImage
    }
}