using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Security.Policy;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace JayTom.Dws.PluginInterface.Utils {

    public static class Utils {
        private static readonly byte[] _dwsKey = PadKey("Hisoka"u8.ToArray(), 16);
        private static readonly byte[] _dwsNonce = Encoding.UTF8.GetBytes("15876396602".PadRight(12, '\0'));
        private static readonly ConcurrentDictionary<(Type Type, string Name), string> DescriptionCache = new();
        /*public static T? GetVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : Visual {
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
        }*/

        public static byte[] PadKey(byte[] key, int length) {
            var paddedKey = new byte[length];
            Array.Copy(key, paddedKey, Math.Min(key.Length, length));
            return paddedKey;
        }

        //加密
        public static string EncryptString(string plainText) {
            try {
                const int tagSize = 16;
                using var aesGcm = new AesGcm(_dwsKey, tagSize);
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var cipherBytes = new byte[plainBytes.Length];
                var tag = new byte[tagSize]; // 用于存储验证标签

                aesGcm.Encrypt(_dwsNonce, plainBytes, cipherBytes, tag); // 提供 tag 参数

                // 将验证标签追加到密文后面
                var cipherWithTag = new byte[cipherBytes.Length + tag.Length];
                Array.Copy(cipherBytes, 0, cipherWithTag, 0, cipherBytes.Length);
                Array.Copy(tag, 0, cipherWithTag, cipherBytes.Length, tag.Length);

                return Convert.ToBase64String(cipherWithTag);
            }
            catch (Exception) {
                return plainText;
            }
        }

        //解密
        public static string DecryptString(string cipherText) {
            try {
                const int tagSize = 16;
                var payload = Convert.FromBase64String(cipherText);
                if (payload.Length < tagSize) {
                    return cipherText;
                }

                var cipherLength = payload.Length - tagSize;
                var decryptedBytes = new byte[cipherLength];
                var cipherBytes = payload.AsSpan(0, cipherLength);
                var tag = payload.AsSpan(cipherLength, tagSize);

                using var aesGcm = new AesGcm(_dwsKey, tagSize);
                aesGcm.Decrypt(_dwsNonce, cipherBytes, tag, decryptedBytes);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception) {
                return cipherText;
            }
        }

        public static T? GetVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : Visual {
            if (parent is null) {
                return null;
            }

            var numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < numVisuals; i++) {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && predicate(typedChild)) {
                    return typedChild;
                }

                var foundChild = GetVisualChild(child, predicate);
                if (foundChild is not null) {
                    return foundChild;
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

        public static Task<bool> IsFileExistsAsync(this string filePath) {
            return Task.FromResult(!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath));
        }

        public static bool IsFileExists(this string filePath) {
            return !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);
        }

        public static BitmapSource ConvertBitmapToBitmapSource(this Image? bitmap) {
            using var memory = new System.IO.MemoryStream();
            bitmap?.Save(memory, ImageFormat.Jpeg);
            memory.Position = 0;
            var bitmapDecoder = BitmapDecoder.Create(memory, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            BitmapSource bitmapSource = bitmapDecoder.Frames[0];
            return bitmapSource;
        }

        public static Image? ConvertImageSourceToImage<T>(this T imageSource) where T : ImageSource {
            if (imageSource is not BitmapSource bitmapSource) return null;
            using var memoryStream = new MemoryStream();
            BitmapEncoder encoder = new BmpBitmapEncoder(); // 选择合适的编码器（这里使用 BMP 编码器）
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);
            using var decodedImage = Image.FromStream(memoryStream);
            return new Bitmap(decodedImage);
        }

        public static byte[]? ImageSourceToByteArray(this ImageSource imageSource) {
            try {
                var bitmapSource = (BitmapSource)imageSource;
                var encoder = new PngBitmapEncoder();
                var memoryStream = new MemoryStream();

                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(memoryStream);

                return memoryStream.ToArray();
            }
            catch (Exception e) {
                return null;
            }
        }

        public static ImageSource? ByteArrayToImageSource(this byte[] byteArray) {
            try {
                var bitmapImage = new BitmapImage();
                using (var stream = new MemoryStream(byteArray)) {
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                }
                bitmapImage.Freeze(); // 冻结图像以提高性能
                return bitmapImage;
            }
            catch (Exception e) {
                return null;
            }
        }

        public static BitmapImage? CreateBitmapImage(Uri uri, int width, int height) {
            try {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = uri;
                image.DecodePixelHeight = height;
                image.DecodePixelWidth = width;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch {
                // ignored
            }

            return null;
        }

        public static string GetDescription(this Enum value) {
            var type = value.GetType();
            var name = value.ToString();
            return DescriptionCache.GetOrAdd((type, name), static key => {
                var field = key.Type.GetField(key.Name);
                var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
                return attribute?.Description ?? key.Name;
            });
        }

        public static TTargetEnum ConvertTo<TTargetEnum>(this Enum sourceEnum) where TTargetEnum : struct, Enum {
            if (!sourceEnum.GetType().IsEnum || !typeof(TTargetEnum).IsEnum) {
                throw new ArgumentException("Both source and target types must be enums.");
            }

            var enumName = sourceEnum.ToString();
            return Enum.TryParse<TTargetEnum>(enumName, out var targetEnum) ? targetEnum : default(TTargetEnum);
        }
    }
}
