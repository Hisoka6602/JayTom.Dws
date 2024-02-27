using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols {

    public class JtstCommunicationProtocol : IDeviceCommunicationProtocol {

        public string EncodeData(FunctionType type, object tag, string data, object? other) {
            if (other is InstructionsAttach attach) {
                if (type == FunctionType.SendExit) {
                    var bytesDictionary = new Dictionary<string, byte[]>()
                    {
                        //整串长度(起始字节到结束字节)
                        {"bitLengthByte",new byte[]{0x2A}},
                        //起始字节
                        {"startByte",new byte[]{0xFC}},
                        //请求0x01或回复0x02
                        {"typeByte",new byte[] { 0x01 }},
                        //命令字节01表示wcs下传格口和单号（固定）
                        {"commandByte",new byte[] { 0x01 }},
                        //线体编码
                        {"productionLineCodeByte",new byte[] { 0x02 }},
                        //仓体号（可自定义）
                        {"compartmentNumberByte",new byte[] { 0x0B }},
                        //条码长度
                        {"barcodeLengthByte",new[] {BitConverter.GetBytes((short)Encoding.UTF8.GetBytes(attach.BarCode??string.Empty).Length)[0]}},
                        //条码(32位)
                        {"barcodeByte",BarcodeToByteArray(attach.BarCode??string.Empty,32,0x20)},
                        //分隔符('|')
                        {"separatorByte",new byte[] { 0x7C }},
                        //格口号，格口号为3，则为0x03
                        {"exitCodeByte",new []{HexStringToByteArray(data)[0]}},
                        //备用
                        {"spareByteByte",new byte[] { 0x00 }},
                        //结束字符
                        {"endByteByte",new byte[] { 0xFD }},
                    };
                    var sum = bytesDictionary.Values.Sum(byteArray => byteArray.Length) - 1;
                    bytesDictionary["bitLengthByte"] = new[] { BitConverter.GetBytes(sum)[0] };

                    //var bytes = bytesDictionary.SelectMany(kv => kv.Value).ToArray();
                    //发送指令
                    var list = bytesDictionary.Select(s =>
                            BitConverter.ToString(s.Value).Replace("-", " "))
                        .ToList();
                    return string.Join(" ", list);
                }
            }

            return string.Empty;
        }

        public DeviceDecodeResult? DecodeData(string data) {
            var bytes = HexStringToByteArray(data);
            if (bytes.Length == 43) {
                return bytes[3] switch {
                    0x02 =>
                        //分拣信息回传
                        new DeviceDecodeResult {
                            IsException = bytes[42] != 0,
                            ExceptionMessage = ExceptionToString(bytes[42]),
                            Keyword = Encoding.UTF8.GetString(bytes.Skip(7).Take(bytes[6]).ToArray()),
                            Description = "分拣完成指令",
                            KeywordPosition = 7,
                            ProtocolName = "JT-ST",
                            RawContent = data,
                            Type = FunctionType.RemovePackage
                        },
                    0x03 =>
                        //启动
                        //第七位开始,隔一位是/=0x2F,一共13，最后一位是星期
                        new DeviceDecodeResult {
                            Keyword = $"{bytes[7]}-{bytes[9]}-{bytes[11]} {bytes[13]}:{bytes[15]}:{bytes[17]}",
                            Description = "启动",
                            KeywordPosition = 7,
                            ProtocolName = "JT-ST",
                            RawContent = data,
                            Type = FunctionType.StartRunning
                        },
                    0x04 =>
                        //停止
                        //第七位开始,隔一位是/=0x2F,一共13，最后一位是星期
                        new DeviceDecodeResult {
                            Keyword = $"{bytes[7]}-{bytes[9]}-{bytes[11]} {bytes[13]}:{bytes[15]}:{bytes[17]}",
                            Description = "停止",
                            KeywordPosition = 7,
                            ProtocolName = "JT-ST",
                            RawContent = data,
                            Type = FunctionType.StopRunning
                        },
                    0x05 =>
                        //报警
                        //从第8个字节生效(暂时不做)
                        new DeviceDecodeResult {
                            Keyword = $"未实现",
                            Description = "报警",
                            KeywordPosition = 7,
                            ProtocolName = "JT-ST",
                            RawContent = data,
                            Type = FunctionType.ExceptionMessage
                        },
                    0x06 =>
                        //锁格
                        new DeviceDecodeResult {
                            Keyword = $"未实现",
                            Description = "锁格",
                            KeywordPosition = 7,
                            ProtocolName = "JT-ST",
                            RawContent = data,
                            Type = FunctionType.LockExit
                        },
                    _ => null
                };
            }
            return null;
        }

        public int DataLen => 43;

        private static byte[] BarcodeToByteArray(string barcode, int length, byte paddingCharacter) {
            // 使用UTF-8编码将字符串转换为字节数组
            var byteArray = Encoding.UTF8.GetBytes(barcode);

            if (byteArray.Length < length) {
                // 使用PadRight方法进行右侧填充，将补充字符添加到达到指定长度
                var paddedString = barcode.PadRight(length, (char)paddingCharacter);
                byteArray = Encoding.UTF8.GetBytes(paddedString);
            }

            return byteArray;
        }

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

        private string ExceptionToString(byte e) {
            return e switch {
                0x00 => "正常",
                0x01 => "分拣判断异常的",
                0x02 => "超时没有接收到的",
                0x03 => "UCS 未反馈目的地，接收到99的",
                0x99 => "其他",
                _ => string.Empty
            };
        }
    }
}