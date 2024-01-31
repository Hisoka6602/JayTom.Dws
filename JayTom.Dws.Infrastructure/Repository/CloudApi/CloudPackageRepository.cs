using System;
using System.Linq;
using System.Text;
using RTools_NTS.Util;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Domain.Dto.CloudApiDto;
using Microsoft.Extensions.Caching.Memory;
using JayTom.Dws.Domain.Repository.CloudApi;

namespace JayTom.Dws.Infrastructure.Repository.CloudApi {

    public class CloudPackageRepository : RepositoryBase<PackageInfoModel>, ICloudPackageRepository {

        public CloudPackageRepository(IDbContextFactory<CloudApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, List<PackageInfoModel>>> SelectPackageOrderByDescending<TOrder>(Expression<Func<PackageInfoModel, bool>> where, Expression<Func<PackageInfoModel, TOrder>> order, int pageIndex, int pageSize,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, List<PackageInfoModel>>(false, new List<PackageInfoModel>());
                var barCodeInfoModels = await dbSet.AsNoTracking()
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.ExitInfo)
                    .Include(b => b.SortingInfo)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.CloudVideoUploadInfo)
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
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.ExitInfo)
                    .Include(b => b.SortingInfo)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.CloudVideoUploadInfo)
                    .Where(where)
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
                    .Where(where)
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.ExitInfo)
                    .Include(b => b.SortingInfo)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.CloudVideoUploadInfo)
                    .FirstOrDefaultAsync(cancellationToken: token);
                return new KeyValuePair<bool, PackageInfoModel>(true, barCodeInfoModels);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, PackageInfoModel>(false, new PackageInfoModel());
            }
        }

        public new async Task<int> Total([NotNull] Expression<Func<PackageInfoModel, bool>> @where,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return 0;
                return await dbSet.AsNoTracking()
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.ExitInfo)
                    .Include(b => b.SortingInfo)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.CloudVideoUploadInfo)
                    .Where(where)
                    .CountAsync(cancellationToken: token);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return 0;
            }
        }

        public async Task<KeyValuePair<bool, object>> GetStatistics(DateTime? startDateTime, DateTime? endDateTime, string? deviceName,
            CancellationToken cancellationToken) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return new KeyValuePair<bool, object>(false, "查询失败");
                var queryable = dbSet.AsNoTracking()
                    .Include(b => b.BarCodeInfo)
                    .Include(b => b.WeightInfo)
                    .Include(b => b.VolumeInfo)
                    .Include(b => b.UploadInfo)
                    .Include(b => b.ExitInfo)
                    .Include(b => b.SortingInfo)
                    .Include(b => b.LogisticsInfo)
                    .Include(b => b.OcrInfo)
                    .ThenInclude(c => c.OcrDetailedInfos)
                    .Include(b => b.ImageInfos)
                    .Include(b => b.CloudVideoUploadInfo)
                    .Where(w => w.BarCodeInfo != null &&
                                (startDateTime == null ||
                                 w.BarCodeInfo.ScanTime >= startDateTime) &&
                                (endDateTime == null ||
                                 w.BarCodeInfo.ScanTime <= endDateTime) &&
                                (string.IsNullOrEmpty(deviceName) || (w.DeviceInfo != null &&
                                                                      w.DeviceInfo.DeviceName.Contains(deviceName)))
                    );
                //总数
                var totalPackages = await queryable.CountAsync(cancellationToken: cancellationToken);
                if (totalPackages > 0) {
                    //正常分拣
                    var normalSortingCount = await queryable.Where(w =>
                            w.SortingInfo != null && w.SortingInfo.IsSortingUsed && !w.SortingInfo.IsAbnormalSorting)
                        .CountAsync(cancellationToken: cancellationToken);
                    //异常分拣
                    var abnormalSortingCount = await queryable.Where(w =>
                             w.SortingInfo != null && w.SortingInfo.IsSortingUsed && w.SortingInfo.IsAbnormalSorting)
                         .CountAsync(cancellationToken: cancellationToken);
                    //平均重量
                    var averageWeight = await queryable.Where(w =>
                            w.WeightInfo != null && w.WeightInfo.FormattedWeight >= 0)
                        .AverageAsync(a => a.WeightInfo.FormattedWeight,
                            cancellationToken: cancellationToken);
                    //识别数
                    var recognitionCount = await queryable.Where(w =>
                             w.BarCodeInfo != null && !w.BarCodeInfo.Barcode.ToLower().Equals("noread"))
                         .CountAsync(cancellationToken: cancellationToken);
                    //小时数
                    var hour = await queryable.Where(w => w.BarCodeInfo != null).GroupBy(g => g.BarCodeInfo.ScanTime.Hour)
                         .CountAsync(cancellationToken: cancellationToken);

                    //格口组
                    var statisticsDtos = await queryable.Where(w => w.ExitInfo != null)
                        .GroupBy(g => g.ExitInfo.PhysicalExit)
                        .Select(s => new StatisticsDto {
                            Name = s.Key,
                            Quantity = s.Count(),
                            Percentage = Math.Round((double)s.Count() / totalPackages, 3),
                            TotalCount = totalPackages
                        }
                        )?.ToListAsync(cancellationToken: cancellationToken);

                    //网络超时
                    var timeoutCount = await queryable.Where(w => w.UploadInfo != null
                                                             && w.UploadInfo.ApiExceptionType == ApiExceptionType.Timeout)
                        .CountAsync(cancellationToken: cancellationToken);
                    //无条码
                    var noReadCount = await queryable.Where(w => w.BarCodeInfo != null
                                                            && (w.BarCodeInfo.Barcode.ToLower().Equals("noread") ||
                                                                string.IsNullOrEmpty(w.BarCodeInfo.Barcode)))
                        .CountAsync(cancellationToken: cancellationToken);

                    //无物理格口
                    var physicalExitCount = await queryable.Where(w => w.ExitInfo != null &&
                                                                  string.IsNullOrEmpty(w.ExitInfo.PhysicalExit))
                        .CountAsync(cancellationToken: cancellationToken);

                    var dtos = new List<StatisticsDto>()
                    {
                    new()
                    {
                        Name = "Timeout",
                        Quantity = timeoutCount,
                        Percentage = Math.Round((double)timeoutCount / totalPackages, 3),
                        TotalCount = totalPackages
                    },
                    new()
                    {
                        Name = "NoRead",
                        Quantity = noReadCount,
                        Percentage = Math.Round((double)noReadCount / totalPackages, 3),
                        TotalCount = totalPackages
                    },
                    new()
                    {
                        Name = "PhysicalExitEmpty",
                        Quantity = physicalExitCount,
                        Percentage = Math.Round((double)physicalExitCount / totalPackages, 3),
                        TotalCount = totalPackages
                    },
                   };
                    return new KeyValuePair<bool, object>(true, new PackageStatisticsDto {
                        TotalPackages = totalPackages,
                        NormalSortingCount = normalSortingCount,
                        AbnormalSortingCount = abnormalSortingCount,
                        AbnormalSortingRate = Math.Round((double)abnormalSortingCount / totalPackages, 3),
                        AverageWeight = Math.Round(averageWeight, 3),
                        RecognitionRate = Math.Round((double)recognitionCount / totalPackages, 3),
                        SortingEfficiency = totalPackages / hour,
                        ExitStatisticsInfo = statisticsDtos,
                        ErrorStatistics = dtos
                    });
                }
                else {
                    return new KeyValuePair<bool, object>(false, "包裹总数为0,无法获取统计数据");
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, object>(false, "查询失败");
            }
        }
    }
}