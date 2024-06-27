using System;
using System.Linq;
using System.Text;
using System.Drawing;
using SixLabors.ImageSharp;
using System.ComponentModel;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using NPOI.XSSF.Streaming.Values;
using Org.BouncyCastle.Utilities;
using Point = System.Drawing.Point;
using System.Collections.Concurrent;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;

namespace JayTom.Dws.Plugin.Device.GrayscaleDevice {

    /// <summary>
    /// 归位灰度仪x02
    /// </summary>
    [Description("归位灰度仪x02,上层判断小车数量")]
    public class GwGrayscaleDevice : BaseTcpOperations, IGrayscaleDevice {
        private ConcurrentQueue<GrayscaleResult> _grayscaleResult = new();

        public static Coordinates AttachmentRectangleBoxCoordinates { get; private set; } = new(0, 0, 600, 200);
        public static Coordinates MainRectangleBoxCoordinates { get; private set; } = new(0, 0, 600, 600);

        public static int RegionCarCount { get; private set; } = 1;

        /*
        //起始符
        private readonly byte _startBytes = 0x3A;

        //命令
        private readonly byte _action = 0x73;

        private byte[] _nullByte = "\r\n"u8.ToArray();
        */

        //public Point CenterCoordinates { get; } = new Point(0, 0);

        public event EventHandler<GrayscaleResult>? ParcelLocationReceived;

        public event EventHandler? ParcelLocationNotReceived;

        public async Task<bool> SendCarNumber(int carNumber, CancellationToken token) {
            await Task.Yield();
            if (carNumber is > 0 and < 1000) {
                var array = $":s{carNumber.ToString().PadLeft(3, '0')}\r\n".Select(c => (byte)c).ToArray();
                return await base.SendMessage(array, token);
            }
            return false;
        }

        public async Task<GrayscaleResult> SendCarNumber(int carNumber, int timeOut, CancellationToken token = default) {
            await Task.Yield();
            NLog.LogManager.GetCurrentClassLogger().Info("请求获取灰度仪信息");
            if (carNumber is > 0 and < 1000) {
                var array = $":s{carNumber.ToString().PadLeft(3, '0')}\r\n".Select(c => (byte)c).ToArray();
                var sendMessage = await base.SendMessage(array, token);
                if (sendMessage) {
                    await Task.Delay(timeOut, token);
                    do {
                        _grayscaleResult.TryDequeue(out var result);
                        if (result is not null && result.CarNumber.Equals(carNumber)) {
                            return result;
                        }
                    } while (_grayscaleResult.Count > 0);
                    NLog.LogManager.GetCurrentClassLogger().Error($"灰度仪超时未返回");
                }
            }

            return new GrayscaleResult();
        }

        public void SetRectangleSizes(Coordinates attachmentRectangle, Coordinates mainRectangle) {
            AttachmentRectangleBoxCoordinates = attachmentRectangle;
            MainRectangleBoxCoordinates = mainRectangle;
        }

        public void SetRegionCarCount(int regionCarCount) {
        }

