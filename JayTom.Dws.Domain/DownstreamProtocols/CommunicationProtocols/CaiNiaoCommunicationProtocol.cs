using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;

namespace JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols {

    /// <summary>
    /// 菜鸟协议
    /// </summary>
    public class CaiNiaoCommunicationProtocol : IDeviceCommunicationProtocol {

        public string EncodeData(FunctionType type, object tag, string data, object? other) {
            // 开始符
            byte startDelimiter = 0x02;

            // 报文用途 默认0x30,0x31
            byte[] messagePurpose = { 0x30, 0x31 };

            // 站号(默认1)
            byte[] stationNumber = { 0x30, 0x30 };
            //设备状态
            byte[] deviceStatus = { 0x30, 0x30 };
            // 设备类型 默认 0x30, 0x31
            byte[] deviceType = { 0x30, 0x30 };
            // 结束符
            byte[] endDelimiter = { 0x0D, 0x0A };

            if (type == FunctionType.Heartbeat) {
                var dataBytes = new List<byte>()
                {
                    startDelimiter,
                };
                dataBytes.AddRange(messagePurpose);
                dataBytes.AddRange(stationNumber);
                dataBytes.AddRange(deviceStatus);
                dataBytes.AddRange(endDelimiter);
                return BitConverter.ToString(dataBytes.ToArray()).Replace("-", "");
            }
            else if (type == FunctionType.SendExit) {
                if (other is InstructionsAttach attach) {
                    var dataBytes = new List<byte>()
                    {
                        startDelimiter,
                    };
                    dataBytes.AddRange(messagePurpose);
                    dataBytes.AddRange(stationNumber);

                    //条码
                    {
                        //const byte end = 0x20;
                        attach.BarCode = attach.BarCode?.Replace("_", ";") ?? string.Empty;
                        if (attach.BarCode.ToLower().Equals("noread")) {
                            dataBytes.AddRange(ConvertAsciiToHex("NOREAD"));
                        }
                        else {
                            List<byte> barCodeByte = new();
                            var hex = ConvertAsciiToHex(attach.BarCode);
                            barCodeByte.AddRange(hex);

                            while (barCodeByte.Count < 72) {
                                barCodeByte.Add(32);
                            }
                            var bytes = barCodeByte.Take(72).ToList();
                            dataBytes.AddRange(bytes);
                        }
                    }

                    //体积
                    {
                        List<byte> volumeByte = new();
                        var hex = ConvertAsciiToHex(attach.Volume.ToString());
                        volumeByte.AddRange(hex);

                        while (volumeByte.Count < 16) {
                            volumeByte.Add(32);
                        }
                        var bytes = volumeByte.Take(16).ToList();
                        dataBytes.AddRange(bytes);
                    }
                    //长度
                    {
                        List<byte> lengthByte = new();
                        var hex = ConvertAsciiToHex(attach.Length.ToString());
                        lengthByte.AddRange(hex);

                        while (lengthByte.Count < 16) {
                            lengthByte.Add(32);
                        }
                        var bytes = lengthByte.Take(16).ToList();
                        dataBytes.AddRange(bytes);
                    }
                    //宽度
                    {
                        List<byte> widthByte = new();
                        var hex = ConvertAsciiToHex(attach.Width.ToString());
                        widthByte.AddRange(hex);

                        while (widthByte.Count < 16) {
                            widthByte.Add(32);
                        }
                        var bytes = widthByte.Take(16).ToList();
                        dataBytes.AddRange(bytes);
                    }
                    //高
                    {
                        List<byte> heightByte = new();
                        var hex = ConvertAsciiToHex(attach.Height.ToString());
                        heightByte.AddRange(hex);

                        while (heightByte.Count < 16) {
                            heightByte.Add(32);
                        }
                        var bytes = heightByte.Take(16).ToList();
                        dataBytes.AddRange(bytes);
                    }
                    //流水号
                    {
                        List<byte> guidByte = new();
                        var hex = ConvertAsciiToHex(attach.Guid.ToString());
                        guidByte.AddRange(hex);
                        while (guidByte.Count < 5) {
                            guidByte.Insert(0, 48);
                        }
                        var bytes = guidByte.Take(5).ToList();
                        dataBytes.AddRange(bytes);
                    }
                    //目标格口1
                    {
                        List<byte> dataByte = new();
                        var hex = ConvertAsciiToHex(data);
                        dataByte.AddRange(hex);
                        while (dataByte.Count < 3) {
                            dataByte.Insert(0, 48);
                        }
                        var bytes = dataByte.Take(3).ToList();
                        dataBytes.AddRange(bytes);
                    }
                    //其他补充格口
                    for (var i = 0; i < 5; i++) {
                        List<byte> bytes = new()
                        {
                            {0x20}, {0x20}, {0x20}
                        };
                        dataBytes.AddRange(bytes);
                    }
                    //结束
                    dataBytes.AddRange(endDelimiter);
                    return BitConverter.ToString(dataBytes.ToArray()).Replace("-", "");
                }

                return string.Empty;
            }

            return string.Empty;
        }

