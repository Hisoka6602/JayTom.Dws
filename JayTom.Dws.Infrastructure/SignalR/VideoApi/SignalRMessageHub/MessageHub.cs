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
        private static SemaphoreSlim _semaphoreSlim = new(1);

        public MessageHub(IHubContext<MessageHub> hubContext,
            IVideoBarCodeService videoBarCodeService) {
            _hubContext = hubContext;
            _videoBarCodeService = videoBarCodeService;
        }

        public async void DataStatistics() {
            try {
                var dataStatistics = new DataStatistics();
                await _semaphoreSlim.WaitAsync();
                //获取计数
                var (key, value) = await _videoBarCodeService.BarcodeTotalForDateBetween(
                    new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                    new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
                        .AddMonths(1).AddSeconds(-1));
                if (key && value is int thisMonthBarcodeTotal) {
                    dataStatistics.ThisMonthBarcodeTotal = thisMonthBarcodeTotal;
                }
                (key, value) = await _videoBarCodeService.BarcodeTotalForDateBetween(
                    new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1),
                    new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddSeconds(-1));
                if (key && value is int lastMonthBarcodeTotal) {
                    dataStatistics.LastMonthBarcodeTotal = lastMonthBarcodeTotal;
                }

                (key, value) = await _videoBarCodeService.BarcodeTotalForDate(DateTime.Today);
                if (key && value is int todayBarcodeTotal) {
                    dataStatistics.TodayBarcodeTotal = todayBarcodeTotal;
                }
                (key, value) = await _videoBarCodeService.BarcodeTotalForDate(DateTime.Today.AddDays(-1));
                if (key && value is int yesterdayBarcodeTotal) {
                    dataStatistics.YesterdayBarcodeTotal = yesterdayBarcodeTotal;
                }

                await _hubContext.Clients.All.SendCoreAsync("DataStatistics", new object?[]
                {
                    dataStatistics
                });
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                _semaphoreSlim.Release();
            }
        }

        public async void MessageItem(MessageBarCodeItemInfo info) {
            try {
                await _semaphoreSlim.WaitAsync();
                //获取计数
                await _hubContext.Clients.All.SendCoreAsync("MessageItem", new object?[]
                {
                    info
                });
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                _semaphoreSlim.Release();
            }
        }

        public async void UpDateItem(MessageBarCodeItemInfo info) {
            try {
                await _semaphoreSlim.WaitAsync();
                //获取计数
                await _hubContext.Clients.All.SendCoreAsync("UpDateItem", new object?[]
                {
                    info
                });
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                _semaphoreSlim.Release();
            }
        }

        public async void UpDateNodes() {
            try {
                await _semaphoreSlim.WaitAsync();
                var (key, value) = await _videoBarCodeService.GroupedNodeNames();
                if (key && value is List<string> nodeNames) {
                    //获取计数
                    await _hubContext.Clients.All.SendCoreAsync("NodeNames", new object?[]
                    {
                        nodeNames
                    });
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                _semaphoreSlim.Release();
            }
        }
    }
}