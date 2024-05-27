using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Service.Client.Interface {

    public interface IPackageApiService {

        /// <summary>
        /// 事件：后台上传完成
        /// </summary>
        event EventHandler<UploadResponse> UploadCompleted;

        /// <summary>
        /// 创建IPackageApi对象(工厂模式)
        /// </summary>
        /// <returns>IPackageApiService实例</returns>
        IPackageApi CreateInstance();

        /// <summary>
        /// 申请分拣回调
        /// </summary>
        /// <param name="info">包裹信息</param>
        /// <param name="other">其他信息</param>
        /// <param name="token">取消令牌</param>
        /// <returns>上传响应</returns>
        Task<UploadResponse> RequestSortCallbackAsync(PackageInfoModel info, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 加入后台队列上传
        /// </summary>
        /// <param name="info">包裹信息</param>
        /// <param name="other">其他信息</param>
        /// <param name="token">取消令牌</param>
        /// <returns>上传成功与否</returns>
        Task<bool> EnqueueUploadAsync(PackageInfoModel info, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 枚举当前可用接口
        /// </summary>
        /// <returns>当前可用接口的名称列表</returns>
        IEnumerable<IPackageApi> ListAvailableEndpoints();
    }

    //弃用标记 [Obsolete("This property is deprecated. Use NewProperty instead.")]
}