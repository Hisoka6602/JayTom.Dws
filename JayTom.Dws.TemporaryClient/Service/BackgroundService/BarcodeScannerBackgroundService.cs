using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Device;
using System.Diagnostics;
using JayTom.Dws.Interface;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Device.Camera;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using Microsoft.Extensions.Logging;
using Image = System.Drawing.Image;
using JayTom.Dws.Interface.WeciMexicoDv;
using Microsoft.Extensions.Configuration;
using System.Reflection.PortableExecutable;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace JayTom.Dws.TemporaryClient.Service.BackgroundService {

    public class BarcodeScannerBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IBarcodeScannerService _barcodeScannerService;
        private readonly ITcpCommunication _tcpCommunication;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BarcodeScannerBackgroundService> _logger;
        private readonly IDataUploader _dataUploader;
        private readonly I3DCamera _camera;
        private double Length { get; set; }

        private double Width { get; set; }

        private double Height { get; set; }

        public BarcodeScannerBackgroundService(IBarcodeScannerService barcodeScannerService,
            ITcpCommunication tcpCommunication,
            IConfiguration configuration,
            ILogger<BarcodeScannerBackgroundService> logger,
            IDataUploader dataUploader, I3DCamera camera) {
            _barcodeScannerService = barcodeScannerService;
            _tcpCommunication = tcpCommunication;
            _configuration = configuration;
            _logger = logger;
            _dataUploader = dataUploader;
            _camera = camera;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            //逻辑写在这 //Tcp初始化
            //获取连接参数
            var tcpConnectParam = new TcpConnectParam() {
                Address = _configuration?["TCPServerConfig:Address"],
                Port = Convert.ToInt32(_configuration?["TCPServerConfig:Port"])
            };
            //获取分隔符
            var splitChar = _configuration?["SplitChar"];
            //获取存图途径
            var dwsImagePath = _configuration?["DwsImagePath"];
            //获取API配置
            var weciMexicoDvApiParam = new WeciMexicoDvApiParam {
                MachineNo = _configuration?["ApiSettings:MachineNo"] ?? string.Empty,
                TimeOut = Convert.ToInt32(_configuration?["ApiSettings:TimeOut"]),
                Url = _configuration?["ApiSettings:Url"] ?? string.Empty,
            };
            //体积相机连接

            var (b, s) = await _camera.Initialization();
            if (b) {
                await _camera.Connect(string.Empty);
            }
            _camera.VolumeCapturedEvent += delegate (object? sender, VolumeCapturedEventArgs args) {
                Length = args.Length;
                Width = args.Width;
                Height = args.Height;
            };
            _camera.ItemNotDetected += delegate (object? sender, EventArgs args) {
                Length =
                Width =
                Height = 0;
            };
            _camera.ItemOutOfBounds += delegate (object? sender, ItemOutOfBoundsEventArgs args) {
                Length =
                    Width =
                        Height = 0;
            };
            //读取模拟体积
            //var volumeSimulations = _configuration?.GetSection("VolumeSimulationArray").Get<List<VolumeSimulation>>();
            //Api配置
            var (key, value) = await _dataUploader.SetParameters(weciMexicoDvApiParam);
            if (!key) {
                _logger.LogError($"{value}");
            }

            _tcpCommunication.SetParameter(tcpConnectParam);
            var connect = _tcpCommunication.Connect();
            if (connect) {
                _logger.LogInformation($"Tcp服务端开启成功,地址{_configuration?["TCPServerConfig:Address"]},端口:{_configuration?["TCPServerConfig:Port"]}");
            }
            else {
                _logger.LogError($"Tcp服务启动失败!");
            }

            _tcpCommunication.Communication += async delegate (object _, CommunicationInfo info) {
                double? uploadLength = null, uploadWidth = null, uploadHeight = null;
                await Task.Yield();
                var strings = info.Content.Split($"{splitChar}");
                if (strings.Length > 1) {
                    var scanTime = DateTime.Now;
                    var barcode = strings[0];
                    var weight = Convert.ToSingle(strings[1]);
                    var imagePath = $"{dwsImagePath}\\{DateTime.Now:yyyy-MM-dd}\\{DateTime.Now:HH}\\{barcode}.jpg";
                    Image? image = null;
                    if (File.Exists(imagePath)) {
                        image = System.Drawing.Image.FromFile(imagePath);
                    }
                    //获取体积
                    var startTime = DateTime.Now;
                    do {
                        if (Length > 0) {
                            uploadLength = Length;
                        }
                        if (Width > 0) {
                            uploadWidth = Width;
                        }
                        if (Height > 0) {
                            uploadHeight = Height;
                        }
                        Console.WriteLine(11);
                    } while ((uploadLength is null || uploadWidth is null || uploadHeight is null)
                             && DateTime.Now.Subtract(startTime).TotalSeconds < 10);

                    var uploadResponse = await _dataUploader.UploadData(barcode, weight,
                        uploadLength ?? 0,
                        uploadWidth ?? 0,
                        uploadHeight ?? 0,
                        image: image, token: stoppingToken);
                    //得到条码和重量
                    //获取图片路径
                    //获取随机体积
                    //上传接口
                    //调用传输事件
                    //Exception
                    _barcodeScannerService.OnScanCompleted(new ScanCompletedEventArgs() {
                        TimestampedGuid = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                        Barcode = barcode,
                        Weight = weight,
                        Length = (float)(uploadLength ?? 0),
                        Width = (float)(uploadWidth ?? 0),
                        Height = (float)(uploadHeight ?? 0),
                        ScanTime = scanTime,
                        RequestStatus = uploadResponse?.IsSuccess == true ? 1 : 2,
                        RequestTime = uploadResponse?.RequestTime ?? DateTime.Now,
                        RequestContent = "The request content is too large and will not be saved.",
                        ResponseContent = (string.IsNullOrWhiteSpace(uploadResponse?.ResponseContent) ? $"Error:{uploadResponse?.ExceptionMsg}" : uploadResponse?.ResponseContent) ?? string.Empty,
                        ResponseTime = uploadResponse?.ResponseTime ?? DateTime.Now,
                    });
                }
            };

            while (!stoppingToken.IsCancellationRequested) {
                /*if (!stoppingToken.IsCancellationRequested &&
                    _camera.Status != DeviceStatus.Connected) {
                    _camera.Dispose();
                    var (key1, value1) = await _camera.Initialization();
                    if (key1) {
                        await _camera.Connect(string.Empty);
                    }
                }*/
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            _tcpCommunication.Close();
            //throw new NotImplementedException();
        }

        public override Task StopAsync(CancellationToken stoppingToken) {
            _camera?.Dispose();
            return Task.CompletedTask;
        }

        public class VolumeSimulation {
            public float Length { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
        }
    }
}