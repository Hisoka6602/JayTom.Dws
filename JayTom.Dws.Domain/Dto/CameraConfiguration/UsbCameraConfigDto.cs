using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.CameraConfiguration {

    public class UsbCameraConfigDto {

        /// <summary>
        /// 曝光度
        /// </summary>
        public int Exposure { get; set; }

        /// <summary>
        /// 分辨率
        /// </summary>
        public Size Resolution { get; set; }

        /// <summary>
        /// 亮度
        /// </summary>
        public int Brightness { get; set; }

        /// <summary>
        /// 对比度
        /// </summary>
        public int Contrast { get; set; }

        /// <summary>
        /// 色调
        /// </summary>
        public int Hue { get; set; }

        /// <summary>
        /// 饱和度
        /// </summary>
        public int Saturation { get; set; }

        /// <summary>
        /// 锐度
        /// </summary>
        public int Sharpness { get; set; }

        /// <summary>
        /// 伽马值
        /// </summary>
        public int Gamma { get; set; }

        /// <summary>
        /// 白平衡
        /// </summary>
        public int WhiteBalance { get; set; }

        /// <summary>
        /// 背光补偿
        /// </summary>
        public int BklightComp { get; set; }

        /// <summary>
        /// 增益
        /// </summary>
        public int Gain { get; set; }

        /// <summary>
        /// 变焦
        /// </summary>
        public int Zoom { get; set; }

        /// <summary>
        /// 对焦
        /// </summary>
        public int Focus { get; set; }

        /// <summary>
        /// 光圈
        /// </summary>
        public int Iris { get; set; }

        /// <summary>
        /// 水平旋转
        /// </summary>
        public int Pan { get; set; }

        /// <summary>
        /// 垂直旋转
        /// </summary>
        public int Tilt { get; set; }

        /// <summary>
        /// 翻转
        /// </summary>
        public int Roll { get; set; }

        /// <summary>
        /// 是否自定义曝光度
        /// </summary>
        public bool IsCustomExposureEnabled { get; set; }

        /// <summary>
        /// 是否自定义亮度
        /// </summary>
        public bool IsCustomBrightnessEnabled { get; set; }

        /// <summary>
        /// 是否自定义对比度
        /// </summary>
        public bool IsCustomContrastEnabled { get; set; }

        /// <summary>
        /// 是否自定义色调
        /// </summary>
        public bool IsCustomHueEnabled { get; set; }

        /// <summary>
        /// 是否自定义饱和度
        /// </summary>
        public bool IsCustomSaturationEnabled { get; set; }

        /// <summary>
        /// 是否自定义锐度
        /// </summary>
        public bool IsCustomSharpnessEnabled { get; set; }

        /// <summary>
        /// 是否自定义伽马值
        /// </summary>
        public bool IsCustomGammaEnabled { get; set; }

        /// <summary>
        /// 是否自定义白平衡
        /// </summary>
        public bool IsCustomWhiteBalanceEnabled { get; set; }

        /// <summary>
        /// 是否自定义背光补偿
        /// </summary>
        public bool IsCustomBacklightCompensationEnabled { get; set; }

        /// <summary>
        /// 是否自定义增益
        /// </summary>
        public bool IsCustomGainEnabled { get; set; }

        /// <summary>
        /// 是否自定义变焦
        /// </summary>
        public bool IsCustomZoomEnabled { get; set; }

        /// <summary>
        /// 是否自定义对焦
        /// </summary>
        public bool IsCustomFocusEnabled { get; set; }

        /// <summary>
        /// 是否自定义光圈
        /// </summary>
        public bool IsCustomApertureEnabled { get; set; }

        /// <summary>
        /// 是否自定义水平旋转
        /// </summary>

        public bool IsCustomHorizontalRotationEnabled { get; set; }

        /// <summary>
        /// 是否自定义垂直旋转
        /// </summary>
        public bool IsCustomVerticalRotationEnabled { get; set; }

        /// <summary>
        /// 是否自定义翻转
        /// </summary>
        public bool IsCustomFlipEnabled { get; set; }
    }
}