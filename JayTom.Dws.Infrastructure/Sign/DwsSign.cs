using System.Text;
using JayTom.Dws.Domain.Sign;
using System.Security.Cryptography;

namespace JayTom.Dws.Infrastructure.Sign {

    public class DwsSign : ISign {

        public bool IsValid(string md5Content, string secret, string content, string constkey) {
            if (md5Content.Length < 16) return false;
            // DWS-HEX-COMPACT: 签名校验必须兼容既有的无分隔符摘要格式。
            var sign = Convert.ToHexString(MD5.HashData(
                Encoding.UTF8.GetBytes(secret + content + constkey)));

            return sign.Equals(md5Content, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsValid(string md5Content, string secret, string content) {
            return IsValid(md5Content, secret, content, "Yszn");
        }

        public bool IsValid(string md5Content, string secret, DateTime validTime, string content) {
            var isValid = IsValid(md5Content, secret, $"{content}{validTime:yyyy-MM-dd HH:mm:00}", "Yszn");
            if (!isValid) {
                validTime = validTime.AddMinutes(1);
                isValid = IsValid(md5Content, secret, $"{content}{validTime:yyyy-MM-dd HH:mm:00}", "Yszn");
            }
            return isValid;
        }
    }
}
