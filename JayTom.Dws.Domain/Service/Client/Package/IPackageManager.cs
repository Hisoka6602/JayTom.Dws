using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Service.Client.Package {

    public interface IPackageManager {

        /// <summary>
        /// 创建包裹完成事件，当成功创建包裹时触发。
        /// </summary>
        event EventHandler<PackageInfoModel> PackageCreated;

        /// <summary>
        /// 拦截创建包裹事件，当创建包裹被拦截时触发。
        /// </summary>
        event EventHandler<PackageInfoModel> PackageIntercepted;

        /// <summary>
        /// 移除包裹事件，当包裹被移除时触发。
        /// </summary>
        event EventHandler<PackageInfoModel> PackageRemoved;

        /// <summary>
        /// 清空包裹事件，当所有包裹被清空时触发。
        /// </summary>
        event EventHandler PackagesCleared;

        /// <summary>
        /// 更新包裹事件，当包裹信息被更新时触发。
        /// </summary>
        event EventHandler<PackageInfoModel> PackageUpdated;

        /// <summary>
        /// 追加包裹信息事件，当包裹信息被追加时触发。
        /// </summary>
        event EventHandler<PackageInfoModel> PackageAppended;

        /// <summary>
        /// 创建包裹方法，用于向包裹管理器添加新的包裹。
        /// </summary>
        Task<bool> CreatePackage(PackageCreationMethodsEnum packageCreationMethodsEnum, PackageInfoModel packageInfo);

        /// <summary>
        /// 移除包裹方法，用于从包裹管理器中移除指定追踪号的包裹。
        /// </summary>
        Task<bool> RemovePackage(PackageInfoModel packageInfo);

        /// <summary>
        /// 移除包裹方法，用于从包裹管理器中移除指定追踪号的包裹。
        /// </summary>
        Task<bool> RemovePackage(long packageTimestamped);

        /// <summary>
        /// 清空包裹方法，用于清空包裹管理器中的所有包裹。
        /// </summary>
        Task<bool> ClearPackages();

        /// <summary>
        /// 更新包裹方法，用于更新指定追踪号的包裹信息。
        /// </summary>
        Task<bool> UpdatePackage(Expression<Func<PackageInfoModel, bool>> @where, BasePackageForeignKeyInfoModel info);

        /// <summary>
        /// 追加包裹信息方法，用于向指定追踪号的包裹添加额外信息。
        /// </summary>
        Task<bool> AppendPackageInfo(Expression<Func<PackageInfoModel, bool>> @where, BasePackageForeignKeyInfoModel info);

        /// <summary>
        /// 查询包裹方法，用于根据追踪号查询指定的包裹。
        /// </summary>
        Task<PackageInfoModel>? FindPackage(Expression<Func<PackageInfoModel, bool>> @where, CancellationToken token);

        //BasePackageForeignKeyInfoModel
    }
}