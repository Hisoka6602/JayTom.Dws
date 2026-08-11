using JayTom.Dws.Abstractions.Imaging;
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
        /// <remarks>调用即转移 <paramref name="image"/> 所有权；方法结束时句柄必定被释放。</remarks>
        Task SaveAndDisposeImageAsync(ImageHandle image, SaveImageType type, string barCode, decimal weight,
           DateTime scanTime, decimal length, decimal width, decimal height, decimal volume,
           string cameraSerialNumber, CancellationToken cancellationToken = default);

        /// <summary>保存包裹图片，并在完成后释放已转移所有权的图像句柄。</summary>
        /// <remarks>调用即转移 <paramref name="image"/> 所有权；方法结束时句柄必定被释放。</remarks>
        Task SaveAndDisposeImageAsync(long packageTimestamped, ImageHandle image, SaveImageType type, string barCode, decimal weight,
            DateTime scanTime, decimal length, decimal width, decimal height, decimal volume,
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
