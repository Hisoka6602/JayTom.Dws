using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Models.VideoApiData;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Legacy.Contracts.Repositories.VideoApiData {

    public interface IVideoScanNodeRepository : IRepository<VideoScanNodeInfoModel> {

        /// <summary>
        /// 获取节点
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<KeyValuePair<bool, List<string>>> GroupedNodeNames(CancellationToken token = default);

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public new Task<bool> Update(VideoScanNodeInfoModel entity, CancellationToken token = default);

        /// <summary>
        /// 查询节点
        /// </summary>
        /// <param name="where"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<List<VideoScanNodeInfoModel>> GetScanNodeInfos(Expression<Func<VideoScanNodeInfoModel, bool>> @where,
            CancellationToken token = default);
    }
}
