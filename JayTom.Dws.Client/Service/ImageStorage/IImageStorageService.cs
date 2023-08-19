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
        /// 存图失败事件
        /// </summary>
        event EventHandler<Exception> ImageSaveFailed;

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
}