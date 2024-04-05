using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Management;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using Formatting = System.Xml.Formatting;

namespace JayTom.Dws.License {

    public class LicenseManager {
        //生成授权文件
        //验证授权文件
        //获取网络时间
        /*public static void GenerateLicenseFile(string privateKey, string userName, TimeSpan expirationDate) {
            try {
                var licenseData = new LicenseData {
                    UserName = "userName",
                    ExpirationDate = DateTime.UtcNow.AddDays(expirationDate.TotalDays)
                };

                var serializerSettings = new JsonSerializerSettings {
                    Formatting = (Newtonsoft.Json.Formatting)Formatting.None,
                    NullValueHandling = NullValueHandling.Ignore
                };
                var dataToSign = JsonConvert.SerializeObject(licenseData, serializerSettings);
                var dataBytes = Encoding.UTF8.GetBytes(dataToSign);

                using (var rsa = RSA.Create()) {
                    // 获取私钥的XML字符串表示
                    var privateKeyXml = rsa.ToXmlString(true); // 参数为true以包含私钥
                    // 获取公钥的XML字符串表示
                    var publicKeyXml = rsa.ToXmlString(false); // 参数为false以只包含公钥
                    //rsa.FromXmlString(privateKey);
                    rsa.FromXmlString(privateKeyXml);
                    var signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    licenseData.Signature = Convert.ToBase64String(signatureBytes);
                }

                var fileContent = JsonConvert.SerializeObject(licenseData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText("license.json", fileContent);
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }*/

        /// <summary>
        /// 加密字符串
        /// </summary>
        /// <param name="data"></param>
        /// <param name="privateKeyXml"></param>
        /// <returns></returns>
        public static byte[] GenerateAuthorizationFile(string data, string privateKeyXml) {
            byte[] encryptedData;
            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml);
            var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
            encryptedData = rsa.Encrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
            return encryptedData;
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        /// <param name="encryptedData"></param>
        /// <param name="publicKeyXml"></param>
        /// <returns></returns>

        public static string DecryptAuthorizationByte(byte[] encryptedData, string publicKeyXml) {
            string decryptedData;
            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKeyXml);
            var decryptedBytes = rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
            decryptedData = System.Text.Encoding.UTF8.GetString(decryptedBytes);
            return decryptedData;
        }

