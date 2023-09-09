using System;
using DryIoc;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using JayTom.Dws.Interface;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using static JayTom.Dws.Client.Service.BackgroundService.ScanProcessBackgroundService;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// Api提交处理器
    /// </summary>
    public class SubmitApiBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfigRepository _configRepository;
        private ConcurrentQueue<SubmitItemInfo> _submitItems = new();
        private ApiSettingsDto? _apiSettingsDto;
        private static DefaultApi.DefaultApiParameters _defaultApiParameters = new();

        public SubmitApiBackgroundService(IHttpClientFactory httpClientFactory, IConfigRepository configRepository) {
            _httpClientFactory = httpClientFactory;
            _configRepository = configRepository;
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
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is SettingsChangedEvent model) {
                    if (model.SettingsName.Equals("ApiSettings")) {
                        var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("ApiSettings"));
                        if (configInfoModel is not null) {
                            try {
                                _apiSettingsDto = JsonConvert.DeserializeObject<ApiSettingsDto>(configInfoModel.Value);
                            }
                            catch (Exception e) {
                                //抛出异常事件
                                Console.WriteLine(e);
                            }
                        }
                    }
                    else if (model.SettingsName.Equals("DefaultApiParameters")) {
                        //默认上传接口改参数
                        var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("DefaultApiParameters"));
                        if (configInfoModel is not null) {
                            try {
                                var defaultApiDto = JsonConvert.DeserializeObject<DefaultApiDto>(configInfoModel.Value);
                                if (defaultApiDto != null) {
                                    _defaultApiParameters = new DefaultApi.DefaultApiParameters() {
                                        CompleteMatch = defaultApiDto.CompleteMatch,
                                        IsUseJsonUpload = defaultApiDto.IsUseJsonUpload,
                                        JsonTemplate = defaultApiDto.JsonTemplate,
                                        RegularExpression = defaultApiDto.RegularExpression,
                                        StringContains = defaultApiDto.StringContains,
                                        Timeout = defaultApiDto.Timeout,
                                        StringTemplate = defaultApiDto.StringTemplate,
                                        Url = defaultApiDto.Url,
                                        ValidationMode = (int)defaultApiDto.ValidationMode,
                                    };
                                }
                            }
                            catch (Exception e) {
                                //抛出异常事件
                                Console.WriteLine(e);
                            }
                        }
                    }
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //读参数
            await ReadDefaultConfig();
            while (!stoppingToken.IsCancellationRequested) {
                //取出
                //需要判断用户选择的接口和参数设置
                var tryDequeue = _submitItems.TryDequeue(out var info);

                if (tryDequeue && info is not null) {
                    //上传
                    //判断上传接口
                    Task.Run(async () => {
                        IDataUploader uploader;
                        switch (_apiSettingsDto?.Type) {
                            case ApiType.None:
                                return;

                            case ApiType.DefaultApi: {
                                    //基础接口
                                    uploader = new DefaultApi(_httpClientFactory);
                                    //设置参数
                                    var (key, value) = await uploader.SetParameters(_defaultApiParameters);
                                    if (key) {
                                        var uploadResponse = await uploader.UploadData(info.Barcode ?? string.Empty,
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
                                    else {
                                        Console.WriteLine("设置参数失败!");
                                    }

                                    break;
                                }
                        }
                    });
                }

                await Task.Delay(10, stoppingToken);
            }
        }

        private async Task ReadDefaultConfig() {
            //上传类型
            var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("ApiSettings"));
            if (configInfoModel is not null) {
                try {
                    _apiSettingsDto = JsonConvert.DeserializeObject<ApiSettingsDto>(configInfoModel.Value);
                }
                catch (Exception e) {
                    //抛出异常事件
                    Console.WriteLine(e);
                }
            }
            //默认接口
            configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("DefaultApiParameters"));
            if (configInfoModel is not null) {
                try {
                    var defaultApiDto = JsonConvert.DeserializeObject<DefaultApiDto>(configInfoModel.Value);
                    if (defaultApiDto != null) {
                        _defaultApiParameters = new DefaultApi.DefaultApiParameters() {
                            CompleteMatch = defaultApiDto.CompleteMatch,
                            IsUseJsonUpload = defaultApiDto.IsUseJsonUpload,
                            JsonTemplate = defaultApiDto.JsonTemplate,
                            RegularExpression = defaultApiDto.RegularExpression,
                            StringContains = defaultApiDto.StringContains,
                            Timeout = defaultApiDto.Timeout,
                            StringTemplate = defaultApiDto.StringTemplate,
                            Url = defaultApiDto.Url,
                            ValidationMode = (int)defaultApiDto.ValidationMode,
                        };
                    }
                }
                catch (Exception e) {
                    //抛出异常事件
                    Console.WriteLine(e);
                }
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