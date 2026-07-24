using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.CloudApi;
using JayTom.Dws.Domain.Repository.VideoApi;

namespace JayTom.Dws.Infrastructure.Repository.VideoApi {

    public class VideoPackageRepository : RepositoryBase<PackageInfoModel>, IVideoPackageRepository {

        public VideoPackageRepository(IDbContextFactory<VideoApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, List<PackageInfoModel>>> SelectPackageOrderByDescending<TOrder>(Expression<Func<PackageInfoModel, bool>> where, Expression<Func<PackageInfoModel, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
                var barCodeInfoModels = await dbSet.AsNoTracking()
                    .OrderByDescending(o => o.PackageCreateTime)
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.DeviceInfo)
                    .Include(b => b.NvrInfos)
                    .Where(where)
                    .OrderByDescending(order)
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken: token);
                return new KeyValuePair<bool, List<PackageInfoModel>>(true, barCodeInfoModels);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
            }
        }

        public async Task<KeyValuePair<bool, List<PackageInfoModel>>> SelectPackage<TOrder>(Expression<Func<PackageInfoModel, bool>> where, Expression<Func<PackageInfoModel, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
                var barCodeInfoModels = await dbSet.AsNoTracking()
                    .OrderBy(o => o.PackageCreateTime)
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.DeviceInfo)
                    .Include(b => b.NvrInfos)
                    .Where(where)
                    .OrderBy(order)
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken: token);
                return new KeyValuePair<bool, List<PackageInfoModel>>(true, barCodeInfoModels);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
            }
        }

        public async Task<KeyValuePair<bool, PackageInfoModel>> FirstOrDefaultInfo(Expression<Func<PackageInfoModel, bool>> where, CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, PackageInfoModel>(false, new PackageInfoModel());
                var barCodeInfoModels = await dbSet.AsNoTracking()
                    .OrderByDescending(o => o.PackageCreateTime)
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.DeviceInfo)
                    .Include(b => b.NvrInfos)
                    .FirstOrDefaultAsync(where, cancellationToken: token);
                return new KeyValuePair<bool, PackageInfoModel>(true, barCodeInfoModels);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, PackageInfoModel>(false, new PackageInfoModel());
            }
        }

        public async Task<KeyValuePair<bool, List<string>>> SelectNodeInfos(CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<DeviceInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, List<string>>(false, new List<string>());
                var listAsync = await dbSet.AsNoTracking()
                    .GroupBy(g => g.NodeName)
                    .Select(s => s.Key)
                    .ToListAsync(cancellationToken: token);

                return new KeyValuePair<bool, List<string>>(true, listAsync);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, List<string>>(false, new List<string>());
            }
        }

        public async Task<KeyValuePair<bool, List<NvrInfoModel>>> SelectNvrInfos(CancellationToken token = default) {
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<NvrInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, List<NvrInfoModel>>(false, new List<NvrInfoModel>());
                var nvrInfoModels = await dbSet.AsNoTracking()
                    .GroupBy(g => new { g.IpAddress, g.Port, g.Channel })
                    .Select(s => new NvrInfoModel {
                        IpAddress = s.Key.IpAddress,
                        Port = s.Key.Port,
                        Channel = s.Key.Channel,
                    })
                    .ToListAsync(cancellationToken: token);

                return new KeyValuePair<bool, List<NvrInfoModel>>(true, nvrInfoModels);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, List<NvrInfoModel>>(false, new List<NvrInfoModel>());
            }
        }

        public new async Task<int> Total(Expression<Func<PackageInfoModel, bool>> @where,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return 0;
                return await dbSet.AsNoTracking()
                    .OrderByDescending(o => o.PackageCreateTime)
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.DeviceInfo)
                    .Include(b => b.NvrInfos)
                    .Where(where)
                    .CountAsync(cancellationToken: token);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return 0;
            }
        }
    }
}
