using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Services.CacheCleanup {

    public interface ICacheCleanupService {

        /// <summary>
        /// 删除指定天数之前的条码数据。
        /// </summary>
        /// <param name="days">指定的天数。</param>
        Task<KeyValuePair<bool, string>> DeleteBarcodeDataOlderThanDays(int days);

        /// <summary>
        /// 删除指定天数之前的扫码图片。
        /// </summary>
        /// <param name="days">指定的天数。</param>
        Task<KeyValuePair<bool, string>> DeleteScanImagesOlderThanDays(int days);

        /// <summary>
        /// 删除指定天数之前的全景图片。
        /// </summary>
        /// <param name="days">指定的天数。</param>
        Task<KeyValuePair<bool, string>> DeletePanoramaImagesOlderThanDays(int days);

        /// <summary>
        /// 删除指定天数之前的FTP图片。
        /// </summary>
        /// <param name="days">指定的天数。</param>
        Task<KeyValuePair<bool, string>> DeleteFtpImagesOlderThanDays(int days);

        /// <summary>
        /// 删除指定天数之前的日志数据。
        /// </summary>
        /// <param name="days">指定的天数。</param>
        Task<KeyValuePair<bool, string>> DeleteLogDataOlderThanDays(int days);

        /// <summary>
        /// 删除最早一天的条码数据。
        /// </summary>
        Task<KeyValuePair<bool, string>> DeleteEarliestBarcodeData();

        /// <summary>
        /// 删除最早一天的扫码图片。
        /// </summary>
        Task<KeyValuePair<bool, string>> DeleteEarliestScanImages();

        /// <summary>
        /// 删除最早一天的全景图片。
        /// </summary>
        Task<KeyValuePair<bool, string>> DeleteEarliestPanoramaImages();

        /// <summary>
        /// 删除最早一天的日志数据。
        /// </summary>
        Task<KeyValuePair<bool, string>> DeleteEarliestLogData();
    }
}