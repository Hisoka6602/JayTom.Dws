using NLog;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.Service.ScanNode;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;

namespace JayTom.Dws.Client.Service.ProcessingServices {

    public class ScanNodePackageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IConfigRepository _configRepository;
        private readonly ISortingService _sortingService;
        private readonly IScanNodeConfigRepository _scanNodeConfigRepository;
        private readonly INodeCommunicationService _nodeCommunicationService;
        private static bool _isWindowsClose;
        private CreatePackageSettingsDto _createPackageSettingsDto = new();
        private SemaphoreSlim _createPackageSlim = new(1);
        private SemaphoreSlim _receivedSlim = new(1);
        private SemaphoreSlim _imagePathSlim = new(1);
        private ConcurrentDictionary<string, FileSystemWatcher> _fileSystemWatcherItems = new();

        public ScanNodePackageBackgroundService(IDeviceService deviceService,
            IImageStorageService imageStorageService,
            IConfigRepository configRepository,
            ISortingService sortingService,
            IScanNodeConfigRepository scanNodeConfigRepository,
            INodeCommunicationService nodeCommunicationService) {
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _configRepository = configRepository;
            _sortingService = sortingService;
            _scanNodeConfigRepository = scanNodeConfigRepository;
            _nodeCommunicationService = nodeCommunicationService;

            //下位机创建包裹
            _sortingService.CreatePackageEvent += async (sender, args) => {
                try {
                    await _createPackageSlim.WaitAsync();
                    if ((_createPackageSettingsDto.PackageCreationMethods &
                         PackageCreationMethodsEnum.LowerMachineCreation) ==
                        PackageCreationMethodsEnum.LowerMachineCreation) {
                        var tryParse = int.TryParse(args.Keyword, out var num);
                        if (tryParse) {
                            //创建包裹
                            var packageInfo = new PackageInfo() {
                                Guid = num,
                                IsCreatedByLowerMachine = true,
                                PackageCreationInstruction = args.Instruction,
                                CreateTime = args.InstructionTime,
                            };
                            EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                                IsSuccess = true,
                                TriggerPosition = TriggerPositionEnum.PackageTrigger,
                                PackageInfo = packageInfo
                            });
                            EventAggregator.Instance.Publish(new InstructionReceived() {
                                Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds(),
                                IsCreatedByLowerMachine = true,
                                SortingCode = packageInfo.Guid.ToString(),
                                InstructionInfos = new List<InstructionInfoModel>()
                                {
                                    new()
                                    {
                                        InstructionContent = args.Instruction,
                                        InstructionGeneratedTime = args.InstructionTime,
                                        InstructionType = InstructionType.CreatePackage
                                    }
                                },
                                ConnectionName = args.ConnectionName,
                            });
                        }
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //下位机(移除包裹)
            _sortingService.RemovePackageEvent += async (sender, args) => {
                await Task.Delay(200);
                try {
                    await _createPackageSlim.WaitAsync();
                    var tryParse = int.TryParse(args.Keyword, out var num);
                    if (tryParse) {
                        var packageInfo = PackageInfoManager.GetPackage(f => f.Value != null && f.Value.Guid.Equals(num));

                        if (packageInfo is not null) {
                            EventAggregator.Instance.Publish(new InstructionReceived() {
                                Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds(),
                                IsCreatedByLowerMachine = true,
                                SortingCode = num.ToString(),
                                InstructionInfos = new List<InstructionInfoModel>()
                                {
                                    new()
                                    {
                                        InstructionContent = args.Instruction,
                                        InstructionGeneratedTime = DateTime.Now,
                                        InstructionType = InstructionType.SignalCallback
                                    },
                                },
                                ConnectionName = args.ConnectionName,
                            });
                            if (_createPackageSettingsDto.PackageRemoveMethods == PackageRemoveMethodsEnum.LowerMachineRemoval) {
                                PackageInfoManager.RemovePackage(packageInfo.CreateTime, "下位机移除");
                            }
                        }
                        else {
                            LogManager.GetCurrentClassLogger().Error($"序号匹配包裹失败,序号:{num},原文:{args.Keyword}");
                        }
                    }
                    else {
                        LogManager.GetCurrentClassLogger().Error($"关键字节转数字失败:{args.Keyword}");
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //下位机(包裹异常)
            _sortingService.PackageException += async (sender, args) => {
                try {
                    await Task.Delay(500);
                    await _createPackageSlim.WaitAsync();
                    var tryParse = int.TryParse(args.Keyword, out var num);
                    if (tryParse) {
                        var packageInfo = PackageInfoManager.GetPackage(f => f.Value != null && f.Value.Guid.Equals(num));
                        if (packageInfo is not null) {
                            EventAggregator.Instance.Publish(new InstructionReceived() {
                                Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds(),
                                IsCreatedByLowerMachine = true,
                                SortingCode = num.ToString(),
                                InstructionInfos = new List<InstructionInfoModel>()
                                {
                                    new()
                                    {
                                        InstructionContent = args.Instruction,
                                        InstructionGeneratedTime = DateTime.Now,
                                        InstructionType = InstructionType.PackageException
                                    }
                                },
                                ConnectionName = args.ConnectionName,
                            });
                        }
                    }
                }
                finally {
                    _createPackageSlim.Release();
                }
            };
            //接收到的消息
            _nodeCommunicationService.DataReceived += async (sender, args) => {
                //获取序号
                var scanNodeConfigInfoModels = await _scanNodeConfigRepository.MemoryCacheData();
                if (args.Info is not null) {
                    try {
                        await _receivedSlim.WaitAsync();
                        //获取已创建的包裹进行赋值
                        var model = scanNodeConfigInfoModels.FirstOrDefault(f => f.IpAddress.Equals(args.Info.IpAddress) &&
                            f.Port.Equals(args.Info.Port));
                        if (model is not null) {
                            var packageInfo = PackageInfoManager.GetPackage(w => w.Value.NodeInfos.Count
                                < model.NodeNum);
                            if (packageInfo is not null) {
                                packageInfo.NodeInfos.Add(new NodeInfoModel() {
                                    NodeName = model.NodeName,
                                    OriginalText = args.Massage,
                                    ScanTime = ConvertScanTime(args.Massage),
                                    SerialNumber = $"{model.IpAddress}-{model.Port}",
                                    NodeNum = model.NodeNum,
                                    PackageId = packageInfo.Timestamp
                                });
                                //触发事件
                                EventAggregator.Instance.Publish(new NodeInfoEvent() {
                                    Content = args.Massage,
                                    NodeName = model.NodeName,
                                    NodeIndex = model.NodeNum,
                                    NodeIp = model.IpAddress,
                                    ScanTime = ConvertScanTime(args.Massage),
                                    PackageInfo = packageInfo
                                });

                                if (packageInfo.NodeInfos.Count == scanNodeConfigInfoModels.Count) {
                                    //完成
                                    PackageInfoManager.CompletedPackage(f => f.Key.Equals(packageInfo.CreateTime));
                                }
                            }
                        }
                        else {
                            NLog.LogManager.GetCurrentClassLogger().Error($"未从设置中匹配的到输入的节点信息");
                        }
                    }
                    finally {
                        _receivedSlim.Release();
                    }
                }
            };
            //配置更改
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is { } model) {
                    switch (model.SettingsName) {
                        case "CreatePackageSettings":
                            _createPackageSettingsDto = await _configRepository.FirstOrDefaultEntity<CreatePackageSettingsDto>(model.SettingsName) ??
                                                        new CreatePackageSettingsDto();

                            break;

                        case "TcpScanSettings":
                            var scanNodeConfigInfoModels = await _scanNodeConfigRepository.MemoryCacheData();
                            if (scanNodeConfigInfoModels.Any()) {
                                foreach (var (key, value) in _fileSystemWatcherItems) {
                                    value.Dispose();
                                }
                                _fileSystemWatcherItems.Clear();
                                scanNodeConfigInfoModels.ForEach(f => {
                                    var fileSystemWatcher = new FileSystemWatcher(f.ImagePath) {
                                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                                                       NotifyFilters.LastWrite,
                                        Filter = "*.*",
                                        EnableRaisingEvents = true
                                    };
                                    fileSystemWatcher.Created += (sender, args) => {
                                        ImageCreated(new ScanNodeImageCreatedInfo() {
                                            NodeIndex = f.NodeNum,
                                            NodeName = f.NodeName,
                                            ImageName = args.FullPath
                                        });
                                    };
                                    _fileSystemWatcherItems.TryAdd(f.NodeName, fileSystemWatcher);
                                });
                            }
                            break;
                    }
                    //其他设置
                }
            });
            //创建包裹后触发
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(async item => {
                if (item is { TriggerPosition: TriggerPositionEnum.PackageTrigger, PackageInfo: { } packageInfo }) {
                    var info = PackageInfoManager.GetLastPackage(f => f is { Value: not null });

                    if (info is not null &&
                        packageInfo.CreateTime.Subtract(info.CreateTime).TotalMilliseconds <
                        _createPackageSettingsDto.PackageCreationInterval) {
                        EventAggregator.Instance.Publish(new FilteredPackageEvent() {
                            FilterType = FilterType.Interval,
                            Timestamp = DateTime.Now,
                            CreateCommand = packageInfo.PackageCreationInstruction
                        });
                        return;
                    }
                    packageInfo.Timestamp = new DateTimeOffset(packageInfo.CreateTime).ToUnixTimeMilliseconds();
                    //添加包裹
                    var packageRemoveTimers = new List<PackageTimer>();
                    if (_createPackageSettingsDto is { IsUseEmptyPackageExpiry: true, EmptyPackageExpiryTime: > 0 }) {
                        packageRemoveTimers.Add(new PackageRemoveTimer() {
                            Description = "空包裹过期",
                            Predicate = w => w.Value.BarCodeInfo == null,
                            RemovalTimeSpan = TimeSpan.FromMilliseconds(_createPackageSettingsDto.EmptyPackageExpiryTime)
                        });
                    }
                    if (_createPackageSettingsDto is { IsUsePackageExpiry: true, PackageExpiryTime: > 0 }) {
                        packageRemoveTimers.Add(new PackageRemoveTimer() {
                            Description = "包裹超过生存周期",
                            RemovalTimeSpan = TimeSpan.FromMilliseconds(_createPackageSettingsDto.PackageExpiryTime)
                        });
                    }
                    var scanNodeConfigInfoModels = await _scanNodeConfigRepository.MemoryCacheData();
                    packageRemoveTimers.AddRange(scanNodeConfigInfoModels.OrderBy(o => o.NodeNum)
                        .Select(model => new PackageAssignmentTimer() {
                            AssignmentTimeSpan = TimeSpan.FromMilliseconds(model.Timeout),
                            Predicate = w => w.Value.NodeInfos.Count < model.NodeNum,
                            AssignmentCallback = a => {
                                a.NodeInfos.Add(new NodeInfoModel() {
                                    NodeName = model.NodeName,
                                    OriginalText = "未接收到扫码器数据",
                                    SerialNumber = string.Empty,
                                    ScanTime = DateTime.Now,
                                    NodeNum = model.NodeNum,
                                    PackageId = packageInfo.Timestamp
                                });

                                if (a.NodeInfos.Count == scanNodeConfigInfoModels.Count) {
                                    PackageInfoManager.CompletedPackage(f => f.Key.Equals(a.CreateTime));
                                }

                                return false;
                            },
                        }));

                    PackageInfoManager.AddPackage(packageInfo, packageRemoveTimers);
                    //触发创建包裹事件
                    EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                        IsSuccess = true,
                        TriggerPosition = TriggerPositionEnum.CreateTimePackageAfter,
                        PackageInfo = packageInfo
                    });
                }
            });
            //程序停止
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(async item => {
            });

