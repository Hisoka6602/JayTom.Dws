using System;
using System.Linq;
using System.Text;
using RTools_NTS.Util;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Linq.Expressions;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using JayTom.Dws.Data.CloudApiData;
using Microsoft.EntityFrameworkCore;
using MathNet.Numerics.Distributions;
using System.Text.RegularExpressions;
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
                    .OrderByDescending(o => o.PackageCreateTime)
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
                    .OrderByDescending(o => o.PackageCreateTime)
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
                    .FirstOrDefaultAsync(where, cancellationToken: token);
                return new KeyValuePair<bool, PackageInfoModel>(true, barCodeInfoModels);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, PackageInfoModel>(false, new PackageInfoModel());
            }
        }

        public new async Task<int> Total(Expression<Func<PackageInfoModel, bool>> @where,
            CancellationToken token = default) {
            try {
                //联表
                await using var concardContext = _contextFactory.CreateDbContext();
                var dbSet = concardContext?.Set<PackageInfoModel>();
                if (dbSet is null) return 0;
                return await dbSet.CountAsync(where, cancellationToken: token);
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
                var exceptionTypeSet = concardContext?.Set<ExceptionTypeInfoModel>();
                var matchSet = concardContext?.Set<ExceptionMatchInfoModel>();
                if (dbSet is null || matchSet is null || exceptionTypeSet is null) return new KeyValuePair<bool, object>(false, "查询失败");
                var queryable = dbSet.AsNoTracking()
                    .OrderByDescending(o => o.PackageCreateTime)
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
                            w.SortingInfo != null && w.SortingInfo.AbnormalSortingType == AbnormalSortingType.None)
                        .CountAsync(cancellationToken: cancellationToken);
                    //异常分拣
                    var abnormalSortingCount = await queryable.Where(w =>
                             w.SortingInfo != null && w.SortingInfo.AbnormalSortingType != AbnormalSortingType.None)
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
                    /*var hour = await queryable.Where(w => w.BarCodeInfo != null).GroupBy(g => g.BarCodeInfo.ScanTime.Hour)
                         .CountAsync(cancellationToken: cancellationToken);*/
                    //获取间隔最短的两个数据
                    /*var shortestTimeDifferenceData = queryable
                        .Where(w => w.SortingInfo != null && w.BarCodeInfo != null)
                        .Select(w => new {
                            Data = w,
                            NextData = queryable
                                .Where(next => next.BarCodeInfo.ScanTime > w.BarCodeInfo.ScanTime)
                                .OrderBy(next => next.PackageCreateTime)
                                .FirstOrDefault()
                        })
                        .Where(pair => pair.NextData != null) // 确保有下一个数据
                        .AsEnumerable() // 切换到客户端执行
                        .OrderBy(pair => (pair.NextData.BarCodeInfo.ScanTime - pair.Data.BarCodeInfo.ScanTime).TotalMilliseconds)
                        .FirstOrDefault();
                    var shortestTimeDifference = shortestTimeDifferenceData.NextData.BarCodeInfo.ScanTime -
                                                 shortestTimeDifferenceData.Data.BarCodeInfo.ScanTime;*/

                    //取中位数时间差

                    double packages = 0;
                    var packageInfoModels = queryable
                        .Where(w => w.SortingInfo != null && w.BarCodeInfo != null)
                        .OrderBy(o => o.BarCodeInfo.ScanTime);

                    var firstOrDefaultAsync = await packageInfoModels.FirstOrDefaultAsync(cancellationToken: cancellationToken);
                    var lastOrDefaultAsync = await packageInfoModels.LastOrDefaultAsync(cancellationToken: cancellationToken);
                    if (firstOrDefaultAsync?.BarCodeInfo is not null &&
                        lastOrDefaultAsync?.BarCodeInfo is not null) {
                        var totalSeconds = firstOrDefaultAsync.BarCodeInfo.ScanTime.Subtract(lastOrDefaultAsync.BarCodeInfo.ScanTime)
                            .TotalSeconds;
                        packages = totalPackages / totalSeconds;
                    }

                    // 现在，firstTimeDifference 包含了时间间隔最短的两个数据之间的时间差

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

                    //异常分拣类型
                    /*
                    var abnormalCount = await queryable.Where(w => w.SortingInfo != null &&
                                                              w.SortingInfo.IsSortingUsed && w.SortingInfo.IsAbnormalSorting)
                        .CountAsync(cancellationToken: cancellationToken);
                        */

                    /*var errorStatistics = await queryable.Where(w => w.SortingInfo != null &&
                                                                     w.SortingInfo.IsSortingUsed &&
                                                                     w.SortingInfo.IsAbnormalSorting &&
                                                                     w.SortingInfo.AbnormalSortingType != AbnormalSortingType.None)
                        .GroupBy(g => g.SortingInfo.AbnormalSortingType)
                        .Select(s => new StatisticsDto {
                            Name = s.Key.ToString(),
                            Quantity = s.Count(),
                            Percentage = Math.Round((double)s.Count() / abnormalCount, 3),
                            TotalCount = abnormalCount,
                            OverallPercentage = Math.Round((double)s.Count() / totalPackages, 3),
                        }
                        )?.ToListAsync(cancellationToken: cancellationToken)!;*/

                    //取出全部异常数据

                    //linq逐条匹配异常
                    var exceptionTypeModels = await exceptionTypeSet.AsQueryable().ToListAsync(cancellationToken: cancellationToken);
                    var exceptionMatchInfoModels = await matchSet.AsQueryable().ToListAsync(cancellationToken: cancellationToken);

                    var infoModels = await queryable.Where(w => w.SortingInfo != null &&
                                                                w.SortingInfo.AbnormalSortingType != AbnormalSortingType.None)
                        ?.ToListAsync(cancellationToken: cancellationToken)! ?? new List<PackageInfoModel>();

                    var errorStatistics = infoModels.Select(s =>
                        ConvertExceptionType(exceptionMatchInfoModels, exceptionTypeModels,
                            s.BarCodeInfo?.Barcode ?? string.Empty,
                            s.UploadInfo))
                        ?.GroupBy(g => g.ExceptionName)
                        ?.Select(s => new StatisticsDto {
                            Name = s.Key.ToString(),
                            Quantity = s.Count(),
                            Percentage = Math.Round((double)s.Count() / infoModels.Count, 3),
                            TotalCount = infoModels.Count,
                            OverallPercentage = Math.Round((double)s.Count() / totalPackages, 3),
                        })
                        ?.ToList();

                    //获取走势数据

                    var trendDataInfos = await queryable.Where(w => w.BarCodeInfo != null)
                        .GroupBy(g => new { ScanMinute = g.BarCodeInfo.ScanTime.Minute, ScanHour = g.BarCodeInfo.ScanTime.Hour, ScanDay = g.BarCodeInfo.ScanTime.Date })
                        .Select(s => new TrendDataInfo {
                            Time = new DateTime(s.Key.ScanDay.Year, s.Key.ScanDay.Month, s.Key.ScanDay.Day, s.Key.ScanHour, s.Key.ScanMinute, 0),
                            Quantity = s.Count()
                        }).ToListAsync(cancellationToken: cancellationToken);
                    //整合走势图,如果不需要则回填trendDataInfos
                    var timeSpan = endDateTime.Value - startDateTime.Value;
                    var nodeCount = 30;
                    var unit = "";

                    // 根据时间跨度确定单位和节点数
                    if (timeSpan.TotalMinutes <= 60) {
                        nodeCount = Convert.ToInt32(Math.Ceiling(timeSpan.TotalMinutes));
                        unit = "分钟";
                    }
                    else if (timeSpan.TotalHours <= 24) {
                        nodeCount = Convert.ToInt32(Math.Ceiling(timeSpan.TotalHours));
                        unit = "小时";
                    }
                    else switch (timeSpan.TotalDays) {
                            case <= 31:
                                nodeCount = Convert.ToInt32(Math.Ceiling(timeSpan.TotalDays));
                                unit = "天数";
                                break;

                            case >= 365:
                                nodeCount = Convert.ToInt32(Math.Ceiling(timeSpan.TotalDays / 365));
                                unit = "年份";
                                break;

                            default:
                                // 计算开始和结束日期之间的完整月份差异
                                var monthsApart = 12 * (endDateTime.Value.Year - startDateTime.Value.Year) + endDateTime.Value.Month - startDateTime.Value.Month;
                                if (startDateTime.Value.AddMonths(monthsApart) < endDateTime.Value) {
                                    // 如果增加月份后的日期仍旧小于结束日期，表示有额外的日子需要被覆盖
                                    monthsApart++;
                                }
                                nodeCount = monthsApart;
                                unit = "月份";
                                break;
                        }

                    List<TrendDataInfo> result = new();
                    var currentStartTime = startDateTime.Value;

                    for (var i = 0; i < nodeCount; i++) {
                        var startTime = currentStartTime;
                        var endTime = AddTime(currentStartTime, unit);

                        if (endTime > endDateTime) {
                            endTime = endDateTime.Value;
                        }

                        var quantity = trendDataInfos.Where(t => t.Time >= startTime && t.Time < endTime).Sum(t => t.Quantity);

                        result.Add(new TrendDataInfo {
                            Time = startTime,
                            Quantity = quantity,
                            Unit = unit
                        });

                        currentStartTime = endTime;
                    }

                    return new KeyValuePair<bool, object>(true, new PackageStatisticsDto {
                        TotalPackages = totalPackages,
                        NormalSortingCount = normalSortingCount,
                        AbnormalSortingCount = abnormalSortingCount,
                        AbnormalSortingRate = Math.Round((double)abnormalSortingCount / totalPackages, 3),
                        AverageWeight = Math.Round(averageWeight, 3),
                        RecognitionRate = Math.Round((double)recognitionCount / totalPackages, 3),
                        SortingEfficiency = (int)(3600 * Math.Abs(packages)),
                        ExitStatisticsInfo = statisticsDtos,
                        ErrorStatistics = errorStatistics,
                        TrendDataItems = result
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

        private DateTime AddTime(DateTime time, string unit) {
            return unit switch {
                "分钟" => time.AddMinutes(1),
                "小时" => time.AddHours(1),
                "天数" => time.AddDays(1),
                "月份" => time.AddMonths(1),
                "年份" => time.AddYears(1),
                _ => time
            };
        }

        private ExceptionTypeInfoModel ConvertExceptionType(List<ExceptionMatchInfoModel> matchInfos,
            List<ExceptionTypeInfoModel> exceptionTypeInfos, string barcode, UploadInfoModel? uploadInfoModel) {
            foreach (var info in matchInfos) {
                var empty = string.IsNullOrEmpty(info.Keywords);
                if (info.DataSource == 0) {
                    //条码
                    var matchException = MatchException(barcode, empty ? info.CustomRegex : info.Keywords, empty);
                    if (matchException) {
                        return exceptionTypeInfos.FirstOrDefault(f =>
                            f.Id.Equals(info.ExceptionTypeId)) ?? new ExceptionTypeInfoModel() {
                                Id = 0,
                                ExceptionName = "未知"
                            };
                    }
                }
                else if (info.DataSource == 1 && !string.IsNullOrEmpty(uploadInfoModel?.RequestContent)) {
                    //提交内容
                    var matchException = MatchException(uploadInfoModel.RequestContent, empty ? info.CustomRegex : info.Keywords, empty);
                    if (matchException) {
                        return exceptionTypeInfos.FirstOrDefault(f =>
                            f.Id.Equals(info.ExceptionTypeId)) ?? new ExceptionTypeInfoModel() {
                                Id = 0,
                                ExceptionName = "未知"
                            }; ;
                    }
                }
                else if (info.DataSource == 2 && !string.IsNullOrEmpty(uploadInfoModel?.ResponseContent)) {
                    //响应内容
                    var matchException = MatchException(uploadInfoModel.ResponseContent, empty ? info.CustomRegex : info.Keywords, empty);
                    if (matchException) {
                        return exceptionTypeInfos.FirstOrDefault(f =>
                            f.Id.Equals(info.ExceptionTypeId)) ?? new ExceptionTypeInfoModel() {
                                Id = 0,
                                ExceptionName = "未知"
                            }; ;
                    }
                }
            }

            return new ExceptionTypeInfoModel() {
                Id = 0,
                ExceptionName = "未知"
            }; ;
        }

        private bool MatchException(string source, string patternOrKey, bool isRegex) {
            try {
                if (isRegex) {
                    return Regex.IsMatch(source, patternOrKey);
                }
                else {
                    return source.Contains(patternOrKey);
                }
            }
            catch (Exception e) {
                return false;
            }
        }
    }
}
