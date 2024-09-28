using System.Text;
using System.Linq;
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
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using JayTom.Dws.Interface.Eshippingit;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;
using JayTom.Dws.Camera.Cameras.VolumeCamera.Hikvision;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR;

internal class Program {

    private static async Task Main(string[] args) {
        var daHuatechNvr = DaHuatechNVR.Instance;

        var (key, value) = await daHuatechNvr.MergeVideos(new[]
            {
               "C:\\Users\\77051\\Desktop\\a.mp4",
               "C:\\Users\\77051\\Desktop\\b.mp4",
           }, "C:\\Users\\77051\\Desktop\\out.mp4",
            30, p => {
                Console.WriteLine(p);
            }, () => false);

        var buildFfmpegArguments = BuildFfmpegArguments(new[]
        {
            "C:\\Users\\Administrator\\Desktop\\1.mp4",
            "C:\\Users\\Administrator\\Desktop\\2.mp4",
            "C:\\Users\\Administrator\\Desktop\\3.mp4",
            "C:\\Users\\Administrator\\Desktop\\4.mp4",
            "C:\\Users\\Administrator\\Desktop\\5.mp4",
            "C:\\Users\\Administrator\\Desktop\\6.mp4",
            "C:\\Users\\Administrator\\Desktop\\7.mp4",
        }, "C:\\Users\\Administrator\\Desktop\\output_4k_2x2grid.mp4");

        Console.WriteLine(buildFfmpegArguments);
        Console.ReadLine();
        return;
        var gwGrayscaleDevice = new GwGrayscaleDevice(new TouchSocketTcpClient(), new TouchSocketTcpServer());
        string hexString = "3A 73 30 36 35 2C 31 2C B3 00 6F 01 2C C5 02 42 03 2C 32 2C CD 00 51 01 2C C9 00 92 01 2C 30 2C 00 00 00 00 2C 00 00 00 00 2C 30 2C 00 00 00 00 2C 00 00 00 00 2C 30 2C 00 00 00 00 2C 00 00 00 00 0D 0A";
        var hexStringToByteArray = HexStringToByteArray(hexString);

        var grayscaleResult = gwGrayscaleDevice.DecodeData(hexStringToByteArray);
        var s = grayscaleResult?.ToString();
        return;
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

    private static string BuildFfmpegArguments(string[] inputFiles, string outputFile) {
        var videoCount = inputFiles.Length;
        // 添加输入文件
        var arguments = inputFiles.Select(file => $"-i \"{file}\"").ToList();
        arguments.Insert(0, "-y");

        List<string> videoIndexList = new();

        // 生成 scale 和 fps 过滤器
        var scaleFilters = new List<string>();
        for (var i = 0; i < videoCount; i++) {
            scaleFilters.Add($"[{i}:v]scale=1920:1080,fps=30[v{i}];");
            videoIndexList.Add($"[v{i}]");
        }

        // 生成占位符
        var blankPlaceholder = "color=black:size=1920x1080[blank]; ";

        var isUseBlankPlaceHolder = scaleFilters.Count is 4 or 6 or 9;

        while (!isUseBlankPlaceHolder) {
            scaleFilters.Add(blankPlaceholder);
            videoIndexList.Add($"[blank]");
            isUseBlankPlaceHolder = scaleFilters.Count is 4 or 6 or 9;
        }

        // 生成布局过滤器
        var layoutFilters = new List<string>();
        switch (videoCount) {
            case 2:
                layoutFilters.Add($"{string.Join("", videoIndexList)}hstack=inputs=2[vout]");
                break;

            case 3:
            case 4:
                layoutFilters.Add($"{string.Join("", videoIndexList.Take(2))}hstack=inputs=2[row1]; {string.Join("", videoIndexList.Skip(2).Take(2))}hstack=inputs=2[row2]; [row1][row2]vstack=inputs=2[vout]");
                break;

            case 5:
            case 6:
                layoutFilters.Add($"{string.Join("", videoIndexList.Take(2))}hstack=inputs=2[row1]; {string.Join("", videoIndexList.Skip(2).Take(2))}hstack=inputs=2[row2]; {string.Join("", videoIndexList.Skip(4).Take(2))}hstack=inputs=2[row3]; [row1][row2]vstack=inputs=2[rowFinal]; [rowFinal][row3]vstack=inputs=2[vout]");
                break;

            case 7:
            case 8:
            case 9:
                layoutFilters.Add($"{string.Join("", videoIndexList.Take(3))}hstack=inputs=3[row1]; {string.Join("", videoIndexList.Skip(3).Take(3))}hstack=inputs=3[row2]; {string.Join("", videoIndexList.Skip(6).Take(3))}hstack=inputs=3[row3]; [row1][row2]vstack=inputs=2[rowFinal]; [rowFinal][row3]vstack=inputs=2[vout]");
                break;

            default:
                throw new ArgumentOutOfRangeException("视频数量超出范围，支持的最大数量为 9。");
        }

        // 合并过滤器
        var filterComplex = string.Join(" ", scaleFilters) + string.Join(" ", layoutFilters);

        // 添加 filter_complex 和输出参数
        arguments.Add($"-filter_complex \"{filterComplex}\"");
        arguments.Add("-map [vout]");
        arguments.Add("-c:v libx264 -preset ultrafast -crf 18 -r 30 -t 30 -shortest -pix_fmt yuv420p");
        arguments.Add($"\"{outputFile}\"");

        return string.Join(" ^\n", arguments);
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

    public static Task<string> GetGksg(string arg0) {
        NLog.LogManager.GetCurrentClassLogger().Info(arg0);
        //判断锁格还是解锁
        if (!string.IsNullOrWhiteSpace(arg0)) {
            var pattern = @"#HEAD::(.*?)#END";
            var match = Regex.Match(arg0, pattern);
            if (match.Success) {
                var content = match.Groups[1].Value;
                var parts = content.Split(new string[] { "::" }, StringSplitOptions.None);

                if (parts.Length >= 4 && int.TryParse(parts[3], out var exit) &&
                    int.TryParse(parts[3], out var status)) {
                }
            }
        }
        arg0.Split("::");
        //_exitItems
        var response = "#MSG::0::成功::||#END";
        return Task.FromResult(response);
    }

    public enum SearchDirection {
        Forward,
        Backward
    }
}