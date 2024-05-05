using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Service.Sorting {

    public interface IExitMonitor {

        /// <summary>
        /// 锁格回调
        /// </summary>
        event EventHandler<PackageExitDefinitionInfoModel> LockExitEvent;

        /// <summary>
        /// 解除锁格回调
        /// </summary>

        event EventHandler<PackageExitDefinitionInfoModel> UnLockExitEvent;

        /// <summary>
        /// 异常事件
        /// </summary>
        event EventHandler<ExceptionEventArgs> ExceptionOccurred;

        /// <summary>
        /// 初始化
        /// </summary>
        event EventHandler<List<PackageExitDefinitionInfoModel>> Initialized;

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接事件
        /// </summary>
        event EventHandler<EventArgs> Connected;

        /// <summary>
        /// 断开连接事件
        /// </summary>
        event EventHandler<EventArgs> Disconnected;

        /// <summary>
        /// 启动监控服务
        /// </summary>
        Task<KeyValuePair<bool, string>> Start(CancellationToken token = default);

        /// <summary>
        /// 停止监控服务
        /// </summary>
        Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default);

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters);

        /// <summary>
        /// 获取所有状态
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, List<PackageExitDefinitionInfoModel>>> GetAllPackageExitStatus();

        /// <summary>
        /// 全部锁格
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> AllLockExit();

        /// <summary>
        /// 全部解锁
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> AllUnLockExit();

        /// <summary>
        /// 全部锁格
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> AllLockExit(int db, int address, int length = 1);

        /// <summary>
        /// 全部解锁
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> AllUnLockExit(int db, int address, int length = 1);
    }
}