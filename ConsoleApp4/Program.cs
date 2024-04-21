using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Text.Json;
using System.Configuration;
using JayTom.Dws.Plugin.Speech;
using System.Text.RegularExpressions;
using JayTom.Dws.Interface.Eshippingit;
using JayTom.Dws.Camera.Cameras.VolumeCamera.Hikvision;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;

internal class Program {

    private static async Task Main(string[] args) {
        string inputStr = @"\*"; // 假设这是要检查的字符串

        if (IsValidRegexPattern(inputStr)) {
            Console.WriteLine("字符串是一个有效的正则表达式。");
        }
        else {
            Console.WriteLine("字符串不是一个有效的正则表达式。");
        }
        return;
        //var totalMinutes = DateTime.Now.AddMinutes(20).Subtract(DateTime.Now).TotalMinutes;
        var hikvisionVolumeCamera = new HikvisionVolumeCamera();
        var enumerateCameras = await hikvisionVolumeCamera.EnumerateCameras();
        var serializeObject = JsonConvert.SerializeObject(enumerateCameras);
        Console.WriteLine($"体积相机-数量:{enumerateCameras?.Count}:{serializeObject}");

        //HikvisionIndustrialCamera
        var hikvisionIndustrialCamera = new HikvisionIndustrialCamera();
        var cameraInfos = await hikvisionIndustrialCamera.EnumerateCameras();
        var o = JsonConvert.SerializeObject(cameraInfos);
        Console.WriteLine($"工业相机-数量:{cameraInfos?.Count}:{o}");
        Console.ReadLine();
        return;
        var eshippingitApi = new EshippingitApi(null);

        var keyValuePair = await eshippingitApi.SetParameters(new EshippingitApi.ApiParameters());

        var uploadResponse = await eshippingitApi.UploadData("Test123456", 0.1, 0.2, 0.3, 0.4);

        await eshippingitApi.PolicyPush("Test123456", DateTime.Now,
           Image.FromFile("C:\\Users\\77051\\Desktop\\3d0db60b584eecbc97badd57fa61e8d.jpg"));

        var totalMilliseconds = DateTime.Now.Subtract(DateTime.Now.AddSeconds(-10)).TotalMilliseconds;
        return;
        /*string hexString1 = "FC 12 00 25 00 00 01 36";
        string hexString2 = "F9 11 00 0A 00 02 01 18";

        // 将十六进制字符串转换为字节数组
        byte[] bytes1 = HexStringToByteArray(hexString1);
        byte[] bytes2 = HexStringToByteArray(hexString2);
        if (bytes2.Length > 5) {
            var hexString = BitConverter.ToString(new[] { bytes2[4], bytes2[5] })
                 .Replace("-", string.Empty).Replace(" ", string.Empty);
            if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out var number)) {
                var key = number.ToString();
            }
        }
        int value1 = bytes1[bytes1.Length - 4] << 8 | bytes1[bytes1.Length - 3];
        int value2 = bytes2[bytes2.Length - 4] << 8 | bytes2[bytes2.Length - 3];

        Console.WriteLine("第一个字符串的倒数第三第四位十进制值: " + value1);
        Console.WriteLine("第二个字符串的倒数第三第四位十进制值: " + value2);

        var totalMinutes = DateTime.Now.Subtract(DateTime.Now.AddMinutes(20)).TotalMinutes;
        Console.WriteLine(totalMinutes);

        try {
            var s = "{\"code\":1,\"msg\":\"请求成功\",\"version\":null,\"data\":[{\"waybillNo\":\"JT5264019990398\",\"terminalDispatchCode\":\"432 R851-00 307\",\"firstDispatchCode\":\"432\",\"secondDispatchCode\":\"R851-00\",\"thirdlyDispatchCode\":\"307\",\"customerCode\":null,\"interceptor\":2,\"orderType\":2,\"pickNetworkCode\":\"\",\"destinationCode\":\"\",\"extendJson\":\"\",\"codeList\":null},{\"waybillNo\":\"JT5264019990398\",\"terminalDispatchCode\":\"432 R851-00 307\",\"firstDispatchCode\":\"432\",\"secondDispatchCode\":\"R851-00\",\"thirdlyDispatchCode\":\"307\",\"customerCode\":null,\"interceptor\":2,\"orderType\":2,\"pickNetworkCode\":\"\",\"destinationCode\":\"\",\"extendJson\":\"\",\"codeList\":null},{\"waybillNo\":\"JT5264019990398\",\"terminalDispatchCode\":\"902,H346-00,005\",\"firstDispatchCode\":\"902\",\"secondDispatchCode\":\"H346-00\",\"thirdlyDispatchCode\":\"005\",\"customerCode\":null,\"interceptor\":1,\"orderType\":1,\"pickNetworkCode\":\"4579114\",\"destinationCode\":\"510100\",\"extendJson\":\"{\\\"stationCode\\\":\\\"5O\\\"}\",\"codeList\":null}],\"succ\":true,\"fail\":false}";
            var resultContent = Regex.Unescape(s);

            var replace = Regex.Replace(s, @"[\u0000-\u001f\b]", "");
            var reader = new System.Text.Json.Utf8JsonReader(Encoding.UTF8.GetBytes(replace));
            var tryParseValue = JsonDocument.TryParseValue(ref reader, out var document);
            if (tryParseValue && document is not null) {
                var fieldValue = FindFieldValue(document.RootElement, "thirdlyDispatchCode");
                if (fieldValue.HasValue) {
                    var equals = fieldValue.Value.ToString()?.Equals("307");
                    Console.WriteLine(equals);
                }
            }
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }
        Console.ReadLine();*/
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

    private static JsonElement? FindFieldValue(JsonElement root, string fieldName) {
        try {
            var stack = new Stack<JsonElement>();
            stack.Push(root);

            while (stack.Count > 0) {
                var element = stack.Pop();

                switch (element.ValueKind) {
                    case JsonValueKind.Object when element.TryGetProperty(fieldName, out var field):
                        return field;

                    case JsonValueKind.Object: {
                            foreach (var property in element.EnumerateObject()) {
                                stack.Push(property.Value);
                            }

                            break;
                        }
                    case JsonValueKind.Array: {
                            foreach (var arrayElement in element.EnumerateArray()) {
                                stack.Push(arrayElement);
                            }

                            break;
                        }
                }
            }
        }
        catch (Exception e) {
            Console.WriteLine(e.ToString());
        }

        return null;
    }
}