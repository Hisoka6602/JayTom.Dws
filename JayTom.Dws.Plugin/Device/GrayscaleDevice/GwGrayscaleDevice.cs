using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.ComponentModel;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using NPOI.XSSF.Streaming.Values;
using Org.BouncyCastle.Utilities;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;

namespace JayTom.Dws.Plugin.Device.GrayscaleDevice {

    /// <summary>
    /// 归位灰度仪x02
    /// </summary>
    [Description("归位灰度仪x02,上层判断小车数量")]
    public class GwGrayscaleDevice : IGrayscaleDevice {

        //起始符
        private readonly byte _startBytes = 0x3A;

        //命令
        private readonly byte _action = 0x73;

        private byte[] _nullByte = "\r\n"u8.ToArray();

        public void Dispose() {
            //断开
        }

        public Point CenterCoordinates { get; } = new Point(0, 0);

        public event EventHandler<GrayscaleResult>? ParcelLocationReceived;

        public event EventHandler? ParcelLocationNotReceived;

        public Task<bool> Connect(string ip, int port, CancellationToken token) {
            //连接

            return Task.FromResult(false);
        }

        public async Task<bool> SendCarNumber(int carNumber, CancellationToken token) {
            await Task.Yield();

            var array = $":s{carNumber.ToString().PadLeft(3, '0')}\r\n".Select(c => (byte)c).ToArray();

            var s = BitConverter.ToString(array);
            return false;
        }

        public GrayscaleResult? DecodeData(byte[] dataBytes) {
            if (dataBytes.Length == 67) {
                var grayscaleResult = new GrayscaleResult();
                var data = Encoding.UTF8.GetString(dataBytes).Replace("\0", "0");

                var strings = data.Split(",");
                if (strings.Length == 16) {
                    //小车号
                    grayscaleResult.CarNumber = Convert.ToInt32(strings[0].Replace(":s", string.Empty));
                    //小车框
                    grayscaleResult.CarFrameExists = strings[1].Equals("1");
                    //中心点x坐标
                    grayscaleResult.CarCenter = new Point(Convert.ToInt32(strings[2]),
                        Convert.ToInt32(strings[3]));
                    //风琴罩
                    grayscaleResult.AccordionExists = strings[4].Equals("1");
                    //风琴罩中心点坐标
                    grayscaleResult.AccordionCenter = new Point(Convert.ToInt32(strings[5]),
                        Convert.ToInt32(strings[6]));
                    //小车包裹面积
                    grayscaleResult.ParcelAreaOnCar = Convert.ToInt32(strings[7]);
                    //风琴罩上的包裹面积
                    grayscaleResult.ParcelAreaOnAccordion = Convert.ToInt32(strings[8]);

                    //如果风琴罩上的中心点坐标等于负数则转两辆车,否则转动回传一辆车
                }
            }

            return new GrayscaleResult();
        }

        public FormatType FormatType { get; set; }
        public ConnectionStatus ConnectionStatus { get; }

        public event EventHandler<string>? ConnectionException;

        public event EventHandler<Exception>? Exception;

        public event EventHandler<string>? Disconnected;

        public event EventHandler<CommunicationInfo>? Communication;

        public event EventHandler<string>? Connected;

        public event EventHandler<Exception>? SendError;

        public Task<bool> Connect(string ipAddress, int port, int timeOut = 1000, FormatType dataType = FormatType.Ascii, int dataLen = 0,
            CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<bool> Reconnect(int count, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<bool> SendMessage(string message, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<bool> SendMessage(byte[] message, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public void Close() {
            throw new NotImplementedException();
        }

        public ConnectionType ConnectionType { get; }
        public ITcpCommServer? TcpServer { get; }
        public ITcpCommClient? TcpClient { get; }

        public Task<bool> Connect(string ipAddress, int port, ConnectionType type, int timeOut = 1000,
            FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default) {
            throw new NotImplementedException();
        }
    }
}