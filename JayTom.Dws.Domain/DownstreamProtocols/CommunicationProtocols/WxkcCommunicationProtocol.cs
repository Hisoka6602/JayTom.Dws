using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;
using JayTom.Dws.Plugin;

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
                            interaction = attach.LinkedCarCount > 0 ? attach.LinkedCarCount.ToString().PadLeft(2, '0') : "01";
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

                    case FunctionType.PackageCenter:
                        //包裹居中
                        startData = "F9";
                        functionData = "15";
                        interaction = attach.LinkedCarCount > 0 ? attach.LinkedCarCount.ToString().PadLeft(2, '0') : "01";
                        break;

                    default:
                        return string.Empty;
                }

                if (type == FunctionType.PackageCenter && attach.PackagePositionInfo is not null) {
                    //需要传偏移方向和偏移量
                    var offsetPercentage = 30 * attach.PackagePositionInfo.OffsetPercentage;
                    if (offsetPercentage > 30) {
                        offsetPercentage = 30;
                    }
                    data =
                        $"{(attach.PackagePositionInfo.OffsetDirection == OffsetDirection.Right ? "01" : "00")} {(int)offsetPercentage:X2}";
                }
                var hexData = $"{startData}{functionData}{attach.Guid:X4}{data.Replace(" ", string.Empty)}{interaction}";
                var byteArray = HexStringToByteArray(hexData);
                var checksum = XorChecksum(byteArray).ToString("X2");
                return HexWithDelimiter($"{hexData}{checksum}", " ");
            }

            return string.Empty;
        }

        public DeviceDecodeResult? DecodeData(string data) {
            data = HexDataFormatter.Normalize(data);
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
                int number;
                switch (bytes[1]) {
                    case 0x12:
                        type = FunctionType.CreatePackage;
                        description = $"创建包裹";
                        number = bytes[2] << 8 | bytes[3];
                        key = number.ToString();
                        keywordPosition = 2;
                        break;

                    case 0x02:
                        type = FunctionType.Heartbeat;
                        description = $"心跳包";
                        number = bytes[2] << 8 | bytes[3];
                        key = number.ToString();
                        keywordPosition = 2;
                        break;

                    case 0x21:
                        type = FunctionType.RemovePackage;
                        description = $"移除包裹";
                        number = bytes[2] << 8 | bytes[3];
                        key = number.ToString();
                        keywordPosition = 2;
                        break;

                    case 0x22:
                        sortingExceptionReturnType = SortingExceptionReturnTypeConvert(bytes[6]);
                        type = FunctionType.PackageException;
                        description = $"分拣异常";
                        number = bytes[2] << 8 | bytes[3];
                        key = number.ToString();
                        keywordPosition = 2;
                        commandParsing = new CommandParsing() {
                            SequenceNumber = (uint)number,
                            ExceptionCode = bytes[7],
                            FunctionCode = bytes[1],
                            CompartmentNumber = BitConverter.ToUInt32([bytes[5], bytes[4], 0, 0], 0)
                        };
                        break;

                    case 0x23:
                        sortingExceptionReturnType = SortingExceptionReturnTypeConvert(bytes[6]);
                        type = FunctionType.PackageExceptionEx;
                        description = $"分拣异常";
                        number = bytes[2] << 8 | bytes[3];
                        key = number.ToString();
                        keywordPosition = 2;
                        commandParsing = new CommandParsing() {
                            SequenceNumber = (uint)number,
                            ExceptionCode = bytes[7],
                            FunctionCode = bytes[1],
                            CompartmentNumber = BitConverter.ToUInt32([bytes[5], bytes[4], 0, 0], 0)
                        };
                        break;

                    case 0x14:
                        //前置信号回复
                        type = FunctionType.ReceivePreSignalReply;
                        description = $"前置信号";
                        number = bytes[2] << 8 | bytes[3];
                        key = number.ToString();
                        keywordPosition = 2;
                        break;

                    case 0x16:
                        type = FunctionType.SequenceBindingReply;
                        description = $"序号回复绑定";
                        number = bytes[2] << 8 | bytes[3];
                        key = number.ToString();
                        keywordPosition = 2;
                        break;

                    case 0x31:
                        type = FunctionType.ResetButtonTrigger;
                        description = $"复位按钮触发";
                        number = bytes[2] << 8 | bytes[3];
                        key = number.ToString();
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

        public SortingExceptionReturnType SortingExceptionReturnTypeConvert(string obj) {
            var hexStringToByteArray = HexStringToByteArray(obj);
            if (hexStringToByteArray.Length == 8) {
                return SortingExceptionReturnTypeConvert(hexStringToByteArray[6]);
            }

            return SortingExceptionReturnType.None;
        }

        public SortingExceptionReturnType SortingExceptionReturnTypeConvert(byte obj) {
            return obj switch {
                0x01 => SortingExceptionReturnType.DistanceTooClose,
                0x02 => SortingExceptionReturnType.LockExit,
                0x03 => SortingExceptionReturnType.VehicleNumberMismatch,
                0x04 => SortingExceptionReturnType.UnstableLineSpeed,
                _ => SortingExceptionReturnType.None
            };
        }

        public CommandParsing? CommandParsingConvert(object obj) {
            if (obj is byte[] { Length: 8 } bytes) {
                return new CommandParsing() {
                    FunctionCode = bytes[1],
                    SequenceNumber = BitConverter.ToUInt32([bytes[3], bytes[2], 0, 0], 0),
                    CompartmentNumber = BitConverter.ToUInt32([bytes[5], bytes[4], 0, 0], 0),
                    ExceptionCode = bytes[6],
                };
            }

            if (obj is string hexString) {
                var toByteArray = HexStringToByteArray(hexString);
                return CommandParsingConvert(toByteArray);
            }

            return null;
        }

        public string? ExitContentConvert(object data) {
            if (data is byte[] { Length: 8 } bytes) {
                return HexDataFormatter.Format(bytes.AsSpan(4, 2));
            }
            if (data is string hexString &&
                HexDataFormatter.TryParse(hexString, out var parsedBytes) &&
                parsedBytes.Length == 8) {
                return ExitContentConvert(parsedBytes);
            }
            return null;
        }

        public static byte[] HexStringToByteArray(string hexString) {
            return HexDataFormatter.TryParse(hexString, out var bytes)
                ? bytes
                : [0x00];
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
