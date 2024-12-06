using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Domain.Repository.LocalData {

    public interface INodeRepository : IRepository<NodeInfoModel> {

        /// <summary>
        /// 插入
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        new Task<bool> Insert([NotNull] NodeInfoModel entity, CancellationToken token = default);

        /// <summary>
        /// 获取缓存数据
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="nodeIndex"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<NodeInfoModel?> GetMemoryCacheNodeInfo(long packageId, int nodeIndex, CancellationToken token = default);
    }
}