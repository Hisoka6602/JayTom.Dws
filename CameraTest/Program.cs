using System;
using Newtonsoft.Json;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

internal class Program {
    private static SemaphoreSlim _takePhotoSlim = new(1);

    private static async Task Main(string[] args) {
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
}