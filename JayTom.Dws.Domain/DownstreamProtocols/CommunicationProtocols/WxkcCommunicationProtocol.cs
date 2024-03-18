using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols {

    public class WxkcCommunicationProtocol : IDeviceCommunicationProtocol {

        public string EncodeData(FunctionType type, object tag, string data, object? other) {
            if (other is InstructionsAttach attach) {
                //判断是否8个字节
                //起始码
                var startData = "00";
                var functionData = "00";
                var interaction = "00";
                var bytes = HexStringToByteArray(data);

                switch (type) {
                    case FunctionType.SendExit:
                        if (bytes.Length == 2) {
                            //格口号
                            startData = "F9";
                            functionData = "11";
                            interaction = "01";
                        }
                        break;

                    case FunctionType.Heartbeat:
                        startData = "95";
                        functionData = "01";
                        break;

                    default:
                        return string.Empty;
                }
                var hexData = $"{startData}{functionData}{attach.Guid:X4}{data.Replace(" ", string.Empty)}{interaction}";
                var byteArray = HexStringToByteArray(hexData);
                var checksum = XorChecksum(byteArray).ToString("X2");
                return HexWithDelimiter($"{hexData}{checksum}", " ");
            }

            return string.Empty;
        }

        public DeviceDecodeResult? DecodeData(string data) {
            //判断是否8个字节
            var bytes = HexStringToByteArray(data);
            var type = FunctionType.None;
            var description = string.Empty;
            var key = string.Empty;
            var keywordPosition = 0;
            if (bytes.Length is 8 or 7) {
                //不效验
                string hexString;
                int number;
                switch (bytes[1]) {
                    case 0x12:
                        type = FunctionType.CreatePackage;
                        description = $"创建包裹";
                        hexString = BitConverter.ToString(new[] { bytes[2], bytes[3] })
                           .Replace("-", string.Empty).Replace(" ", string.Empty);
                        if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out number)) {
                            key = number.ToString();
                        }
                        keywordPosition = 2;
                        break;

                    case 0x02:
                        type = FunctionType.Heartbeat;
                        description = $"心跳包";
                        hexString = BitConverter.ToString(new[] { bytes[2], bytes[3] })
                           .Replace("-", string.Empty).Replace(" ", string.Empty);
                        if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out number)) {
                            key = number.ToString();
                        }
                        keywordPosition = 2;
                        break;

                    case 0x22://原0x21,菜鸟项目改成0x22
                        type = FunctionType.RemovePackage;
                        description = $"移除包裹";
                        hexString = BitConverter.ToString(new[] { bytes[2], bytes[3] })
                            .Replace("-", string.Empty).Replace(" ", string.Empty);
                        if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out number)) {
                            key = number.ToString();
                        }
                        keywordPosition = 2;
                        break;

                        /*case 0x22:
                            type = FunctionType.None;
                            break;*/
                }

                return new DeviceDecodeResult() {
                    Description = description,
                    Type = type,
                    Keyword = key,
                    KeywordPosition = keywordPosition,
                    ProtocolName = "无限创科协议",
                    RawContent = data
                };
            }

            return null;
        }

        public string ConvertSortingCode(object tag) {
            if (tag is long and > 0) {
                return $"{tag:X4}";
            }

            return string.Empty;
        }

        public int DataLen => 8;

        private static byte[] HexStringToByteArray(string hexString) {
            try {
                hexString = hexString.Replace(" ", ""); // 移除空格

                var bytes = new byte[hexString.Length / 2];
                for (var i = 0; i < hexString.Length; i += 2) {
                    bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
                }

                return bytes;
            }
            catch (Exception e) {
                return new byte[] { 0x00 };
            }
        }

        public static string HexWithDelimiter(string hexString, string delimiter) {
            for (int i = 2; i <= hexString.Length; i += 3) {
                hexString = hexString.Insert(i, delimiter);
            }
            return hexString;
        }

        public static byte XorChecksum(byte[] data) {
            byte checksum = 0;
            for (var i = 0; i < data.Length; i++) {
                if (i > 0) {
                    checksum ^= data[i];
                }
            }
            return checksum;
        }
    }
}