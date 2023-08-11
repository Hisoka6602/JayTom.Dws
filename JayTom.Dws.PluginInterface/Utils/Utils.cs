using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Drawing;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace JayTom.Dws.PluginInterface.Utils {

    public static class Utils {

        public static T? GetVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : Visual {
            var numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < numVisuals; i++) {
                var v = VisualTreeHelper.GetChild(parent, i);
                if (v is not T child) {
                    child = GetVisualChild(v, predicate);
                    if (child is not null) {
                        return child;
                    }
                }
                else {
                    if (predicate(child)) {
                        return child;
                    }
                }
            }

            return null;
        }

        public static T? GetParentContainer<T>(DependencyObject obj, Func<T, bool> predicate) where T : Visual {
            var parent = VisualTreeHelper.GetParent(obj);

            while (parent != null) {
                if (parent is T typedParent) {
                    if (predicate(typedParent)) {
                        return typedParent;
                    }
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        public static async Task<bool> IsFileExistsAsync(this string filePath) {
            return await Task.Run(() => !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath));
        }

        public static bool IsFileExists(this string filePath) {
            return !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);
        }

        public static BitmapSource ConvertBitmapToBitmapSource(this Image bitmap) {
            BitmapSource bitmapSource;
            using var memory = new System.IO.MemoryStream();
            bitmap.Save(memory, ImageFormat.Bmp);
            memory.Position = 0;
            var bitmapDecoder = BitmapDecoder.Create(memory, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            bitmapSource = bitmapDecoder.Frames[0];
            return bitmapSource;
        }

        public static Image? ConvertImageSourceToImage<T>(this T imageSource) where T : ImageSource {
            if (imageSource is not BitmapSource bitmapSource) return null;
            using var memoryStream = new MemoryStream();
            BitmapEncoder encoder = new BmpBitmapEncoder(); // 选择合适的编码器（这里使用 BMP 编码器）
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);
            return Image.FromStream(memoryStream);
        }
    }
}