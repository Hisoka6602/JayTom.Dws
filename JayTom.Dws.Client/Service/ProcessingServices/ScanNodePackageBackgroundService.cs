using System;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.Service.ScanNode;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.Service.ProcessingServices {

    public class ScanNodePackageBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IDeviceService _deviceService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IConfigRepository _configRepository;
        private readonly ISortingService _sortingService;
        private readonly IScanNodeConfigRepository _scanNodeConfigRepository;
        private readonly INodeCommunicationService _nodeCommunicationService;
        private static bool _isWindowsClose;

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
            _sortingService.CreatePackageEvent += (sender, args) => {
            };
            //下位机(移除包裹)
            _sortingService.RemovePackageEvent += (sender, args) => {
            };
            //下位机(包裹异常)
            _sortingService.PackageException += (sender, args) => {
            };

            //配置更改
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
            });
            //创建包裹后触发
            EventAggregator.Instance.Subscribe<TriggerPositionEvent>(async item => {
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
                            Barcode = "NoRead",
                            NodeName = f.NodeName,
                            OriginalText = "未接收到扫码器数据",
                            SerialNumber = string.Empty,
                            ScanTime = DateTime.Now,
                        });
                    }
                });

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

            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken).ContinueWith(a => {
                    //匹配图片
                }, stoppingToken);
            }
        }
    }
}