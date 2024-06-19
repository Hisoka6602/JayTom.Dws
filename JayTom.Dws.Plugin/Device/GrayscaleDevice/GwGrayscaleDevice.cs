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
                if (dataBytes.Length == 67) {
                    var grayscaleResult = new GrayscaleResult();

                    var segments = SplitByteArray(dataBytes, 0x2C);
                    if (segments?.Any() == true && segments.Count == 16) {
                        var replace = Encoding.UTF8.GetString(segments[0]).Replace("\0", "0");
                        //小车号
                        grayscaleResult.CarNumber = Convert.ToInt32(replace.Replace(":s", string.Empty));

                        //附加框信息
                        var isAttachmentPackagePresent = segments[1][0] > 0x30;
                        var coordinates = new Coordinates(
                            BitConverter.ToInt16(segments[2].Take(2).ToArray()),
                            BitConverter.ToInt16(segments[3].Take(2).ToArray()),
                            BitConverter.ToInt16(segments[2].Skip(2).Take(2).ToArray()),
                            BitConverter.ToInt16(segments[3].Skip(2).Take(2).ToArray()));
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
                        for (var i = 1; i <= 4; i++) {
                            var isPackagePresent = segments[i * 3 + 1][0] > 0x30;
                            var packageRegionCoordinates = new Coordinates(
                                BitConverter.ToInt16(segments[i * 3 + 2].Take(2).ToArray()),
                                BitConverter.ToInt16(segments[i * 3 + 3].Take(2).ToArray()),
                                BitConverter.ToInt16(segments[i * 3 + 2].Skip(2).Take(2).ToArray()),
                                BitConverter.ToInt16(segments[i * 3 + 3].Skip(2).Take(2).ToArray()));
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
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"灰度仪解析异常:{e.Message}");
            }

            NLog.LogManager.GetCurrentClassLogger().Error($"灰度仪返回结果未符合解析条件");
            return null;
        }

        private List<byte[]>? SplitByteArray1(byte[] data, byte delimiter) {
            try {
                var segments = new List<byte[]>();
                var start = 0;

                for (var i = 0; i < data.Length; i++) {
                    if (data[i] == delimiter) {
                        var length = i - start;
                        var segment = new byte[length];
                        Array.Copy(data, start, segment, 0, length);
                        segments.Add(segment);
                        start = i + 1;
                    }
                }

                // Add the last segment
                if (start < data.Length) {
                    var length = data.Length - start;
                    var segment = new byte[length];
                    Array.Copy(data, start, segment, 0, length);
                    segments.Add(segment);
                }

                return segments;
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"数据转换错误:{e.Message}");
                return null;
            }
        }

        private List<byte[]>? SplitByteArray(byte[] data, byte delimiter) {
            try {
                var segments = new List<byte[]>();
                var start = 0;

                for (var i = 0; i < data.Length; i++) {
                    // 检查是否遇到了分隔符
                    if (data[i] == delimiter) {
                        // 检查分隔符后面是否有一个Uint16值 (2字节)
                        if (i + 2 < data.Length && data[i + 2] == delimiter) {
                            // 创建当前段并跳过 Uint16 段 (2字节)
                            var length = i - start;
                            var segment = new byte[length];
                            Array.Copy(data, start, segment, 0, length);
                            segments.Add(segment);
                            start = i + 1;

                            // 跳过 Uint16 段
                            i += 2;
                        }
                        else {
                            var length = i - start;
                            var segment = new byte[length];
                            Array.Copy(data, start, segment, 0, length);
                            segments.Add(segment);
                            start = i + 1;
                        }
                    }
                }

                // 添加最后一个段
                if (start < data.Length) {
                    var length = data.Length - start;
                    var segment = new byte[length];
                    Array.Copy(data, start, segment, 0, length);
                    segments.Add(segment);
                }

                return segments;
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"数据转换错误:{e.Message}");
                return null;
            }
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