            //移除包裹事件
            PackageInfoManager.PackageRemoved += (sender, args) => {
                EventAggregator.Instance.Publish(new TriggerPositionEvent() {
                    IsSuccess = true,
                    TriggerPosition = TriggerPositionEnum.RemovePackageAfter,
                    PackageInfo = args.RemovedPackage,
                    Description = args.Description
                });
            };
            PackageInfoManager.PackageCompleted += async (sender, args) => {
                //执行输出
                //判断数量是否一致
                var scanNodeConfigInfoModels = await _scanNodeConfigRepository.MemoryCacheData();

                scanNodeConfigInfoModels.OrderBy(o => o.NodeNum).ToList().ForEach(f => {
                    var model = args.CompletedPackage.NodeInfos.FirstOrDefault(f =>
                        f.NodeName.Equals(f.NodeName));
                    if (model is null) {
                        args.CompletedPackage.NodeInfos.Add(new NodeInfoModel() {
                            NodeName = f.NodeName,
                            OriginalText = "未接收到扫码器数据NoRead",
                            SerialNumber = string.Empty,
                            ScanTime = DateTime.Now,
                        });
                    }
                });
                if (args.CompletedPackage.BarCodeInfo is null) {
                    var infoModel = args.CompletedPackage.NodeInfos.LastOrDefault(l =>
                        !l.OriginalText.Equals("NoRead", StringComparison.CurrentCultureIgnoreCase));
                    args.CompletedPackage.BarCodeInfo = new BarCodeInfoModel() {
                        Barcode = ConvertBarcode(infoModel?.OriginalText ?? "NoRead"),
                        BindTime = DateTime.Now,
                        ScanTime = infoModel?.ScanTime ?? DateTime.Now,
                    };
                }
                EventAggregator.Instance.Publish(args.CompletedPackage);
            };
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //读配置
            try {
                //读配置
                _createPackageSettingsDto = await _configRepository.FirstOrDefaultEntity<CreatePackageSettingsDto>("CreatePackageSettings", stoppingToken) ?? new CreatePackageSettingsDto();

                var scanNodeConfigInfoModels = await _scanNodeConfigRepository.MemoryCacheData();
                if (scanNodeConfigInfoModels.Any()) {
                    foreach (var (key, value) in _fileSystemWatcherItems) {
                        value.Dispose();
                    }
                    _fileSystemWatcherItems.Clear();
                    scanNodeConfigInfoModels.ForEach(f => {
                        var fileSystemWatcher = new FileSystemWatcher(f.ImagePath) {
                            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                                           NotifyFilters.LastWrite,
                            Filter = "*.*",
                            EnableRaisingEvents = true
                        };
                        fileSystemWatcher.Created += (sender, args) => {
                            ImageCreated(new ScanNodeImageCreatedInfo() {
                                NodeIndex = f.NodeNum,
                                NodeName = f.NodeName,
                                ImageName = args.FullPath
                            });
                        };
                        _fileSystemWatcherItems.TryAdd(f.NodeName, fileSystemWatcher);
                    });
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken).ContinueWith(a => {
                    //获取未匹配到图片的数据
                    // PackageInfoManager.GetPackage(s=>s.Value.)
                }, stoppingToken);
            }
        }

        public static DateTime? ExtractDateTime(string input) {
            try {
                var pattern = @"(\d{4})_(\d{2})_(\d{2})_(\d{2})(\d{2})(\d{2})";
                var match = Regex.Match(input, pattern);
                if (match.Success) {
                    var year = int.Parse(match.Groups[1].Value);
                    var month = int.Parse(match.Groups[2].Value);
                    var day = int.Parse(match.Groups[3].Value);
                    var hour = int.Parse(match.Groups[4].Value);
                    var minute = int.Parse(match.Groups[5].Value);
                    var second = int.Parse(match.Groups[6].Value);

                    return new DateTime(year, month, day, hour, minute, second);
                }

                return null;
            }
            catch (Exception ex) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{ex}");
                return null;
            }
        }

        private string ConvertBarcode(string tcpMassage) {
            var strings = tcpMassage.Split("--");
            return strings.Length > 1 ? strings[1] : "NoRead";
        }

        private DateTime ConvertScanTime(string tcpMassage) {
            var strings = tcpMassage.Split("--");
            if (strings.Length > 1 && long.TryParse(strings[0], out var result)) {
                return DateTimeOffset.FromUnixTimeMilliseconds(result).LocalDateTime;
            }
            return DateTime.Now;
        }

        private async void ImageCreated(ScanNodeImageCreatedInfo info) {
            try {
                await _imagePathSlim.WaitAsync();

                var packageInfo = PackageInfoManager.GetPackage(s =>
                    s.Value.NodeInfos.Any(a => a.NodeNum.Equals(info.NodeIndex) &&
                                               string.IsNullOrEmpty(a.ImagePath)));
                if (packageInfo is not null) {
                    var nodeInfoModel = packageInfo.NodeInfos.FirstOrDefault(f => f.NodeNum.Equals(info.NodeIndex) &&
                        string.IsNullOrEmpty(f.ImagePath));
                    if (nodeInfoModel is not null) {
                        nodeInfoModel.ImagePath = info.ImageName;
                        //推送图片匹配
                        EventAggregator.Instance.Publish(new NodeImageInfoEvent {
                            NodeName = info.NodeName,
                            NodeIndex = info.NodeIndex,
                            PackageInfo = packageInfo,
                            ImagePath = info.ImageName
                        });
                    }
                    else {
                        NLog.LogManager.GetCurrentClassLogger().Error($"图片:{info.ImageName},未匹配到对应的节点");
                    }
                }
                else {
                    NLog.LogManager.GetCurrentClassLogger().Error($"图片:{info.ImageName},未匹配到数据");
                }
            }
            finally {
                _imagePathSlim.Release();
            }
        }

        public class ScanNodeImageCreatedInfo {
            public int NodeIndex { get; set; }
            public string ImageName { get; set; } = string.Empty;
            public string NodeName { get; set; } = string.Empty;
        }
    }
}