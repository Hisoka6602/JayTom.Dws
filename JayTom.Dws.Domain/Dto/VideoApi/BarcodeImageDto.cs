using JayTom.Dws.Abstractions.Imaging;

namespace JayTom.Dws.Domain.Dto.VideoApi {

    public class BarcodeImageDto {

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 相机名称
        /// </summary>
        public string CameraName { get; set; } = string.Empty;

        /// <summary>
        /// 图像
        /// </summary>
        public ImageHandle? Image { get; set; }
    }
}
