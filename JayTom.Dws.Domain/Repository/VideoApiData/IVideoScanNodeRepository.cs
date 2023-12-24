using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Data.VideoApiData;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Domain.Repository.VideoApiData {

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
        public new Task<bool> Update([NotNull] VideoScanNodeInfoModel entity, CancellationToken token = default);

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