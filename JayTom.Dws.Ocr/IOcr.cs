using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Ocr {

    public interface IOcr : IDisposable {

        /// <summary>
        /// 验证授权
        /// </summary>
        /// <returns></returns>
        bool ValidateAuthorization();

        /// <summary>
        /// 本地识别
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        string RecognizeLocal(string imagePath);

        /// <summary>
        /// 在线识别
        /// </summary>
        /// <param name="imageUrl"></param>
        /// <returns></returns>
        string RecognizeOnline(string imageUrl);

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void SetParameter(string key, object value);

        /// <summary>
        /// 初始化
        /// </summary>
        void Initialize();
    }
}