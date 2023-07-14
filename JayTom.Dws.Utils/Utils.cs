using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Utils {

    public static class Utils {
        private const int HWND_BROADCAST = 0xffff;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int SMTO_ABORTIFHUNG = 0x0002;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, int wParam, string lParam, int fuFlags,
            int uTimeout, IntPtr lpdwResult);

        public static string ConvertImageToBase64(this Image image) {
            try {
                using (MemoryStream memoryStream = new MemoryStream()) {
                    image.Save(memoryStream, image.RawFormat);

                    var imageBytes = memoryStream.ToArray();

                    var base64String = Convert.ToBase64String(imageBytes);

                    return base64String;
                }
            }
            catch {
                return string.Empty;
            }
        }

        public static Image ConvertBase64ToImage(this string base64String) {
            try {
                var imageBytes = Convert.FromBase64String($"{base64String}");

                using (MemoryStream memoryStream = new MemoryStream(imageBytes)) {
                    // 将字节数组转换为Image对象
                    return Image.FromStream(memoryStream);
                }
            }
            catch {
                return null;
            }
        }

        public static Image AddTextWatermark(this Image image, string watermarkText, Color brushColor,
            int fontSize = 70, string familyName = "Arial") {
            try {
                using var graphics = Graphics.FromImage(image);
                using var font = new Font(familyName, fontSize, FontStyle.Bold);
                using var brush = new SolidBrush(brushColor);
                graphics.DrawString(watermarkText, font, brush, new PointF(10, 10));
            }
            catch (Exception) {
                return image;
            }

            return image;
        }

        public static KeyValuePair<bool, string> AddSystemEnvironmentVariable(string path, string variableName = "Path") {
            try {
                using (RegistryKey? environmentKey = Registry.CurrentUser.OpenSubKey(@"Environment", true)) {
                    if (environmentKey != null) {
                        var currentValue = environmentKey.GetValue(variableName) as string;

                        // 检查是否已经包含 Percipio 路径
                        if (string.IsNullOrEmpty(currentValue) || !currentValue.Contains(path)) {
                            // 在现有值的末尾添加 Percipio 路径，并使用分号进行分隔
                            var newValue = currentValue + path;

                            environmentKey.SetValue(variableName, newValue);

                            SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, 0, "Environment",
                                SMTO_ABORTIFHUNG, 5000, IntPtr.Zero);

                            return new KeyValuePair<bool, string>(true, $"路径已成功添加到环境变量中");
                        }
                        else {
                            return new KeyValuePair<bool, string>(true, $"环境变量中已存在{path} 路径，无需添加。");
                        }
                    }
                    else {
                        return new KeyValuePair<bool, string>(false, "无法打开环境变量注册表项");
                    }
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public static void SetPath(params string[] paths)  // 设置当前进程的目录环境变量
        {
            var pathSeparator = System.IO.Path.PathSeparator;
            var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            if (path == null) {
                return;
            }

            path = paths.Aggregate(path, (current, t) => current + ";" + t);
            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
        }
    }
}