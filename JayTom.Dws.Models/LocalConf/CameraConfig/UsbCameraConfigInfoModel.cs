using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf.CameraConfig {

    [Table("Conf_UsbCameraConfigInfo", Schema = "dbo")]
    public class UsbCameraConfigInfoModel : BaseCameraConfigInfoModel {

        /// <summary>
        /// 曝光度
        /// </summary>
        [Column("Exposure"), Required, InsertOrUpdate]
        public int Exposure { get; set; }

        /// <summary>
        /// 亮度
        /// </summary>
        [Column("Brightness"), Required, InsertOrUpdate]
        public int Brightness { get; set; }

        /// <summary>
        /// 对比度
        /// </summary>
        [Column("Contrast"), Required, InsertOrUpdate]
        public int Contrast { get; set; }

        /// <summary>
        /// 色调
        /// </summary>
        [Column("Hue"), Required, InsertOrUpdate]
        public int Hue { get; set; }

        /// <summary>
        /// 饱和度
        /// </summary>
        [Column("Saturation"), Required, InsertOrUpdate]
        public int Saturation { get; set; }

        /// <summary>
        /// 锐度
        /// </summary>
        [Column("Sharpness"), Required, InsertOrUpdate]
        public int Sharpness { get; set; }

        /// <summary>
        /// 伽马值
        /// </summary>
        [Column("Gamma"), Required, InsertOrUpdate]
        public int Gamma { get; set; }

        /// <summary>
        /// 白平衡
        /// </summary>
        [Column("WhiteBalance"), Required, InsertOrUpdate]
        public int WhiteBalance { get; set; }

        /// <summary>
        /// 背光补偿
        /// </summary>
        [Column("BklightComp"), Required, InsertOrUpdate]
        public int BklightComp { get; set; }

        /// <summary>
        /// 增益
        /// </summary>
        [Column("Gain"), Required, InsertOrUpdate]
        public int Gain { get; set; }

        /// <summary>
        /// 变焦
        /// </summary>
        [Column("Zoom"), Required, InsertOrUpdate]
        public int Zoom { get; set; }

        /// <summary>
        /// 对焦
        /// </summary>
        [Column("Focus"), Required, InsertOrUpdate]
        public int Focus { get; set; }

        /// <summary>
        /// 光圈
        /// </summary>
        [Column("Iris"), Required, InsertOrUpdate]
        public int Iris { get; set; }

        /// <summary>
        /// 水平旋转
        /// </summary>
        [Column("Pan"), Required, InsertOrUpdate]
        public int Pan { get; set; }

        /// <summary>
        /// 垂直旋转
        /// </summary>
        [Column("Tilt"), Required, InsertOrUpdate]
        public int Tilt { get; set; }

        /// <summary>
        /// 翻转
        /// </summary>
        [Column("Roll"), Required, InsertOrUpdate]
        public int Roll { get; set; }

        /// <summary>
        /// 是否自定义曝光度
        /// </summary>
        [Column("IsCustomExposureEnabled"), Required, InsertOrUpdate]
        public bool IsCustomExposureEnabled { get; set; }

        /// <summary>
        /// 是否自定义亮度
        /// </summary>
        [Column("IsCustomBrightnessEnabled"), Required, InsertOrUpdate]
        public bool IsCustomBrightnessEnabled { get; set; }

        /// <summary>
        /// 是否自定义对比度
        /// </summary>
        [Column("IsCustomContrastEnabled"), Required, InsertOrUpdate]
        public bool IsCustomContrastEnabled { get; set; }

        /// <summary>
        /// 是否自定义色调
        /// </summary>
        [Column("IsCustomHueEnabled"), Required, InsertOrUpdate]
        public bool IsCustomHueEnabled { get; set; }

        /// <summary>
        /// 是否自定义饱和度
        /// </summary>
        [Column("IsCustomSaturationEnabled"), Required, InsertOrUpdate]
        public bool IsCustomSaturationEnabled { get; set; }

        /// <summary>
        /// 是否自定义锐度
        /// </summary>
        [Column("IsCustomSharpnessEnabled"), Required, InsertOrUpdate]
        public bool IsCustomSharpnessEnabled { get; set; }

        /// <summary>
        /// 是否自定义伽马值
        /// </summary>
        [Column("IsCustomGammaEnabled"), Required, InsertOrUpdate]
        public bool IsCustomGammaEnabled { get; set; }

        /// <summary>
        /// 是否自定义白平衡
        /// </summary>
        [Column("IsCustomWhiteBalanceEnabled"), Required, InsertOrUpdate]
        public bool IsCustomWhiteBalanceEnabled { get; set; }

        /// <summary>
        /// 是否自定义背光补偿
        /// </summary>
        [Column("IsCustomBacklightCompensationEnabled"), Required, InsertOrUpdate]
        public bool IsCustomBacklightCompensationEnabled { get; set; }

        /// <summary>
        /// 是否自定义增益
        /// </summary>
        [Column("IsCustomGainEnabled"), Required, InsertOrUpdate]
        public bool IsCustomGainEnabled { get; set; }

        /// <summary>
        /// 是否自定义变焦
        /// </summary>
        [Column("IsCustomZoomEnabled"), Required, InsertOrUpdate]
        public bool IsCustomZoomEnabled { get; set; }

        /// <summary>
        /// 是否自定义对焦
        /// </summary>
        [Column("IsCustomFocusEnabled"), Required, InsertOrUpdate]
        public bool IsCustomFocusEnabled { get; set; }

        /// <summary>
        /// 是否自定义光圈
        /// </summary>
        [Column("IsCustomApertureEnabled"), Required, InsertOrUpdate]
        public bool IsCustomApertureEnabled { get; set; }

        /// <summary>
        /// 是否自定义水平旋转
        /// </summary>
        [Column("IsCustomHorizontalRotationEnabled"), Required, InsertOrUpdate]
        public bool IsCustomHorizontalRotationEnabled { get; set; }

        /// <summary>
        /// 是否自定义垂直旋转
        /// </summary>
        [Column("IsCustomVerticalRotationEnabled"), Required, InsertOrUpdate]
        public bool IsCustomVerticalRotationEnabled { get; set; }

        /// <summary>
        /// 是否自定义翻转
        /// </summary>
        [Column("IsCustomFlipEnabled"), Required, InsertOrUpdate]
        public bool IsCustomFlipEnabled { get; set; }

        /// <summary>
        /// 分辨率宽度
        /// </summary>
        [Column("ResolutionWidth"), Required, InsertOrUpdate]
        public int ResolutionWidth { get; set; }

        /// <summary>
        /// 分辨率高度
        /// </summary>
        [Column("ResolutionHeight"), Required, InsertOrUpdate]
        public int ResolutionHeight { get; set; }
    }
}