        /// <summary>
        /// 对称加密(生成)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="publicKeyXml"></param>
        /// <param name="filePath"></param>
        public static void GenerateAuthorizationFile(LicenseData data, string publicKeyXml, string filePath) {
            try {
                byte[] encryptedData;
                using (var rsa = RSA.Create()) {
                    rsa.FromXmlString(publicKeyXml);
                    var dataBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
                    encryptedData = rsa.Encrypt(dataBytes, RSAEncryptionPadding.Pkcs1);
                }

                using (var aes = Aes.Create()) {
                    aes.GenerateKey();
                    aes.GenerateIV();
                    var key = aes.Key;
                    var iv = aes.IV;

                    using (var encryptor = aes.CreateEncryptor(key, iv)) {
                        var encryptedBytes = encryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

                        using (var outputStream = new MemoryStream())
                        using (var binaryWriter = new BinaryWriter(outputStream)) {
                            binaryWriter.Write(key.Length);
                            binaryWriter.Write(key);
                            binaryWriter.Write(iv.Length);
                            binaryWriter.Write(iv);
                            binaryWriter.Write(encryptedBytes.Length);
                            binaryWriter.Write(encryptedBytes);

                            File.WriteAllBytes(filePath, outputStream.ToArray());
                        }
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// 对称加密(解析)
        /// </summary>
        /// <param name="privateKeyXml"></param>
        /// <param name="filePath"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static KeyValuePair<bool, string> DecryptAuthorizationFile(string privateKeyXml, string filePath, out LicenseData? data) {
            byte[] encryptedData;
            byte[] key;
            byte[] iv;
            try {
                using (var inputStream = new MemoryStream(File.ReadAllBytes(filePath)))
                using (var binaryReader = new BinaryReader(inputStream)) {
                    var keyLength = binaryReader.ReadInt32();
                    key = binaryReader.ReadBytes(keyLength);
                    var ivLength = binaryReader.ReadInt32();
                    iv = binaryReader.ReadBytes(ivLength);
                    var encryptedLength = binaryReader.ReadInt32();
                    encryptedData = binaryReader.ReadBytes(encryptedLength);
                }

                using (var aes = Aes.Create()) {
                    aes.Key = key;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(key, iv)) {
                        var decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

                        using (var rsa = RSA.Create()) {
                            rsa.FromXmlString(privateKeyXml);
                            var decryptedBytes = rsa.Decrypt(decryptedData, RSAEncryptionPadding.Pkcs1);
                            var s = Encoding.UTF8.GetString(decryptedBytes);
                            data = JsonConvert.DeserializeObject<LicenseData>(s);

                            if (data is null) {
                                return new KeyValuePair<bool, string>(false, "授权文件解析错误!");
                            }
                            //机器码不匹配则不通过
                            if (!data.MachineCode.Equals(GenerateMachineCode())) {
                                return new KeyValuePair<bool, string>(false, "机器码不匹配!");
                            }
                            //如果过期时间大于当前时间则不通过
                            if (data.ExpirationDate.CompareTo(DateTime.Now) <= 0) {
                                return new KeyValuePair<bool, string>(false, "授权已过期!");
                            }

                            if (data.CreationTime.Subtract(DateTime.Now).TotalMinutes >= 10) {
                                return new KeyValuePair<bool, string>(false, "授权时间异常!");
                            }
                            return new KeyValuePair<bool, string>(true, "授权正常");
                        }
                    }
                }
            }
            catch (Exception e) {
                data = null;
            }
            return new KeyValuePair<bool, string>(false, "授权文件异常!");
        }

        public static void GenerateKeyPair(out string publicKeyXml, out string privateKeyXml) {
            using var rsa = RSA.Create();
            publicKeyXml = rsa.ToXmlString(false);
            privateKeyXml = rsa.ToXmlString(true);
        }

        public static string GenerateMachineCode() {
            var cpuSerialNumber = string.Empty;
            var hardDiskId = string.Empty;
            string machineName;
            string versionString;
            var machineCode = string.Empty;
            try {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                var collection = searcher.Get();
                foreach (var o in collection) {
                    var obj = (ManagementObject)o;
                    cpuSerialNumber += obj?["ProcessorId"].ToString();
                }
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                collection = searcher.Get();
                foreach (var o in collection) {
                    var obj = (ManagementObject)o;
                    var interfaceType = obj["InterfaceType"]?.ToString();
                    if (interfaceType != null && !interfaceType.Contains("USB")) {
                        hardDiskId += obj?["SerialNumber"].ToString();
                    }
                    else {
                        var s = obj?["SerialNumber"].ToString();
                        Console.WriteLine(s);
                    }
                }

                machineName = Environment.MachineName;
                versionString = Environment.OSVersion.VersionString;

                machineCode = $"{cpuSerialNumber}{hardDiskId}{machineName}{versionString}";

                using var md5 = MD5.Create();
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes($"{machineCode}Hisoka"));
                var strResult = BitConverter.ToString(result);
                machineCode = strResult.Replace("-", "");
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            return machineCode;
        }

        public static void GenerateAuthorizationFile1(LicenseData data, string publicKeyXml, string privateKeyXml, string filePath) {
            try {
                byte[] encryptedData;
                using (var rsa = RSA.Create()) {
                    rsa.FromXmlString(publicKeyXml);
                    var dataBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
                    encryptedData = rsa.Encrypt(dataBytes, RSAEncryptionPadding.Pkcs1);
                }

                byte[] key, iv;
                using (var aes = Aes.Create()) {
                    aes.GenerateKey();
                    aes.GenerateIV();
                    key = aes.Key;
                    iv = aes.IV;

                    using (var encryptor = aes.CreateEncryptor(key, iv)) {
                        var encryptedBytes = encryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

                        using (var outputStream = new MemoryStream())
                        using (var binaryWriter = new BinaryWriter(outputStream)) {
                            binaryWriter.Write(key.Length);
                            binaryWriter.Write(key);
                            binaryWriter.Write(iv.Length);
                            binaryWriter.Write(iv);
                            binaryWriter.Write(encryptedBytes.Length);
                            binaryWriter.Write(encryptedBytes);

                            binaryWriter.Write(privateKeyXml);

                            File.WriteAllBytes(filePath, outputStream.ToArray());
                        }
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        public static bool DecryptAuthorizationFile1(string filePath, out LicenseData? data) {
            byte[] encryptedData, key, iv;
            string privateKeyXml;
            try {
                using (var inputStream = new MemoryStream(File.ReadAllBytes(filePath)))
                using (var binaryReader = new BinaryReader(inputStream)) {
                    var keyLength = binaryReader.ReadInt32();
                    key = binaryReader.ReadBytes(keyLength);
                    var ivLength = binaryReader.ReadInt32();
                    iv = binaryReader.ReadBytes(ivLength);
                    var encryptedLength = binaryReader.ReadInt32();
                    encryptedData = binaryReader.ReadBytes(encryptedLength);

                    privateKeyXml = binaryReader.ReadString();
                }

                using (var aes = Aes.Create()) {
                    aes.Key = key;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(key, iv)) {
                        var decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

                        using (var rsa = RSA.Create()) {
                            rsa.FromXmlString(privateKeyXml);
                            var decryptedBytes = rsa.Decrypt(decryptedData, RSAEncryptionPadding.Pkcs1);
                            var s = Encoding.UTF8.GetString(decryptedBytes);
                            data = JsonConvert.DeserializeObject<LicenseData>(s);

                            if (data is null) {
                                return false;
                            }
                            //机器码不匹配则不通过
                            if (!data.MachineCode.Equals(GenerateMachineCode())) {
                                return false;
                            }
                            //如果过期时间大于当前时间则不通过
                            if (data.ExpirationDate.CompareTo(DateTime.Now) <= 0) {
                                return false;
                            }

                            return true;
                        }
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
                data = null;
            }
            return false;
        }

        public static KeyValuePair<bool, string> GenerateAuthorizationFile(LicenseData data, string publicKeyXml, string privateKeyXml, string filePath) {
            try {
                //byte[] encryptedData;
                var encryptedData = new List<byte>();
                using (var rsa = RSA.Create()) {
                    rsa.FromXmlString(publicKeyXml);
                    var dataBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
                    //encryptedData = rsa.Encrypt(dataBytes, RSAEncryptionPadding.Pkcs1);
                    // 分块加密
                    var blockSize = rsa.KeySize / 8 - 11;

                    for (var i = 0; i < dataBytes.Length; i += blockSize) {
                        var block = new ReadOnlySpan<byte>(dataBytes, i, Math.Min(blockSize, dataBytes.Length - i));
                        var encryptedBlock = rsa.Encrypt(block.ToArray(), RSAEncryptionPadding.Pkcs1);
                        encryptedData.AddRange(encryptedBlock);
                    }
                }

                byte[] key, iv, encryptedPrivateKey;
                using (var aes = Aes.Create()) {
                    aes.GenerateKey();
                    aes.GenerateIV();
                    key = aes.Key;
                    iv = aes.IV;

                    using (var encryptor = aes.CreateEncryptor(key, iv)) {
                        var privateKeyBytes = Encoding.UTF8.GetBytes(privateKeyXml);
                        encryptedPrivateKey = encryptor.TransformFinalBlock(privateKeyBytes, 0, privateKeyBytes.Length);
                    }
                }

                using (var outputStream = new MemoryStream())
                using (var binaryWriter = new BinaryWriter(outputStream)) {
                    binaryWriter.Write(encryptedPrivateKey.Length);
                    binaryWriter.Write(encryptedPrivateKey);
                    binaryWriter.Write(key.Length);
                    binaryWriter.Write(key);
                    binaryWriter.Write(iv.Length);
                    binaryWriter.Write(iv);
                    binaryWriter.Write(encryptedData.Count);
                    binaryWriter.Write(encryptedData.ToArray());

                    File.WriteAllBytes(filePath, outputStream.ToArray());
                    return new KeyValuePair<bool, string>(true, string.Empty);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public static KeyValuePair<bool, string> DecryptAuthorizationFile(string filePath, out LicenseData? data) {
            byte[] encryptedData, key, iv, encryptedPrivateKey;
            try {
                using (var inputStream = new MemoryStream(File.ReadAllBytes(filePath)))
                using (var binaryReader = new BinaryReader(inputStream)) {
                    var privateKeyLength = binaryReader.ReadInt32();
                    encryptedPrivateKey = binaryReader.ReadBytes(privateKeyLength);
                    var keyLength = binaryReader.ReadInt32();
                    key = binaryReader.ReadBytes(keyLength);
                    var ivLength = binaryReader.ReadInt32();
                    iv = binaryReader.ReadBytes(ivLength);
                    var encryptedLength = binaryReader.ReadInt32();
                    encryptedData = binaryReader.ReadBytes(encryptedLength);
                }

                using (var aes = Aes.Create()) {
                    aes.Key = key;
                    aes.IV = iv;
                    using (var decryptor = aes.CreateDecryptor(key, iv)) {
                        var privateKeyBytes = decryptor.TransformFinalBlock(encryptedPrivateKey, 0, encryptedPrivateKey.Length);
                        var privateKeyXml = Encoding.UTF8.GetString(privateKeyBytes);

                        using (var rsa = RSA.Create()) {
                            rsa.FromXmlString(privateKeyXml);
                            //var decryptedData = rsa.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);

                            var blockSize = rsa.KeySize / 8;
                            var decryptedData = new List<byte>();

                            for (var i = 0; i < encryptedData.Length; i += blockSize) {
                                var remainingBytes = Math.Min(blockSize, encryptedData.Length - i);
                                var block = new byte[remainingBytes];
                                Buffer.BlockCopy(encryptedData, i, block, 0, remainingBytes);
                                var decryptedBlock = rsa.Decrypt(block, RSAEncryptionPadding.Pkcs1);
                                decryptedData.AddRange(decryptedBlock);
                            }

                            var decryptedString = Encoding.UTF8.GetString(decryptedData.ToArray());
                            //var decryptedString = Encoding.UTF8.GetString(decryptedData);

                            data = JsonConvert.DeserializeObject<LicenseData>(decryptedString);

                            if (data is null) {
                                return new KeyValuePair<bool, string>(false, "授权文件解析错误!");
                            }
                            //机器码不匹配则不通过
                            if (!data.MachineCode.Equals(GenerateMachineCode())) {
                                return new KeyValuePair<bool, string>(false, "机器码不匹配!");
                            }
                            //如果过期时间大于当前时间则不通过
                            if (data.ExpirationDate.CompareTo(DateTime.Now) <= 0) {
                                return new KeyValuePair<bool, string>(false, "授权已过期!");
                            }
                            if (data.CreationTime.Subtract(DateTime.Now).TotalMinutes >= 10) {
                                return new KeyValuePair<bool, string>(false, "授权时间异常!");
                            }
                            if (!data.IsAvailable) {
                                return new KeyValuePair<bool, string>(false, "授权码已冻结!");
                            }
                            return new KeyValuePair<bool, string>(true, "授权正常");
                        }
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
                data = null;
            }
            return new KeyValuePair<bool, string>(false, "授权文件异常!");
        }
    }
}