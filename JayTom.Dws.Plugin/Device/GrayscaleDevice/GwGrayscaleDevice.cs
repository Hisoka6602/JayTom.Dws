using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using NPOI.XSSF.Streaming.Values;
using Org.BouncyCastle.Utilities;
using SixLabors.ImageSharp.Metadata.Profiles.Iptc;

namespace JayTom.Dws.Plugin.Device.GrayscaleDevice {

    /// <summary>
    /// 归位灰度仪x02
    /// </summary>
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

        private GrayscaleResult? DecodeData(byte[] dataBytes) {
            if (dataBytes.Length == 45) {
                var grayscaleResult = new GrayscaleResult();
                var data = Encoding.UTF8.GetString(dataBytes);

                var strings = data.Split(",");
                if (strings.Length == 9) {
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
    }
}