using System;
using System.Text;
using Newtonsoft.Json;
using JayTom.Dws.Camera;
using JayTom.Dws.License;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Security.Cryptography;
using JayTom.Dws.Interface.Routdata;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using LicenseManager = JayTom.Dws.License.LicenseManager;

internal class Program {
    private static SemaphoreSlim _takePhotoSlim = new(1);

    private static async Task Main(string[] args) {
        var ldkjApi = new RoutdataApi(null);
        var uploadResponse = await ldkjApi.UploadData("9883813791427", 0);

        await Task.Delay(50000);
        return;
        //var usbBarCodeReader = new UsbBarCodeReader().EnumerateCameras();

        // 创建RSA实例
        /*using (RSA rsa = RSA.Create()) {
            // 获取RSA的公钥和私钥
            string publicKeyXml = rsa.ToXmlString(false);
            string privateKeyXml = rsa.ToXmlString(true);

            // 要加密的数据
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes("Hello, RSA!");

            // 加密数据
            byte[] encryptedData = EncryptData(dataToEncrypt, publicKeyXml);

            // 解密数据
            byte[] decryptedData = DecryptData(encryptedData, privateKeyXml);

            // 显示结果
            Console.WriteLine("Original Data: " + Encoding.UTF8.GetString(dataToEncrypt));
            Console.WriteLine("Encrypted Data: " + Convert.ToBase64String(encryptedData));
            Console.WriteLine("Decrypted Data: " + Encoding.UTF8.GetString(decryptedData));

            // 显示密钥字符串
            Console.WriteLine("Public Key: " + publicKeyXml);
            Console.WriteLine("Private Key: " + privateKeyXml);
        }
        return;*/

        //获取对称密钥
        JayTom.Dws.License.LicenseManager.GenerateKeyPair(out var publicKeyXml, out var privateKeyXml);
        //加密
        JayTom.Dws.License.LicenseManager.GenerateAuthorizationFile(new LicenseData() {
            ExpirationDate = DateTime.Now.AddDays(-1),
            MachineCode = LicenseManager.GenerateMachineCode(),
            Signature = "ABCDEFGHIJKLM",
            UserName = "AAAAAAAAA"
        }, publicKeyXml,
            "..\\License.key");
        //写出解密密钥
        //await File.WriteAllTextAsync("..\\License.ini", privateKeyXml);
        //privateKeyXml = await File.ReadAllTextAsync("..\\License.ini");
        //解密
        var decryptAuthorizationFile = JayTom.Dws.License.LicenseManager.DecryptAuthorizationFile(privateKeyXml, "..\\License.key", out var linData);
        return;

        /*var securityCamera = new DaHuatechSecurityCamera() {
             CameraConnectionParameters = JsonConvert.SerializeObject(
                 new SecurityCameraConnectionParameters {
                     Username = "admin",
                     Password = "Aa12345678"
                 })
         };
         for (int i = 0; i < 5; i++) {
             securityCamera = new DaHuatechSecurityCamera() {
                 CameraConnectionParameters = JsonConvert.SerializeObject(
                     new SecurityCameraConnectionParameters {
                         Username = "admin",
                         Password = "Aa12345678"
                     })
             };
         }
         securityCamera.PhotoTaken += async delegate (object? sender, PhotoTakenEventArgs eventArgs) {
             if (eventArgs.Image is not null) {
                 try {
                     await _takePhotoSlim.WaitAsync();
                     eventArgs.Image?.Save(
                         $"{System.IO.Directory.GetCurrentDirectory()}\\Image\\{eventArgs.Barcode}.{eventArgs.BarcodeTimestamp}.jpg");
                     //写文件
                     eventArgs.Image?.Dispose();
                 }
                 catch (Exception e) {
                     Console.WriteLine(e);
                 }
                 finally {
                     _takePhotoSlim.Release();
                 }
             }
         };
         securityCamera.CameraExceptionOccurred += delegate (object? sender, CameraExceptionEventArgs eventArgs) {
             Console.WriteLine(JsonConvert.SerializeObject(eventArgs.Exception));
         };

         var initialize = await securityCamera.Initialize(new CameraInfo {
             IpAddress = "192.168.31.108",
             Port = 37777,
             ConnectionType = CameraConnectionType.Ethernet
         });

         await securityCamera.Start(null);
         for (var i = 0; i < 10; i++) {
             await securityCamera.TakePhotoAsync($"No0000000000{i + 1}__", DateTimeOffset.Now.ToUnixTimeMilliseconds());
         }*/

        var baseDaHuatech = BaseDaHuatech.CreateInstance();

        var deviceNetInfoExes = await BaseDaHuatech.EnumDevices();

        if (deviceNetInfoExes != null) {
            foreach (var deviceNetInfoExe in deviceNetInfoExes) {
                var daHuatech = BaseDaHuatech.CreateInstance();
                var (key, value) = await daHuatech.LogIn(deviceNetInfoExe.szSerialNo,
                    "admin", "Aa12345678");
                if (!key) {
                    Console.WriteLine($"登录失败:{value}");
                }
                else {
                    Console.WriteLine($"登录成功:{value:X}");
                }
                daHuatech.RegisterImageCallback(deviceNetInfoExe.szSerialNo, async image => {
                    if (image is not null) {
                        try {
                            await _takePhotoSlim.WaitAsync();
                            image?.Save(
                                $"{System.IO.Directory.GetCurrentDirectory()}\\Image\\{deviceNetInfoExe.szSerialNo}.{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.jpg");
                            //写文件
                            image?.Dispose();
                        }
                        catch (Exception e) {
                            Console.WriteLine(e);
                        }
                        finally {
                            _takePhotoSlim.Release();
                        }
                    }
                });
                Task.Run(async () => {
                    for (var i = 0; i < 10; i++) {
                        await Task.Delay(800);
                        var (b, s) = await daHuatech.GetRealtimeImage(deviceNetInfoExe.szSerialNo);
                        if (!b) {
                            Console.WriteLine(s);
                        }
                    }
                });
            }
        }

        Console.WriteLine("Hello, World!");
        Console.ReadLine();
        GC.Collect();
        Console.ReadLine();
    }

    private static byte[] EncryptData(byte[] data, string publicKeyXml) {
        using (RSA rsa = RSA.Create()) {
            rsa.FromXmlString(publicKeyXml);
            return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
        }
    }

    private static byte[] DecryptData(byte[] data, string privateKeyXml) {
        using (RSA rsa = RSA.Create()) {
            rsa.FromXmlString(privateKeyXml);
            return rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
        }
    }
}