        public DeviceDecodeResult? DecodeData(string data) {
            //心跳和触发
            var bytes = HexStringToByteArray(data);
            var subArray = GetSubArray(bytes, 0x02, 0x0D, 0x0A);
            if (subArray?.Any() == true) {
                if (subArray.Length == 9) {
                    //心跳
                    if (subArray[5] == 0x30 && subArray[6] == 0x30) {
                        return new DeviceDecodeResult() {
                            Description = "心跳",
                            Type = FunctionType.Heartbeat,
                            ProtocolName = "菜鸟分拣协议",
                            RawContent = data
                        };
                    }
                    else {
                        var exceptionMessage = subArray[5] switch {
                            0x30 when subArray[6] == 0x31 => "设备控制器异常",
                            0x31 when subArray[6] == 0x31 => "表灰度仪异常",
                            0x31 when subArray[6] == 0x32 => "条码识别设备异常",
                            0x31 when subArray[6] == 0x33 => "体积等其余属性识别设备异常 ",
                            _ => string.Empty
                        };
                        return new DeviceDecodeResult() {
                            Description = "设备异常",
                            ExceptionMessage = $"PLC反馈设备异常:{exceptionMessage}",
                            IsException = true,
                            Type = FunctionType.ExceptionMessage,
                            ProtocolName = "菜鸟分拣协议",
                            RawContent = data
                        };
                    }
                }
                else if (subArray.Length == 15) {
                    //创建包裹
                    var array = subArray.Skip(8).Take(5).ToArray();
                    var asciiNum = Encoding.ASCII.GetString(array);
                    if (int.TryParse(asciiNum, out var num)) {
                        return new DeviceDecodeResult() {
                            Description = "创建包裹触发",
                            Type = FunctionType.CreatePackage,
                            KeywordPosition = 8,
                            Keyword = num.ToString(),
                            ProtocolName = "菜鸟分拣协议",
                            RawContent = data
                        };
                    }
                    else {
                        return new DeviceDecodeResult() {
                            Description = "创建包裹触发点内容解析异常",
                            ExceptionMessage = "返回的ascii码无法解析成数字",
                            IsException = true,
                            Type = FunctionType.ExceptionMessage,
                            ProtocolName = "菜鸟分拣协议",
                            RawContent = data
                        };
                    }
                }
            }
            else {
                return new DeviceDecodeResult() {
                    Description = "解析异常",
                    ExceptionMessage = "找不到数据边界",
                    IsException = true,
                    Type = FunctionType.ExceptionMessage
                };
            }
            return new DeviceDecodeResult() {
                Description = "未定义内容",
                ExceptionMessage = "未定义内容",
                IsException = true,
                Type = FunctionType.ExceptionMessage,
                ProtocolName = "菜鸟分拣协议",
                RawContent = data
            };
        }

        public string ConvertSortingCode(object tag) {
            if (tag is long and > 0) {
                return $"{tag:X4}";
            }

            return string.Empty;
        }

        private byte[] ConvertAsciiToHex(string asciiText) {
            // 将ASCII文本转换为字节数组
            var asciiBytes = Encoding.ASCII.GetBytes(asciiText);

            // 将字节数组转换为十六进制表示
            var hexBuilder = new StringBuilder(asciiBytes.Length * 2);
            foreach (byte b in asciiBytes) {
                hexBuilder.AppendFormat("{0:X2}", b);
            }

            // 将十六进制字符串转换为字节数组
            var hexBytes = new byte[hexBuilder.Length / 2];
            for (var i = 0; i < hexBuilder.Length; i += 2) {
                hexBytes[i / 2] = Convert.ToByte(hexBuilder.ToString().Substring(i, 2), 16);
            }

            return hexBytes;
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

        private static byte[] GetSubArray(byte[] bytes, byte start, byte endPart1, byte endPart2) {
            // 寻找起始索引
            var startIndex = Array.IndexOf(bytes, start);

            // 寻找最后一个endPart1的索引
            var lastPart1Index = Array.LastIndexOf(bytes, endPart1);

            // 如果未找到起始或结束索引，则处理异常或返回空数组，取决于实际需求

            // 寻找endPart2的索引，从最后一个endPart1的位置开始
            var endIndex = Array.IndexOf(bytes, endPart2, lastPart1Index);

            // 如果未找到endPart2，或者endPart2不是跟在endPart1后面的，也处理异常或返回空数组，取决于实际需求
            if (endIndex == -1 || endIndex != lastPart1Index + 1) {
                // 处理异常或返回空数组
                return Array.Empty<byte>();
            }

            // 截取子数组
            var subArray = new byte[endIndex - startIndex + 1];
            Array.Copy(bytes, startIndex, subArray, 0, subArray.Length);

            return subArray;
        }

        public int DataLen { get; } = 0;
    }
}