using System;
using HidLibrary;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Hid_Test {

    internal class Program {

        private static void Main(string[] args1) {
            // 枚举所有的 HID 设备
            var devices = HidDevices.Enumerate().ToList();
            var targetDevice = devices.FirstOrDefault(d => d.Capabilities.UsagePage == 1 && d.Capabilities.Usage == 6);
            // 打印所有设备的信息
            Console.WriteLine(JsonConvert.SerializeObject(devices.Select(s => $"{s.Attributes.ProductHexId}--{s.Description}"), Formatting.Indented));
            // var targetDevice = devices.FirstOrDefault(f => f.Description.Contains("系统控制器") && f.Attributes.ProductHexId.Equals("0x1026"));
            if (targetDevice != null) {
                Console.WriteLine("Device found!");

                // 打开设备
                targetDevice.OpenDevice();

                // 注册数据接收事件

                targetDevice.MonitorDeviceEvents = true;
                ReadKeyboardReport(targetDevice);
                /*targetDevice.Read(a => {
                    Console.WriteLine("Data received: " + BitConverter.ToString(a.Data));
                });*/
                // 持续读取数据

                /*while (true) {
                    //var data = targetDevice.ReadReport();

                    var data = targetDevice.Read();

                    if (data != null && data.Data.Length > 0) {
                        Console.WriteLine("Data received: " + BitConverter.ToString(data.Data));
                    }
                    else {
                        Console.WriteLine("No data received");
                    }

                    // 暂停一段时间以避免过于频繁的读取
                    Thread.Sleep(100); // 暂停 100 毫秒
                }*/
            }
            else {
                Console.WriteLine("Device not found");
            }

            Console.WriteLine("aa");
            Console.ReadLine();
        }

        private static void ReadKeyboardReport(HidDevice device) {
            device.ReadReport(report => {
                if (report.Data.Length > 0) {
                    Console.WriteLine($"Report Data: {BitConverter.ToString(report.Data)}");
                }
                else {
                    Console.WriteLine("Empty report received.");
                }

                // 继续读取下一份报告
                ReadKeyboardReport(device);
            });
        }
    }
}