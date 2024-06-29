using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using JayTom.Dws.Domain.Dto;
using System.ComponentModel;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;
using JayTom.Dws.Client.Service.BackgroundService;
using PackageInfo = JayTom.Dws.Domain.Manager.PackageInfo;
using PackageRemoveTimer = JayTom.Dws.Domain.Manager.PackageRemoveTimer;
using PackageRemovedEventArgs = JayTom.Dws.Domain.Manager.PackageRemovedEventArgs;
using PackageCompletedEventArgs = JayTom.Dws.Domain.Manager.PackageCompletedEventArgs;

namespace JayTom.Dws.Client.Service.Manager {

    public static class PackageInfoManager {
        private static ConcurrentDictionary<DateTime, PackageInfo> _packageInfos = new();

        /// <summary>
        /// 包裹移除
        /// </summary>
        public static event EventHandler<PackageRemovedEventArgs>? PackageRemoved;

        /// <summary>
        /// 包裹完成
        /// </summary>
        public static event EventHandler<PackageCompletedEventArgs>? PackageCompleted;

        public static void OnPackageRemoved(PackageRemovedEventArgs e) {
            PackageRemoved?.Invoke(null, e);
        }

        public static void OnPackageCompleted(PackageCompletedEventArgs e) {
            PackageCompleted?.Invoke(null, e);
        }

        //public static ConcurrentDictionary<DateTime, PackageInfo> GetPackageInfos() => _packageInfos;

        /// <summary>
        /// 添加包裹
        /// </summary>
        /// <param name="package"></param>
        /// <param name="removeTimers"></param>
        public static void AddPackage(PackageInfo package, List<PackageRemoveTimer> removeTimers) {
            //_packageInfos[package.CreateTime] = package;
            _packageInfos.TryAdd(package.CreateTime, package);
            package.StartRemovalTimers(_packageInfos, removeTimers);
        }

        /// <summary>
        /// 移除包裹
        /// </summary>
        /// <param name="createTime"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static bool RemovePackage(DateTime createTime, string description = "手动移除") {
            var tryRemove = _packageInfos.TryRemove(createTime, out var info);
            if (tryRemove && info is not null) {
                OnPackageRemoved(new PackageRemovedEventArgs(info, description));
            }

            return tryRemove;
        }

        /// <summary>
        /// 移除包裹
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static bool RemovePackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate, string description = "手动移除") {
            var (key, value) = _packageInfos.FirstOrDefault(predicate);
            if (value is not null) {
                var tryRemove = _packageInfos.TryRemove(key, out var info);
                if (tryRemove && info is not null) {
                    OnPackageRemoved(new PackageRemovedEventArgs(info, description));
                }
                return tryRemove;
            }

            return false;
        }

        /// <summary>
        /// 包裹是否存在
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static bool PackageExists(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            return _packageInfos.Any(predicate);
        }

        /// <summary>
        /// 获取包裹
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static PackageInfo? GetPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var (key, value) = _packageInfos.OrderBy(o => o.Key).FirstOrDefault(predicate);
            if (value is null) return null;
            _packageInfos.TryGetValue(key, out var package);
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds >= 500) {
                NLog.LogManager.GetCurrentClassLogger().Error($"GetPackage耗时超500ms,耗时{stopwatch.ElapsedMilliseconds}");
            }
            return package;
        }

        /// <summary>
        /// 获取最后一个
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static PackageInfo? GetLastPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var (key, value) = _packageInfos.OrderBy(o => o.Key).LastOrDefault(predicate);
            if (value is null) return null;
            _packageInfos.TryGetValue(key, out var package);
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds >= 500) {
                NLog.LogManager.GetCurrentClassLogger().Error($"GetLastPackage耗时超500ms,耗时{stopwatch.ElapsedMilliseconds}");
            }
            return package;
        }

        /// <summary>
        /// 获取包裹
        /// </summary>
        /// <param name="createTime"></param>
        /// <returns></returns>
        public static PackageInfo? GetPackage(DateTime createTime) {
            _packageInfos.TryGetValue(createTime, out var package);
            return package;
        }

        /// <summary>
        /// 获取包裹
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static List<PackageInfo>? GetPackages(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            return _packageInfos?.Any(predicate) == true ?
                _packageInfos.Where(predicate)?.
                    OrderBy(o => o.Key)?.
                    Select(s => s.Value)
                    ?.ToList()
                : new List<PackageInfo>();
        }

        /// <summary>
        /// 获取包裹数量
        /// </summary>
        /// <returns></returns>
        public static int GetPackageCount() {
            return _packageInfos.Count;
        }

        /// <summary>
        /// 清空包裹
        /// </summary>
        public static void ClearAllPackages() {
            _packageInfos.Clear();
        }

        /// <summary>
        /// 完成包裹
        /// </summary>
        /// <param name="predicate"></param>

        public static void CompletedPackage(Func<KeyValuePair<DateTime, PackageInfo>, bool> predicate) {
            var packageInfo = GetPackage(predicate);
            if (packageInfo == null) return;
            packageInfo.VolumeInfo ??= new VolumeInfoModel();
            packageInfo.WeightInfo ??= new WeightInfoModel();
            packageInfo.BarCodeInfo ??= new BarCodeInfoModel();
            packageInfo.IsStackedPackage ??= false;
            packageInfo.IsCompleted = true;
            OnPackageCompleted(new PackageCompletedEventArgs(packageInfo, string.Empty));
        }
    }
}