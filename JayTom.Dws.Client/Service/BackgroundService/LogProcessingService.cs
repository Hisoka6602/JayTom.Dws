using System;
using DryIoc;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Camera;
using System.Threading.Tasks;
using JayTom.Dws.Plugin.Scale;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Repository.LocalLog;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// 日志处理器
    /// </summary>
    public class LogProcessingService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IAppLogRepository _appLogRepository;
        private readonly ICameraLogRepository _cameraLogRepository;
        private readonly ISortingLogRepository _sortingLogRepository;
        private readonly IWeighingLogRepository _weighingLogRepository;
        private readonly IVolumeLogRepository _volumeLogRepository;
        private readonly IApiLogRepository _apiLogRepository;
        private readonly IOutputLogRepository _outputLogRepository;
        private readonly IInputLogRepository _inputLogRepository;
        private readonly IOcrLogRepository _ocrLogRepository;
        private readonly IFtpLogRepository _ftpLogRepository;
        private readonly ICleanupLogRepository _cleanupLogRepository;
        private readonly IExceptionLogRepository _exceptionLogRepository;
        private readonly IDeviceService _deviceService;
        private ConcurrentQueue<ExceptionLogInfoModel> _exceptionItems = new();
        private ConcurrentQueue<AppLogInfoModel> _appLogItems = new();
        private ConcurrentQueue<CameraLogInfoModel> _cameraLogItems = new();
        private ConcurrentQueue<SortingLogInfoModel> _sortingLogItems = new();
        private ConcurrentQueue<WeighingLogInfoModel> _weighingLogItems = new();
        private ConcurrentQueue<VolumeLogInfoModel> _volumeLogItems = new();
        private ConcurrentQueue<ApiLogInfoModel> _apiLogInfoItems = new();
        private ConcurrentQueue<OutputLogInfoModel> _outputLogItems = new();
        private ConcurrentQueue<InputLogInfoModel> _inputLogItems = new();
        private ConcurrentQueue<OcrLogInfoModel> _ocrLogItems = new();
        private ConcurrentQueue<FtpLogInfoModel> _ftpLogItems = new();
        private ConcurrentQueue<LogCleaningLogInfoModel> _logCleaningLogItems = new();

        //LogCleaningLogInfoModel
        public LogProcessingService(IAppLogRepository appLogRepository,
            ICameraLogRepository cameraLogRepository,
            ISortingLogRepository sortingLogRepository,
            IWeighingLogRepository weighingLogRepository,
            IVolumeLogRepository volumeLogRepository,
            IApiLogRepository apiLogRepository,
            IOutputLogRepository outputLogRepository,
            IInputLogRepository inputLogRepository,
            IOcrLogRepository ocrLogRepository,
            IFtpLogRepository ftpLogRepository,
            ICleanupLogRepository cleanupLogRepository,
            IExceptionLogRepository exceptionLogRepository,
            IDeviceService deviceService) {
            _appLogRepository = appLogRepository;
            _cameraLogRepository = cameraLogRepository;
            _sortingLogRepository = sortingLogRepository;
            _weighingLogRepository = weighingLogRepository;
            _volumeLogRepository = volumeLogRepository;
            _apiLogRepository = apiLogRepository;
            _outputLogRepository = outputLogRepository;
            _inputLogRepository = inputLogRepository;
            _ocrLogRepository = ocrLogRepository;
            _ftpLogRepository = ftpLogRepository;
            _cleanupLogRepository = cleanupLogRepository;
            _exceptionLogRepository = exceptionLogRepository;
            _deviceService = deviceService;
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(item => {
                if (item is SettingsChangedEvent model) {
                    _appLogItems.Enqueue(new AppLogInfoModel() {
                        Type = LogType.Information,
                        Message = $"更改配置:{model.SettingsName}"
                    });
                }
            });
            //异常日志
            EventAggregator.Instance.Subscribe<ExceptionLogInfoModel>(item => {
                if (item is ExceptionLogInfoModel model) {
                    //添加
                    _exceptionItems.Enqueue(model);
                }
            });
            //程序运行日志
            EventAggregator.Instance.Subscribe<AppLogInfoModel>(item => {
                if (item is AppLogInfoModel model) {
                    //添加

                    _appLogItems.Enqueue(model);
                }
            });
            //相机日志
            EventAggregator.Instance.Subscribe<CameraLogInfoModel>(item => {
                if (item is CameraLogInfoModel model) {
                    //添加

                    _cameraLogItems.Enqueue(model);
                }
            });
            //分拣日志
            EventAggregator.Instance.Subscribe<SortingLogInfoModel>(item => {
                if (item is SortingLogInfoModel model) {
                    //添加

                    _sortingLogItems.Enqueue(model);
                }
            });
            //称重日志队列
            EventAggregator.Instance.Subscribe<WeighingLogInfoModel>(item => {
                if (item is WeighingLogInfoModel model) {
                    //添加

                    _weighingLogItems.Enqueue(model);
                }
            });
            //体积日志队列
            EventAggregator.Instance.Subscribe<VolumeLogInfoModel>(item => {
                if (item is VolumeLogInfoModel model) {
                    //添加

                    _volumeLogItems.Enqueue(model);
                }
            });
            //Api日志队列
            EventAggregator.Instance.Subscribe<ApiLogInfoModel>(item => {
                if (item is ApiLogInfoModel model) {
                    //添加

                    _apiLogInfoItems.Enqueue(model);
                }
            });
            //输出日志队列
            EventAggregator.Instance.Subscribe<OutputLogInfoModel>(item => {
                if (item is OutputLogInfoModel model) {
                    //添加

                    _outputLogItems.Enqueue(model);
                }
            });
            //输入日志队列
            EventAggregator.Instance.Subscribe<InputLogInfoModel>(item => {
                if (item is InputLogInfoModel model) {
                    //添加

                    _inputLogItems.Enqueue(model);
                }
            });
            //Ocr日志队列
            EventAggregator.Instance.Subscribe<OcrLogInfoModel>(item => {
                if (item is OcrLogInfoModel model) {
                    //添加

                    _ocrLogItems.Enqueue(model);
                }
            });
            //Ftp日志队列
            EventAggregator.Instance.Subscribe<FtpLogInfoModel>(item => {
                if (item is FtpLogInfoModel model) {
                    //添加

                    _ftpLogItems.Enqueue(model);
                }
            });
            //清理记录队列
            EventAggregator.Instance.Subscribe<LogCleaningLogInfoModel>(item => {
                if (item is LogCleaningLogInfoModel model) {
                    //添加

                    _logCleaningLogItems.Enqueue(model);
                }
            });
            _deviceService.BarcodeScanned += delegate (object? sender, BarcodeReadEventArgs args) {
                EventAggregator.Instance.Publish(new CameraLogInfoModel() {
                    Type = LogType.Information,
                    Message = $"相机:{args.CameraSerialNumber}获取到条码[{args.Barcode}]",
                    CameraSerialNumber = args.CameraSerialNumber,
                });
            };
            _deviceService.VolumeCaptured += delegate (object? sender, VolumeCapturedEventArgs args) {
                EventAggregator.Instance.Publish(new CameraLogInfoModel() {
                    Type = LogType.Information,
                    Message = $"相机,获取体积信息:{args.Length},{args.Width},{args.Height}",
                });
                EventAggregator.Instance.Publish(new VolumeLogInfoModel() {
                    Type = LogType.Information,
                    Message = $"获取体积信息:{args.Length},{args.Width},{args.Height}",
                    DataSourceType = DataSourceType.DeviceInput
                });
            };
            _deviceService.CameraBound += delegate (object? sender, CameraFinderItemInfoModel model) {
                EventAggregator.Instance.Publish(new CameraLogInfoModel() {
                    Type = LogType.Information,
                    Message = $"相机:{model.SerialNumber},绑定到{model.BoundType}",
                    CameraSerialNumber = model.SerialNumber
                });
            };
            _deviceService.CameraEnumerationRefreshed += delegate (object? sender, List<CameraFinderItemInfoModel> list) {
                EventAggregator.Instance.Publish(new CameraLogInfoModel() {
                    Type = LogType.Information,
                    Message = $"枚举相机",
                });
            };
            _deviceService.CameraDisconnected += delegate (object? sender, List<ICamera> list) {
                EventAggregator.Instance.Publish(new CameraLogInfoModel() {
                    Type = LogType.Warning,
                    Message = $"相机断开连接",
                });
            };
            _deviceService.CameraFault += delegate (object? sender, List<ICamera> list) {
                EventAggregator.Instance.Publish(new CameraLogInfoModel() {
                    Type = LogType.Exception,
                    Message = $"相机故障",
                });
            };
            _deviceService.CameraUnbound += delegate (object? sender, CameraFinderItemInfoModel model) {
                EventAggregator.Instance.Publish(new CameraLogInfoModel() {
                    Type = LogType.Information,
                    Message = $"相机已解绑",
                    CameraSerialNumber = model.SerialNumber
                });
            };
            _deviceService.NotBarcodeHitEvent += delegate (object? sender, BarcodeReadEventArgs args) {
                EventAggregator.Instance.Publish(new CameraLogInfoModel() {
                    Type = LogType.Warning,
                    Message = $"相机光电触发但未识别到条码",
                    CameraSerialNumber = args.CameraSerialNumber
                });
            };
            _deviceService.PanoramaCaptured += delegate (object? sender, PanoramaCaptureEventArgs args) {
                EventAggregator.Instance.Publish(new CameraLogInfoModel() {
                    Type = LogType.Information,
                    Message = $"相机截取到全景图",
                    CameraSerialNumber = args.CameraSerialNumber
                });
            };
            //磅秤
            _deviceService.ScaleConnected += delegate (object? sender, ScaleConnectedEventArgs args) {
                EventAggregator.Instance.Publish(new WeighingLogInfoModel() {
                    Type = LogType.Information,
                    Message = $"磅秤已连接",
                    DataSourceType = DataSourceType.DeviceInput
                });
            };
            _deviceService.ScaleDisconnected += delegate (object? sender, ScaleDisconnectedEventArgs args) {
                EventAggregator.Instance.Publish(new WeighingLogInfoModel() {
                    Type = LogType.Warning,
                    Message = $"磅秤已断开",
                    DataSourceType = DataSourceType.DeviceInput
                });
            };
            _deviceService.WeightStabilized += delegate (object? sender, WeightChangedEventArgs args) {
                EventAggregator.Instance.Publish(new WeighingLogInfoModel() {
                    Type = LogType.Information,
                    FormatWeight = args.FormattedWeight,
                    Source = args.OriginalContent,
                    Message = $"获取到重量,原内容[{args.OriginalContent}],格式化后重量:{args.FormattedWeight:F3}",
                    DataSourceType = DataSourceType.DeviceInput,
                    CommunicationType = CommunicationType.Receive,
                });
            };
            //http
            EventAggregator.Instance.Subscribe<ApiResponseReceived>(item => {
                if (item is ApiResponseReceived model) {
                    EventAggregator.Instance.Publish(new ApiLogInfoModel() {
                        Type = model.UploadResponse?.IsSuccess == true ? LogType.Information : LogType.Exception,
                        ApiParameters = model.UploadResponse?.ApiParameters ?? string.Empty,
                        CreateTime = model.UploadResponse?.RequestTime ?? DateTime.Now,
                        Duration = model.UploadResponse?.Duration ?? 0,
                        ExceptionMsg = model.UploadResponse?.ExceptionMsg ?? string.Empty,
                        RequestContent = model.UploadResponse?.RequestContent ?? string.Empty,
                        RequestTime = model.UploadResponse?.RequestTime ?? DateTime.Now,
                        ResponseContent = model.UploadResponse?.ResponseContent ?? string.Empty,
                        ResponseTime = model.UploadResponse?.ResponseTime ?? DateTime.Now,
                        Url = model.UploadResponse?.RequestUrl ?? string.Empty,
                    });
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                //异常日志
                var isException = _exceptionItems.TryDequeue(out var exception);
                if (isException && exception is not null) {
                    await _exceptionLogRepository.Insert(exception, stoppingToken);
                }
                //程序日志
                var isAppLog = _appLogItems.TryDequeue(out var appLog);
                if (isAppLog && appLog is not null) {
                    await _appLogRepository.Insert(appLog, stoppingToken);
                }
                //相机日志
                var isCameraLog = _cameraLogItems.TryDequeue(out var cameraLog);
                if (isCameraLog && cameraLog is not null) {
                    await _cameraLogRepository.Insert(cameraLog, stoppingToken);
                }
                //分拣日志
                var isSortingLog = _sortingLogItems.TryDequeue(out var sortingLog);
                if (isSortingLog && sortingLog is not null) {
                    await _sortingLogRepository.Insert(sortingLog, stoppingToken);
                }
                //称重日志
                var isWeighingLog = _weighingLogItems.TryDequeue(out var weighingLog);
                if (isWeighingLog && weighingLog is not null) {
                    await _weighingLogRepository.Insert(weighingLog, stoppingToken);
                }
                //体积日志
                var isVolumeLog = _volumeLogItems.TryDequeue(out var volumeLog);
                if (isVolumeLog && volumeLog is not null) {
                    await _volumeLogRepository.Insert(volumeLog, stoppingToken);
                }
                //API日志
                var isApiLog = _apiLogInfoItems.TryDequeue(out var apiLog);
                if (isApiLog && apiLog is not null) {
                    await _apiLogRepository.Insert(apiLog, stoppingToken);
                }
                //输出日志
                var isOutputLog = _outputLogItems.TryDequeue(out var outputLog);
                if (isOutputLog && outputLog is not null) {
                    await _outputLogRepository.Insert(outputLog, stoppingToken);
                }
                //输入日志
                var isInputLog = _inputLogItems.TryDequeue(out var inputLog);
                if (isInputLog && inputLog is not null) {
                    await _inputLogRepository.Insert(inputLog, stoppingToken);
                }
                //OCR日志
                var isOcrLog = _ocrLogItems.TryDequeue(out var ocrLog);
                if (isOcrLog && ocrLog is not null) {
                    await _ocrLogRepository.Insert(ocrLog, stoppingToken);
                }
                //FTP日志
                var isFtpLog = _ftpLogItems.TryDequeue(out var ftpLog);
                if (isFtpLog && ftpLog is not null) {
                    await _ftpLogRepository.Insert(ftpLog, stoppingToken);
                }
                //清理记录
                var isLogCleaningLog = _logCleaningLogItems.TryDequeue(out var logCleaningLog);
                if (isLogCleaningLog && logCleaningLog is not null) {
                    await _cleanupLogRepository.Insert(logCleaningLog, stoppingToken);
                }
                await Task.Delay(60, stoppingToken);
            }
        }
    }
}