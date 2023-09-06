using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Interface;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using static JayTom.Dws.Client.Service.BackgroundService.ScanProcessBackgroundService;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// Api提交处理器
    /// </summary>
    public class SubmitApiBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDataUploader _dataUploader;
        private ConcurrentQueue<SubmitItemInfo> _submitItems = new();

        public SubmitApiBackgroundService(IDataUploader dataUploader) {
            _dataUploader = dataUploader;
            //ScanBarCodeInfo
            EventAggregator.Instance.Subscribe<ScanBarCodeInfo>(item => {
                if (item is ScanBarCodeInfo model) {
                    _submitItems.Enqueue(new SubmitItemInfo() {
                        Barcode = model.BarCode,
                        Weight = (float)(model.Weight ?? 0),
                        Length = (float)(model.Length ?? 0),
                        Width = (float)(model.Width ?? 0),
                        Height = (float)(model.Height ?? 0),
                        Volume = (float)(model.Volume ?? 0),
                        ScanTime = model.ScanTime,
                        //图片暂时不写
                    });
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                //取出
                //需要判断用户选择的接口和参数设置
                var tryDequeue = _submitItems.TryDequeue(out var info);
                if (tryDequeue && info is not null) {
                    //上传
                    var uploadResponse = await _dataUploader.UploadData(info.Barcode ?? string.Empty,
                        info.Weight, info.ScanTime,
                        info.Length, info.Width,
                        info.Height, info.Volume,
                        info.Image, info.PanoramaImage,
                        stoppingToken);
                    //临时单线程
                    EventAggregator.Instance.Publish(new ApiResponseReceived {
                        Barcode = info.Barcode,
                        ScanTime = info.ScanTime,
                        UploadResponse = uploadResponse
                    });
                }

                await Task.Delay(10, stoppingToken);
            }
        }

        public class SubmitItemInfo {

            /// <summary>
            /// 条码
            /// </summary>
            public string? Barcode { get; set; }

            /// <summary>
            /// 重量
            /// </summary>
            public float Weight { get; set; }

            /// <summary>
            /// 扫码时间
            /// </summary>
            public DateTime ScanTime { get; set; }

            /// <summary>
            /// 长度
            /// </summary>
            public float Length { get; set; }

            /// <summary>
            /// 宽度
            /// </summary>
            public float Width { get; set; }

            /// <summary>
            /// 高度
            /// </summary>
            public float Height { get; set; }

            /// <summary>
            /// 体积
            /// </summary>
            public float Volume { get; set; }

            /// <summary>
            /// 条码图片
            /// </summary>
            public Bitmap? Image { get; set; }

            /// <summary>
            /// 全景图
            /// </summary>
            public Bitmap? PanoramaImage { get; set; }
        }

        /// <summary>
        /// Api回传类
        /// </summary>
        public class ApiResponseReceived {

            /// <summary>
            /// 条码
            /// </summary>
            public string? Barcode { get; set; }

            /// <summary>
            /// 扫码时间
            /// </summary>
            public DateTime ScanTime { get; set; }

            /// <summary>
            /// 响应内容
            /// </summary>
            public UploadResponse? UploadResponse { get; set; }
        }
    }
}