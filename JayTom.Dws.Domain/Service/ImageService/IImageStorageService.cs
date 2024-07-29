using System.Drawing;
using JayTom.Dws.Domain.Dto;

namespace JayTom.Dws.Domain.Service.ImageService {

    public interface IImageStorageService {

        /// <summary>
        /// 存图参数
        /// </summary>
        public ImageSettingsDto? ImageSettingsDto { get; }

        /// <summary>
        /// 存图失败事件
        /// </summary>
        event EventHandler<Exception> ImageSaveFailed;

        /// <summary>
        /// 存图完成事件
        /// </summary>
        event EventHandler<ImageSavedEventArgs> ImageSaved;

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <param name="image"></param>
        /// <param name="type"></param>
        /// <param name="barCode"></param>
        /// <param name="weight"></param>
        /// <param name="scanTime"></param>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="volume"></param>
        /// <param name="cameraSerialNumber"></param>
        /// <param name="cancellationToken"></param>
        Task SaveImage(Image image, SaveImageType type, string barCode, float weight,
           DateTime scanTime, float length, float width, float height, float volume,
           string cameraSerialNumber, CancellationToken cancellationToken = default);

        Task SaveImage(long packageTimestamped, Image image, SaveImageType type, string barCode, float weight,
            DateTime scanTime, float length, float width, float height, float volume,
            string cameraSerialNumber, CancellationToken cancellationToken = default);
    }

    public class ImageSavedEventArgs : EventArgs {
        public long PackageTimestamp { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string? CameraSerialNumber { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string? BarCode { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 图片类型
        /// </summary>
        public SaveImageType? ImageType { get; set; }

        /// <summary>
        /// 存图的时间
        /// </summary>
        public DateTime SaveDateTime { get; set; }

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }
    }
}