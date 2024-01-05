using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.CameraConfig {

    [Table("Conf_UsbCameraConfigInfo", Schema = "dbo")]
    public class UsbCameraConfigInfoModel : BaseCameraConfigInfoModel {

        /// <summary>
        /// 曝光度
        /// </summary>
        [Column("Exposure"), Required, InsertOrUpdata]
        public int Exposure { get; set; }

        /// <summary>
        /// 分辨率
        /// </summary>
        [NotMapped]
        public Size Resolution { get; set; }

        /// <summary>
        /// 亮度
        /// </summary>
        [Column("Brightness"), Required, InsertOrUpdata]
        public int Brightness { get; set; }

        /// <summary>
        /// 对比度
        /// </summary>
        [Column("Contrast"), Required, InsertOrUpdata]
        public int Contrast { get; set; }

        /// <summary>
        /// 色调
        /// </summary>
        [Column("Hue"), Required, InsertOrUpdata]
        public int Hue { get; set; }

        /// <summary>
        /// 饱和度
        /// </summary>
        [Column("Saturation"), Required, InsertOrUpdata]
        public int Saturation { get; set; }

        /// <summary>
        /// 锐度
        /// </summary>
        [Column("Sharpness"), Required, InsertOrUpdata]
        public int Sharpness { get; set; }

        /// <summary>
        /// 伽马值
        /// </summary>
        [Column("Gamma"), Required, InsertOrUpdata]
        public int Gamma { get; set; }

        /// <summary>
        /// 白平衡
        /// </summary>
        [Column("WhiteBalance"), Required, InsertOrUpdata]
        public int WhiteBalance { get; set; }

        /// <summary>
        /// 背光补偿
        /// </summary>
        [Column("BklightComp"), Required, InsertOrUpdata]
        public int BklightComp { get; set; }

        /// <summary>
        /// 增益
        /// </summary>
        [Column("Gain"), Required, InsertOrUpdata]
        public int Gain { get; set; }

        /// <summary>
        /// 变焦
        /// </summary>
        [Column("Zoom"), Required, InsertOrUpdata]
        public int Zoom { get; set; }

        /// <summary>
        /// 对焦
        /// </summary>
        [Column("Focus"), Required, InsertOrUpdata]
        public int Focus { get; set; }

        /// <summary>
        /// 光圈
        /// </summary>
        [Column("Iris"), Required, InsertOrUpdata]
        public int Iris { get; set; }

        /// <summary>
        /// 水平旋转
        /// </summary>
        [Column("Pan"), Required, InsertOrUpdata]
        public int Pan { get; set; }

        /// <summary>
        /// 垂直旋转
        /// </summary>
        [Column("Tilt"), Required, InsertOrUpdata]
        public int Tilt { get; set; }

        /// <summary>
        /// 翻转
        /// </summary>
        [Column("Roll"), Required, InsertOrUpdata]
        public int Roll { get; set; }

        /// <summary>
        /// 是否自定义曝光度
        /// </summary>
        [Column("IsCustomExposureEnabled"), Required, InsertOrUpdata]
        public bool IsCustomExposureEnabled { get; set; }

        /// <summary>
        /// 是否自定义亮度
        /// </summary>
        [Column("IsCustomBrightnessEnabled"), Required, InsertOrUpdata]
        public bool IsCustomBrightnessEnabled { get; set; }

        /// <summary>
        /// 是否自定义对比度
        /// </summary>
        [Column("IsCustomContrastEnabled"), Required, InsertOrUpdata]
        public bool IsCustomContrastEnabled { get; set; }

        /// <summary>
        /// 是否自定义色调
        /// </summary>
        [Column("IsCustomHueEnabled"), Required, InsertOrUpdata]
        public bool IsCustomHueEnabled { get; set; }

        /// <summary>
        /// 是否自定义饱和度
        /// </summary>
        [Column("IsCustomSaturationEnabled"), Required, InsertOrUpdata]
        public bool IsCustomSaturationEnabled { get; set; }

        /// <summary>
        /// 是否自定义锐度
        /// </summary>
        [Column("IsCustomSharpnessEnabled"), Required, InsertOrUpdata]
        public bool IsCustomSharpnessEnabled { get; set; }

        /// <summary>
        /// 是否自定义伽马值
        /// </summary>
        [Column("IsCustomGammaEnabled"), Required, InsertOrUpdata]
        public bool IsCustomGammaEnabled { get; set; }

        /// <summary>
        /// 是否自定义白平衡
        /// </summary>
        [Column("IsCustomWhiteBalanceEnabled"), Required, InsertOrUpdata]
        public bool IsCustomWhiteBalanceEnabled { get; set; }

        /// <summary>
        /// 是否自定义背光补偿
        /// </summary>
        [Column("IsCustomBacklightCompensationEnabled"), Required, InsertOrUpdata]
        public bool IsCustomBacklightCompensationEnabled { get; set; }

        /// <summary>
        /// 是否自定义增益
        /// </summary>
        [Column("IsCustomGainEnabled"), Required, InsertOrUpdata]
        public bool IsCustomGainEnabled { get; set; }

        /// <summary>
        /// 是否自定义变焦
        /// </summary>
        [Column("IsCustomZoomEnabled"), Required, InsertOrUpdata]
        public bool IsCustomZoomEnabled { get; set; }

        /// <summary>
        /// 是否自定义对焦
        /// </summary>
        [Column("IsCustomFocusEnabled"), Required, InsertOrUpdata]
        public bool IsCustomFocusEnabled { get; set; }

        /// <summary>
        /// 是否自定义光圈
        /// </summary>
        [Column("IsCustomApertureEnabled"), Required, InsertOrUpdata]
        public bool IsCustomApertureEnabled { get; set; }

        /// <summary>
        /// 是否自定义水平旋转
        /// </summary>
        [Column("IsCustomHorizontalRotationEnabled"), Required, InsertOrUpdata]
        public bool IsCustomHorizontalRotationEnabled { get; set; }

        /// <summary>
        /// 是否自定义垂直旋转
        /// </summary>
        [Column("IsCustomVerticalRotationEnabled"), Required, InsertOrUpdata]
        public bool IsCustomVerticalRotationEnabled { get; set; }

        /// <summary>
        /// 是否自定义翻转
        /// </summary>
        [Column("IsCustomFlipEnabled"), Required, InsertOrUpdata]
        public bool IsCustomFlipEnabled { get; set; }
    }
}