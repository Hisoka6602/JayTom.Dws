using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using JayTom.Dws.Domain.Service.VideoApi;

namespace JayTom.Dws.Infrastructure.SignalR.VideoApi.SignalRMessageHub {

    public class MessageHub : Hub, IMessageHub {
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly IVideoBarCodeService _videoBarCodeService;
        private static readonly SemaphoreSlim DataStatisticsLock = new(1, 1);

        public MessageHub(IHubContext<MessageHub> hubContext,
            IVideoBarCodeService videoBarCodeService) {
            _hubContext = hubContext;
            _videoBarCodeService = videoBarCodeService;
        }

        public async Task DataStatistics() {
            var lockTaken = false;
            try {
                lockTaken = await DataStatisticsLock.WaitAsync(0);
                if (!lockTaken) {
                    return;
                }

                var dataStatistics = new DataStatistics();
                var (key, value) = await _videoBarCodeService.BarcodeTotalForDate(DateTime.Today);
                if (key && value is int todayBarcodeTotal) {
                    dataStatistics.TodayBarcodeTotal = todayBarcodeTotal;
                }
                (key, value) = await _videoBarCodeService.BarcodeTotalForDate(DateTime.Today.AddDays(-1));
                if (key && value is int yesterdayBarcodeTotal) {
                    dataStatistics.YesterdayBarcodeTotal = yesterdayBarcodeTotal;
                }

                await _hubContext.Clients.All.SendCoreAsync("DataStatistics",
                [
                    dataStatistics
                ]);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                if (lockTaken) {
                    DataStatisticsLock.Release();
                }
            }
        }

        public async Task MessageItem(MessageBarCodeItemInfo info) {
            try {
                await _hubContext.Clients.All.SendCoreAsync("MessageItem",
                [
                    info
                ]);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
        }

        /// <summary>
        /// 向所有客户端广播条码更新。
        /// </summary>
        public async Task UpDateItem(MessageBarCodeItemInfo info) {
            try {
                await _hubContext.Clients.All.SendCoreAsync("UpDateItem",
                [
                    info
                ]);
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
        }

        public async Task UpDateNodes() {
            try {
                var (key, value) = await _videoBarCodeService.GroupedNodeNames();
                if (key && value is List<string> nodeNames) {
                    await _hubContext.Clients.All.SendCoreAsync("NodeNames",
                    [
                        nodeNames
                    ]);
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
        }
    }
}
