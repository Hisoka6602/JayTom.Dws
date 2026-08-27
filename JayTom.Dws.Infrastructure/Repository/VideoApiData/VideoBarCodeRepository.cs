using NLog;
using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Models.VideoApiData;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore.Storage;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;
using JayTom.Dws.Legacy.Contracts.Repositories.VideoApiData;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JayTom.Dws.Infrastructure.Repository.VideoApiData {

    public class VideoBarCodeRepository : RepositoryBase<VideoBarCodeInfoModel, VideoApiContext>, IVideoBarCodeRepository {

        public VideoBarCodeRepository(IDbContextFactory<VideoApiContext> contextFactory, IMemoryCache cache) : base(contextFactory, cache) {
        }

        public async Task<KeyValuePair<bool, int>> GetBarcodeTotal(string barCode, DateTime? nodeStartDateTime, DateTime? nodeEndDateTime, string? nodeName,
            string? cameraSerialNumber, string? cameraName, CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<VideoScanNodeInfoModel>();
                    if (dbSet is null) return new KeyValuePair<bool, int>(false, 0);

                    var countAsync = await dbSet.AsNoTracking()
                        .Include(a => a.VideoNodeImageInfos)
                        ?.Where(w => w.BarCodeInfo != null &&
                                         (string.IsNullOrEmpty(barCode) || w.BarCodeInfo.Barcode.Contains(barCode)) &&
                                     (nodeStartDateTime == null || w.ScanTime >= nodeStartDateTime.Value) &&
                                     (nodeEndDateTime == null || w.ScanTime <= nodeEndDateTime.Value) &&
                                     (string.IsNullOrEmpty(nodeName) || w.Name.Contains(nodeName)) &&
                                     (string.IsNullOrEmpty(cameraName) || (w.VideoNodeImageInfos != null &&
                                                                           w.VideoNodeImageInfos.Any(v =>
                                                                               v.CameraName.Contains(cameraName)))) &&
                                     (string.IsNullOrEmpty(cameraSerialNumber) || (w.VideoNodeImageInfos != null &&
                                         w.VideoNodeImageInfos.Any(v =>
                                             v.CameraSerialNumber.Contains(
                                                 cameraSerialNumber)))))
                        ?.CountAsync(cancellationToken: token)!;

                    return new KeyValuePair<bool, int>(true, countAsync);
                }
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                return new KeyValuePair<bool, int>(false, 0);
            }
            return new KeyValuePair<bool, int>(false, 0);
        }

        public async Task<KeyValuePair<bool, object>> GetBarcodeInfos(string barCode, DateTime? nodeStartDateTime, DateTime? nodeEndDateTime, string? nodeName,
            string? cameraSerialNumber, string? cameraName, int pageIndex = 0, int pageSize = 1000,
            CancellationToken token = default) {
            try {
                await using (var concardContext = _contextFactory.CreateDbContext()) {
                    var dbSet = concardContext?.Set<VideoBarCodeInfoModel>();
                    if (dbSet is null) return new KeyValuePair<bool, object>(false, "dbSet为Null");

                    var videoBarCodeInfoModels = await dbSet.AsNoTracking()
                        ?.Include(a => a.VideoScanNodeInfos)
                        ?.ThenInclude(b => b.VideoNvrCameraBindingInfos)
                        ?.Include(a => a.VideoScanNodeInfos)
                        ?.ThenInclude(b => b.VideoNodeImageInfos)
                        ?.Where(w => w.VideoScanNodeInfos != null &&
                                     (string.IsNullOrEmpty(barCode) || w.Barcode.Contains(barCode)) &&
                                     (nodeStartDateTime == null || w.VideoScanNodeInfos.Any(v => v.ScanTime >= nodeStartDateTime.Value)) &&
                                     (nodeEndDateTime == null || w.VideoScanNodeInfos.Any(v => v.ScanTime <= nodeEndDateTime.Value)) &&
                                     (string.IsNullOrEmpty(nodeName) || w.VideoScanNodeInfos.Any(v => v.Name.Contains(nodeName))) &&
                                     ((string.IsNullOrEmpty(cameraName) || w.VideoScanNodeInfos.Any(v => v.VideoNodeImageInfos != null && v.VideoNodeImageInfos.Any(vni => vni.CameraName.Contains(cameraName))))) &&
                                     ((string.IsNullOrEmpty(cameraSerialNumber) || w.VideoScanNodeInfos.Any(v => v.VideoNodeImageInfos != null && v.VideoNodeImageInfos.Any(vni => vni.CameraName.Contains(cameraSerialNumber))))))
                         ?.Include(a => a.VideoScanNodeInfos)
                        ?.OrderByDescending(o => o.ScanTime)
                        ?.Skip(pageIndex * pageSize)
                        ?.Take(pageSize)
                        ?.ToListAsync(cancellationToken: token)!;

                    videoBarCodeInfoModels = videoBarCodeInfoModels.Select(s => new VideoBarCodeInfoModel {
                        Barcode = s.Barcode,
                        ScanTime = s.ScanTime,
                        TimestampedGuid = s.TimestampedGuid,
                        VideoScanNodeInfos = s.VideoScanNodeInfos?
                           .Where(w =>
                               (nodeStartDateTime == null || w.ScanTime.CompareTo(nodeStartDateTime.Value) >= 0) &&
                               (nodeEndDateTime == null || w.ScanTime.CompareTo(nodeEndDateTime.Value) <= 0) &&
                               (string.IsNullOrEmpty(nodeName) || w.Name.Contains(nodeName)))?.Select(s1 =>
                               new VideoScanNodeInfoModel() {
                                   BarCodeInfo = s,
                                   BarcodeId = s1.BarcodeId,
                                   ScanTime = s1.ScanTime,
                                   Description = s1.Description,
                                   Name = s1.Name,
                                   VideoNvrCameraBindingInfos = s1.VideoNvrCameraBindingInfos?.Select(nvr => new VideoNvrCameraBindingInfoModel {
                                       Channel = nvr.Channel,
                                       IpAddress = nvr.IpAddress,
                                       Password = nvr.Password,
                                       Port = nvr.Port,
                                       ScanNodeId = nvr.ScanNodeId,
                                       Username = nvr.Username,
                                   })?.ToList() ?? new List<VideoNvrCameraBindingInfoModel>(),
                                   VideoNodeImageInfos = s1.VideoNodeImageInfos?.Where(w1 =>
                                           (string.IsNullOrEmpty(cameraName) || w1.CameraName.Contains(cameraName)) &&
                                           (string.IsNullOrEmpty(cameraSerialNumber) ||
                                            w1.CameraSerialNumber.Contains(cameraSerialNumber)))
                                       ?.ToList() ?? new List<VideoNodeImageInfoModel>()
                               })?.ToList() ?? new List<VideoScanNodeInfoModel>(),
                    })?.ToList() ?? new List<VideoBarCodeInfoModel>();

                    return new KeyValuePair<bool, object>(true, videoBarCodeInfoModels);
                }
            }
            catch (Win32Exception) {
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
                return new KeyValuePair<bool, object>(false, e.Message);
            }
            return new KeyValuePair<bool, object>(false, "null");
        }

        public new async Task<bool> Insert(VideoBarCodeInfoModel entity, CancellationToken token = default) {
            IDbContextTransaction? contextTransaction = null;
            try {
                await using var concardContext = _contextFactory.CreateDbContext();
                var strategy = concardContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () => {
                    await using (contextTransaction = await concardContext.Database.BeginTransactionAsync(token)) {
                        if (contextTransaction is not null) {
                            var videoBarCodeInfoModels = concardContext?.Set<VideoBarCodeInfoModel>();
                            await videoBarCodeInfoModels.AddAsync(entity, token);
                            await concardContext?.SaveChangesAsync(token);
                            await contextTransaction.CommitAsync(token);

                            return true;
                        }
                    }

                    return false;
                });
            }
            catch (Win32Exception) {
                //await contextTransaction?.RollbackAsync(token)!;
            }
            catch (TaskCanceledException) {
                //await contextTransaction?.RollbackAsync(token)!;
            }
            catch (Exception e) {
                //await contextTransaction?.RollbackAsync(token)!;
                LogManager.GetCurrentClassLogger().Log(LogLevel.Error, e.ToString());
            }
            return false;
        }
    }
}
