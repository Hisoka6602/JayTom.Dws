using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalLog;

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
            IExceptionLogRepository exceptionLogRepository) {
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