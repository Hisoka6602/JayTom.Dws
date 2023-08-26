using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Service.ImageStorage {

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
        void SaveImage(Image image, SaveImageType type, string barCode, float weight,
           DateTime scanTime, float length, float width, float height, float volume,
           string cameraSerialNumber, CancellationToken cancellationToken = default);
    }

    public class ImageSavedEventArgs : EventArgs {

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
    }
}