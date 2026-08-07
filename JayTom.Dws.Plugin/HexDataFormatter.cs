using System;
using System.Buffers;
namespace JayTom.Dws.Plugin {

    /// <summary>
    /// 统一十六进制字节数据在界面和日志中的文本格式。
    /// </summary>
    public static class HexDataFormatter {

        /// <summary>
        /// 将字节数据格式化为大写、单空格分隔的十六进制文本。
        /// </summary>
        public static string Format(ReadOnlySpan<byte> bytes) {
            if (bytes.IsEmpty) {
                return string.Empty;
            }

            return string.Create((bytes.Length * 3) - 1, bytes.ToArray(),
                static (destination, source) => {
                    var destinationIndex = 0;
                    for (var sourceIndex = 0; sourceIndex < source.Length; sourceIndex++) {
                        if (destinationIndex > 0) {
                            destination[destinationIndex++] = ' ';
                        }

                        _ = Convert.TryToHexString(
                            source.AsSpan(sourceIndex, 1),
                            destination.Slice(destinationIndex, 2),
                            out var charsWritten);
                        destinationIndex += charsWritten;
                    }
                });
        }

        /// <summary>
        /// 将紧凑、空格、连字符或逗号分隔的十六进制文本统一为空格分隔格式。
        /// 无法解析时保留原文。
        /// </summary>
        public static string Normalize(string? value) {
            return TryParse(value, out var bytes)
                ? Format(bytes)
                : value ?? string.Empty;
        }

        /// <summary>
        /// 尝试解析常见分隔形式的十六进制文本。
        /// </summary>
        public static bool TryParse(string? value, out byte[] bytes) {
            bytes = [];
            if (string.IsNullOrWhiteSpace(value)) {
                return false;
            }

            var source = value.AsSpan();
            var hexCharacterCount = 0;
            foreach (var character in source) {
                if (char.IsWhiteSpace(character) || character is '-' or ',' or ':') {
                    continue;
                }

                if (!char.IsAsciiHexDigit(character)) {
                    return false;
                }

                hexCharacterCount++;
            }

            if (hexCharacterCount == 0 || hexCharacterCount % 2 != 0) {
                return false;
            }

            Span<char> compact = hexCharacterCount <= 256
                ? stackalloc char[hexCharacterCount]
                : new char[hexCharacterCount];
            var compactIndex = 0;
            foreach (var character in source) {
                if (!char.IsWhiteSpace(character) && character is not '-' and not ',' and not ':') {
                    compact[compactIndex++] = character;
                }
            }

            bytes = new byte[hexCharacterCount / 2];
            var status = Convert.FromHexString(
                compact, bytes, out var charsConsumed, out var bytesWritten);
            if (status == OperationStatus.Done &&
                charsConsumed == compact.Length &&
                bytesWritten == bytes.Length) {
                return true;
            }

            bytes = [];
            return false;
        }
    }
}
