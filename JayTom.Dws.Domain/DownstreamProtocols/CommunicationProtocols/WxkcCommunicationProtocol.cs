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
                if (string.IsNullOrEmpty(data)) {
                    data = "00 00";
                }
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

                    case FunctionType.SendPreSignal:
                        //前置信号
                        startData = "F9";
                        functionData = "13";
                        interaction = "01";
                        break;

                    case FunctionType.PackageInfoCompletedSignal:
                        //包裹信息赋值完成
                        startData = "F9";
                        functionData = "15";
                        interaction = "01";
                        break;

                    case FunctionType.Heartbeat:
                        startData = "F9";
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
            var sortingExceptionReturnType = SortingExceptionReturnType.None;
            var description = string.Empty;
            var key = string.Empty;
            var keywordPosition = 0;
            var commandParsing = new CommandParsing();
            if (bytes.Length is 8) {
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

                    case 0x21:
                        type = FunctionType.RemovePackage;
                        description = $"移除包裹";
                        hexString = BitConverter.ToString(new[] { bytes[2], bytes[3] })
                            .Replace("-", string.Empty).Replace(" ", string.Empty);
                        if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out number)) {
                            key = number.ToString();
                        }
                        keywordPosition = 2;
                        break;

                    case 0x22:
                        sortingExceptionReturnType = SortingExceptionReturnTypeConvert(bytes[6]);
                        type = FunctionType.PackageException;
                        description = $"分拣异常";
                        hexString = BitConverter.ToString(new[] { bytes[2], bytes[3] })
                            .Replace("-", string.Empty).Replace(" ", string.Empty);
                        if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out number)) {
                            key = number.ToString();
                        }
                        keywordPosition = 2;
                        commandParsing = new CommandParsing() {
                            SequenceNumber = (uint)number,
                            ExceptionCode = bytes[7],
                            FunctionCode = bytes[1],
                            CompartmentNumber = BitConverter.ToUInt32(new byte[] { bytes[5], bytes[4], 0, 0 }, 0)
                        };
                        break;

                    /*case 0x23:
                        /*type = FunctionType.None;#1#
                        type = FunctionType.FlowToEndOrException;
                        description = $"流到尾部/或指定异常口";
                        hexString = BitConverter.ToString(new[] { bytes[2], bytes[3] })
                            .Replace("-", string.Empty).Replace(" ", string.Empty);
                        if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out number)) {
                            key = number.ToString();
                        }
                        keywordPosition = 2;
                        break;*/

                    case 0x14:
                        //前置信号回复
                        type = FunctionType.ReceivePreSignalReply;
                        description = $"前置信号";
                        hexString = BitConverter.ToString(new[] { bytes[2], bytes[3] })
                            .Replace("-", string.Empty).Replace(" ", string.Empty);
                        if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out number)) {
                            key = number.ToString();
                        }
                        keywordPosition = 2;
                        break;

                    case 0x16:
                        type = FunctionType.SequenceBindingReply;
                        description = $"序号回复绑定";
                        hexString = BitConverter.ToString(new[] { bytes[2], bytes[3] })
                            .Replace("-", string.Empty).Replace(" ", string.Empty);
                        if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out number)) {
                            key = number.ToString();
                        }
                        keywordPosition = 2;
                        break;

                    case 0x31:
                        type = FunctionType.ResetButtonTrigger;
                        description = $"复位按钮触发";
                        hexString = BitConverter.ToString(new[] { bytes[2], bytes[3] })
                            .Replace("-", string.Empty).Replace(" ", string.Empty);
                        if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out number)) {
                            key = number.ToString();
                        }
                        keywordPosition = 2;
                        break;
                }

                return new DeviceDecodeResult() {
                    Description = description,
                    Type = type,
                    Keyword = key,
                    KeywordPosition = keywordPosition,
                    ProtocolName = "无限创科协议",
                    RawContent = data,
                    SortingExceptionReturnType = sortingExceptionReturnType,
                    CommandParsing = commandParsing
                };
            }

            return null;
        }

        public int GetLastFourDigits(long value) {
            return (int)(value % 10000);
        }

        public string ConvertSortingCode(object tag) {
            return tag is long and > 0 ? $"{tag:X4}" : string.Empty;
        }

        public int DataLen => 8;

        public SortingExceptionReturnType SortingExceptionReturnTypeConvert(object obj) {
            switch (obj) {
                case byte exceptionByte:
                    return exceptionByte switch {
                        0x01 => SortingExceptionReturnType.DistanceTooClose,
                        0x02 => SortingExceptionReturnType.Locked,
                        0x03 => SortingExceptionReturnType.VehicleNumberMismatch,
                        0x04 => SortingExceptionReturnType.UnstableLineSpeed,
                        _ => SortingExceptionReturnType.None
                    };

                case byte[] { Length: 8 } bytes:
                    return SortingExceptionReturnTypeConvert(bytes[6]);

                case string hexString: {
                        var toByteArray = HexStringToByteArray(hexString);
                        return SortingExceptionReturnTypeConvert(toByteArray);
                    }
                default:
                    return SortingExceptionReturnType.None;
            }
        }

        public CommandParsing? CommandParsingConvert(object obj) {
            if (obj is byte[] { Length: 8 } bytes) {
                return new CommandParsing() {
                    FunctionCode = bytes[1],
                    SequenceNumber = BitConverter.ToUInt32(new byte[] { bytes[3], bytes[2], 0, 0 }, 0),
                    CompartmentNumber = BitConverter.ToUInt32(new byte[] { bytes[5], bytes[4], 0, 0 }, 0),
                    ExceptionCode = bytes[6],
                };
            }

            if (obj is string hexString) {
                var toByteArray = HexStringToByteArray(hexString);
                return CommandParsingConvert(toByteArray);
            }

            return null;
        }

        public static byte[] HexStringToByteArray(string hexString) {
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
            for (var i = 2; i <= hexString.Length; i += 3) {
                hexString = hexString.Insert(i, delimiter);
            }
            return hexString;
        }

        public static byte XorChecksum(byte[] data) {
            return data.Where((t, i) => i > 0).Aggregate<byte, byte>(0, (current, t) => (byte)(current ^ t));
        }
    }
}