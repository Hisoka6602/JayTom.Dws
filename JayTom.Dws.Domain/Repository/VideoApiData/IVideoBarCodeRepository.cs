using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Data.VideoApiData;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace JayTom.Dws.Domain.Repository.VideoApiData {

    public interface IVideoBarCodeRepository : IRepository<VideoBarCodeInfoModel> {

        /// <summary>
        /// 插入
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public new Task<bool> Insert(VideoBarCodeInfoModel entity, CancellationToken token = default);

        /// <summary>
        /// 获取条数
        /// </summary>
        /// <param name="barCode"></param>
        /// <param name="nodeStartDateTime"></param>
        /// <param name="nodeEndDateTime"></param>
        /// <param name="nodeName"></param>
        /// <param name="cameraSerialNumber"></param>
        /// <param name="cameraName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, int>> GetBarcodeTotal(string barCode, DateTime? nodeStartDateTime,
            DateTime? nodeEndDateTime, string? nodeName,
            string? cameraSerialNumber, string? cameraName, CancellationToken token = default);

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="barCode"></param>
        /// <param name="nodeStartDateTime"></param>
        /// <param name="nodeEndDateTime"></param>
        /// <param name="nodeName"></param>
        /// <param name="cameraSerialNumber"></param>
        /// <param name="cameraName"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, object>> GetBarcodeInfos(string barCode, DateTime? nodeStartDateTime,
            DateTime? nodeEndDateTime, string? nodeName,
            string? cameraSerialNumber, string? cameraName, int pageIndex = 0, int pageSize = 1000,
            CancellationToken token = default);
    }
}
