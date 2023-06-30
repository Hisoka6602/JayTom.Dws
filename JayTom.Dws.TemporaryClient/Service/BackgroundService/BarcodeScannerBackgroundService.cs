using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Interface;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;
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

        public BarcodeScannerBackgroundService(IBarcodeScannerService barcodeScannerService,
            ITcpCommunication tcpCommunication,
            IConfiguration configuration,
            ILogger<BarcodeScannerBackgroundService> logger,
            IDataUploader dataUploader) {
            _barcodeScannerService = barcodeScannerService;
            _tcpCommunication = tcpCommunication;
            _configuration = configuration;
            _logger = logger;
            _dataUploader = dataUploader;
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

            //读取模拟体积
            var volumeSimulations = _configuration?.GetSection("VolumeSimulationArray").Get<List<VolumeSimulation>>();
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

                    var volumeSimulation = volumeSimulations?[new Random().Next(volumeSimulations.Count)];
                    var uploadResponse = await _dataUploader.UploadData(barcode, weight,
                        volumeSimulation?.Length ?? 0,
                        volumeSimulation?.Width ?? 0,
                        volumeSimulation?.Height ?? 0,
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
                        Length = volumeSimulation?.Length ?? 0,
                        Width = volumeSimulation?.Width ?? 0,
                        Height = volumeSimulation?.Height ?? 0,
                        ScanTime = scanTime,
                        RequestStatus = uploadResponse?.IsSuccess == true ? 1 : 2,
                        RequestTime = uploadResponse?.RequestTime ?? DateTime.Now,
                        RequestContent = uploadResponse?.RequestContent ?? string.Empty,
                        ResponseContent = (string.IsNullOrWhiteSpace(uploadResponse?.ResponseContent) ? $"Error:{uploadResponse?.ExceptionMsg}" : uploadResponse?.ResponseContent) ?? string.Empty,
                        ResponseTime = uploadResponse?.ResponseTime ?? DateTime.Now,
                    });
                }
            };

            while (!stoppingToken.IsCancellationRequested) {
                /*_barcodeScannerService.OnScanCompleted(new ScanCompletedEventArgs() {
                    TimestampedGuid = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    Barcode = new Random(Guid.NewGuid().GetHashCode()).Next(100000000, 999999999).ToString(),
                    Weight = (float)0.5,
                    Length = 60,
                    Width = 70,
                    Height = 80,
                    ScanTime = DateTime.Now,
                    RequestStatus = 2,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传的内容",
                    ResponseContent = "返回内容",
                    ResponseTime = DateTime.Now,
                });*/
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
            _tcpCommunication.Close();
            //throw new NotImplementedException();
        }

        public class VolumeSimulation {
            public float Length { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
        }
    }
}