        public GrayscaleResult? DecodeData(byte[] dataBytes) {
            try {
                NLog.LogManager.GetCurrentClassLogger().Info($"接收到的内容:{BitConverter.ToString(dataBytes).Replace("-", " ")}");
                if (dataBytes.Length == 67 && dataBytes.LastOrDefault() == 0x0A) {
                    var grayscaleResult = new GrayscaleResult();

                    var replace = Encoding.UTF8.GetString(dataBytes[..5]).Replace("\0", "0");
                    //小车号
                    grayscaleResult.CarNumber = Convert.ToInt32(replace.Replace(":s", string.Empty));

                    //附加框信息
                    var isAttachmentPackagePresent = dataBytes[6] > 0x30;
                    var coordinates = new Coordinates(
                        BitConverter.ToInt16(dataBytes[8..13].Take(2).ToArray()),
                        BitConverter.ToInt16(dataBytes[13..18].Take(2).ToArray()),
                        BitConverter.ToInt16(dataBytes[8..13].Skip(2).Take(2).ToArray()),
                        BitConverter.ToInt16(dataBytes[13..18].Skip(2).Take(2).ToArray()));
                    var attachmentCenterPoint = (AttachmentRectangleBoxCoordinates.X2 - AttachmentRectangleBoxCoordinates.X1) / 2;
                    var attachmentPackageCenterPoint = (coordinates.X2 - coordinates.X1) / 2;
                    var attachmentPoint = attachmentPackageCenterPoint - attachmentCenterPoint;
                    var orientation = attachmentPoint switch {
                        > 0 => PackageOrientation.Right,
                        < 0 => PackageOrientation.Left,
                        _ => PackageOrientation.Center
                    };
                    grayscaleResult.AttachmentRectangleBoxInfo = new BoxPackageInfo() {
                        IsPackagePresent = isAttachmentPackagePresent,
                        PackageRegionCoordinates = coordinates,
                        PackageOrientation = isAttachmentPackagePresent ? orientation : PackageOrientation.Center,
                        OrientationValue = isAttachmentPackagePresent ? Math.Abs(attachmentPoint) : 0,
                    };
                    //主框信息
                    for (var i = 0; i < 4; i++) {
                        var isPackagePresent = dataBytes[18 + i * 12] > 0x30;
                        var xStart = 20 + i * 12;
                        var xEnd = xStart + 4;
                        var yStart = 20 + i * 12 + 5;
                        var yEnd = yStart + 4;
                        var packageRegionCoordinates = new Coordinates(
                            BitConverter.ToInt16(dataBytes[xStart..xEnd].Take(2).ToArray()),
                            BitConverter.ToInt16(dataBytes[yStart..yEnd].Take(2).ToArray()),
                            BitConverter.ToInt16(dataBytes[xStart..xEnd].Skip(2).Take(2).ToArray()),
                            BitConverter.ToInt16(dataBytes[yStart..yEnd].Skip(2).Take(2).ToArray()));
                        //计算偏向
                        var centerPoint = (MainRectangleBoxCoordinates.X2 - MainRectangleBoxCoordinates.X1) / 2;
                        var packageCenterPoint = (packageRegionCoordinates.X2 - packageRegionCoordinates.X1) / 2;
                        var point = packageCenterPoint - centerPoint;
                        var packageOrientation = point switch {
                            > 0 => PackageOrientation.Right,
                            < 0 => PackageOrientation.Left,
                            _ => PackageOrientation.Center
                        };
                        if (isPackagePresent) {
                            grayscaleResult.MainRectangleBoxInfos.Add(new BoxPackageInfo() {
                                IsPackagePresent = isPackagePresent,
                                PackageRegionCoordinates = packageRegionCoordinates,
                                PackageOrientation = isPackagePresent ? packageOrientation : PackageOrientation.Center,
                                OrientationValue = isPackagePresent ? Math.Abs(point) : 0
                            });
                        }
                    }
                    //中心点
                    grayscaleResult.CenterPoint = new Point(MainRectangleBoxCoordinates.X2 / 2,
                        MainRectangleBoxCoordinates.Y2 / 2);
                    if (grayscaleResult.MainRectangleBoxInfos.Any()) {
                        var pCenterPoint = grayscaleResult.MainRectangleBoxInfos.Max(a => a.PackageRegionCoordinates.Y2) -
                                           grayscaleResult.MainRectangleBoxInfos.Min(a => a.PackageRegionCoordinates.Y1);

                        var carWidth = (MainRectangleBoxCoordinates.Y2 - MainRectangleBoxCoordinates.Y1) / RegionCarCount;

                        grayscaleResult.LinkedCarCount =
                            pCenterPoint / carWidth + (pCenterPoint % (float)carWidth > 0 ? 1 : 0);
                    }
                    else {
                        grayscaleResult.LinkedCarCount = 1;
                    }

                    if (grayscaleResult.AttachmentRectangleBoxInfo.IsPackagePresent) {
                        grayscaleResult.LinkedCarCount += 1;
                    }
                    NLog.LogManager.GetCurrentClassLogger().Info($"解析后的内容:{grayscaleResult}");

                    return grayscaleResult;
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"灰度仪解析异常:{e.Message}");
            }

            NLog.LogManager.GetCurrentClassLogger().Error($"灰度仪返回结果未符合解析条件");
            return null;
        }

        public GwGrayscaleDevice(ITcpCommClient tcpCommClient, ITcpCommServer tcpCommServer) : base(tcpCommClient, tcpCommServer) {
            base.Communication += (sender, info) => {
                if (info.Type == CommunicationType.Receive) {
                    var array = base.ConvertHexStringToByteArray(info.Content);
                    var result = DecodeData(array);
                    if (result is not null) {
                        _grayscaleResult.Enqueue(result);
                    }
                }
            };
        }
    }
}