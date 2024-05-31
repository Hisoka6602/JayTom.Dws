using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Text.Json;
using FluentFTP.Helpers;
using System.Configuration;
using JayTom.Dws.Interface;
using JayTom.Dws.Plugin.Speech;
using JayTom.Dws.Interface.Post;
using JayTom.Dws.Interface.geek_;
using System.Text.RegularExpressions;
using JayTom.Dws.Interface.Eshippingit;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;
using JayTom.Dws.Camera.Cameras.VolumeCamera.Hikvision;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;

internal class Program {

    private static async Task Main(string[] args) {
        await new PostApi(null).UploadData("0123456789", 0, 0);

        var localTime = Convert.ToDateTime("2024-05-21T12:17:17Z").ToLocalTime();

        return;
        //return Task.CompletedTask;
        var gwGrayscaleDevice = new GwGrayscaleDevice();
        string hexString = "3A 73 30 31 37 2C 30 2C 00 00 00 00 2C 00 00 00 00 2C 32 2C D1 00 E1 01 2C AF 00 78 01 2C 30 2C 00 00 00 00 2C 00 00 00 00 2C 30 2C 00 00 00 00 2C 00 00 00 00 2C 30 2C 00 00 00 00 2C 00 00 00 00 0D 0A";
        var hexStringToByteArray = HexStringToByteArray(hexString);

        var grayscaleResult = gwGrayscaleDevice.DecodeData(hexStringToByteArray);

        gwGrayscaleDevice.SendCarNumber(17, new CancellationToken());
        Console.ReadLine();
        var description = SortingExceptionReturnType.VehicleNumberMismatch.GetDescription();
        // return Task.CompletedTask;
        var totalMicroseconds = DateTime.Now.Subtract(DateTime.Now.AddSeconds(-1)).TotalMicroseconds;
        // return Task.CompletedTask;
        /*try {
            var s =
                "{\"code\":1,\"msg\":\"请求成功\",\"version\":null,\"data\":[{\"waybillNo\":\"JT2073687636814\",\"terminalDispatchCode\":\"432,K848-00,027\",\"firstDispatchCode\":\"432\",\"secondDispatchCode\":\"K848-00\",\"thirdlyDispatchCode\":\"027\",\"customerCode\":null,\"interceptor\":1,\"orderType\":1,\"pickNetworkCode\":\"2596149\",\"destinationCode\":\"330700\",\"extendJson\":\"{\\\"stationCode\\\":\\\"L6\\\"}\",\"codeList\":null},{\"waybillNo\":\"JT2073687636814\",\"terminalDispatchCode\":\"432,K848-00,027\",\"firstDispatchCode\":\"432\",\"secondDispatchCode\":\"K848-00\",\"thirdlyDispatchCode\":\"027\",\"customerCode\":null,\"interceptor\":1,\"orderType\":1,\"pickNetworkCode\":\"2596149\",\"destinationCode\":\"330700\",\"extendJson\":\"{\\\"stationCode\\\":\\\"L6\\\"}\",\"codeList\":null},{\"waybillNo\":\"JT2073687636814\",\"terminalDispatchCode\":\"432,K848-00,027\",\"firstDispatchCode\":\"432\",\"secondDispatchCode\":\"K848-00\",\"thirdlyDispatchCode\":\"027\",\"customerCode\":null,\"interceptor\":2,\"orderType\":1,\"pickNetworkCode\":\"2596149\",\"destinationCode\":\"330700\",\"extendJson\":\"{\\\"stationCode\\\":\\\"L6\\\"}\",\"codeList\":null}],\"succ\":true,\"fail\":false}";
            var resultContent = Regex.Unescape(s);

            var replace = Regex.Replace(s, @"[\u0000-\u001f\b]", "");
            var reader = new System.Text.Json.Utf8JsonReader(Encoding.UTF8.GetBytes(replace));
            var tryParseValue = JsonDocument.TryParseValue(ref reader, out var document);
            if (tryParseValue && document is not null) {
                var fieldValue = FindFieldValue(document.RootElement,
                    "interceptor", SearchDirection.Forward);
                if (fieldValue.HasValue) {
                    var equals = fieldValue.Value.ToString()?.Equals("1");
                    Console.WriteLine(equals);
                }
            }
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }*/
        Console.ReadLine();
    }

    private static bool IsValidRegexPattern(string pattern) {
        try {
            Regex.Match("在", pattern);
            var isMatch = Regex.IsMatch("在", pattern);
            return true;
        }
        catch (ArgumentException) {
            return false;
        }
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

    private static JsonElement? FindFieldValue(JsonElement root, string fieldName, SearchDirection direction = SearchDirection.Forward) {
        try {
            var stack = new Stack<JsonElement>();
            stack.Push(root);

            JsonElement? lastMatch = null;

            while (stack.Count > 0) {
                var element = stack.Pop();

                switch (element.ValueKind) {
                    case JsonValueKind.Object when element.TryGetProperty(fieldName, out var field):
                        lastMatch = field;
                        if (direction == SearchDirection.Forward) {
                            continue;
                        }
                        break;

                    case JsonValueKind.Object: {
                            foreach (var property in element.EnumerateObject()) {
                                stack.Push(direction == SearchDirection.Forward
                                    ? property.Value
                                    : property.Value.Clone());
                            }

                            break;
                        }
                    case JsonValueKind.Array: {
                            var array = element.EnumerateArray().ToList();
                            if (direction == SearchDirection.Backward) {
                                array.Reverse();
                            }
                            foreach (var arrayElement in array) {
                                stack.Push(arrayElement);
                            }

                            break;
                        }
                }
            }

            return lastMatch;
        }
        catch (Exception e) {
            Console.WriteLine(e.ToString());
        }

        return null;
    }

    public enum SearchDirection {
        Forward,
        Backward
    }
}