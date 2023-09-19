using System.Text;
using System.Drawing;
using static Program;
using Newtonsoft.Json;
using System.Reflection.Emit;
using System.Drawing.Imaging;
using Image = System.Drawing.Image;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

internal class Program {

    #region 声明委托类型

    // 声明委托类型
    /*[UnmanagedFunctionPointer(CallingConvention.Cdecl)] // 根据实际情况选择调用约定
    private delegate void ReinitFsaDecoderDelegate(int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int FsaDecoderEnableSymbolDelegate(int symbol, int enable);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FsaDecoderSetStrengthDelegate(int level);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FsaDecoderSetTimeoutConfigDelegate(int time1, int time2, int time3);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FsaDecoderSetMaxBarcodeNumDelegate(int num);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int FsaDecodeDelegate(IntPtr pImage); // 参数类型改为IntPtr

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int FsaDecoderGetResultNumDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int FsaDecoderGetResultLengthDelegate(int index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int FsaDecoderGetResultBoundsDelegate(IntPtr bounds, int index); // 参数类型改为IntPtr

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int FsaDecoderGetResultStringDelegate(IntPtr data, int index); // 参数类型改为IntPtr

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void UninstallFsaDecoderDelegate();*/

    #endregion 声明委托类型

    #region Dll 函数

    [DllImport("srDecoderDll.dll")]
    private static extern IntPtr ReinitFsaDecoder(int width, int height);

    [DllImport("srDecoderDll.dll")]
    private static extern int FsaDecoderEnableSymbol(int symbol, int enable);

    [DllImport("srDecoderDll.dll")]
    private static extern void FsaDecoderSetStrength(int level);

    [DllImport("srDecoderDll.dll")]
    private static extern void FsaDecoderSetTimeoutConfig(int time1, int time2, int time3);

    [DllImport("srDecoderDll.dll")]
    private static extern void FsaDecoderSetMaxBarcodeNum(int num);

    [DllImport("srDecoderDll.dll")]
    private static extern int FsaDecode(byte[] pImage);

    [DllImport("srDecoderDll.dll")]
    private static extern int FsaDecoderGetResultNum();

    [DllImport("srDecoderDll.dll")]
    private static extern int FsaDecoderGetResultLength(int index);

    [DllImport("srDecoderDll.dll")]
    private static extern int FsaDecoderGetResultBounds(ref BoundsInfo bounds, int index);

    [DllImport("srDecoderDll.dll")]
    private static extern int FsaDecoderGetResultString(IntPtr data, int index);

    [DllImport("srDecoderDll.dll")]
    private static extern void UninstallFsaDecoder();

    #endregion Dll 函数

    #region 结构体

    [StructLayout(LayoutKind.Sequential)]
    private struct BoundsInfo {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    private struct ResultEntry {
        public int length;
        public IntPtr data;
    }

    #endregion 结构体

    #region 宏定义

    public class SymbolType {
        public const int SYMBOL_UPCA = 1;
        public const int SYMBOL_UPCE0 = 2;
        public const int SYMBOL_EAN8 = 3;
        public const int SYMBOL_EAN13 = 4;
        public const int SYMBOL_TELEPEN = 5;
        public const int SYMBOL_MATRIX25 = 6;
        public const int SYMBOL_CHINAPOST = 7;
        public const int SYMBOL_CODE11 = 8;
        public const int SYMBOL_CODABAR = 9;
        public const int SYMBOL_CODABLOCK_A = 10;
        public const int SYMBOL_CODABLOCK_F = 11;
        public const int SYMBOL_INTER25 = 12;
        public const int SYMBOL_CODE128 = 13;
        public const int SYMBOL_GS1_128 = 14;
        public const int SYMBOL_ISBT128 = 15;
        public const int SYMBOL_CODE93 = 16;
        public const int SYMBOL_CODE39 = 17;
        public const int SYMBOL_PHARMACODE = 18;
        public const int SYMBOL_STANDARD25 = 19;
        public const int SYMBOL_IATA25 = 20;
        public const int SYMBOL_MSI = 21;
        public const int SYMBOL_TRIOPTIC = 22;
        public const int SYMBOL_RSS = 23;
        public const int SYMBOL_RSS_Limited = 24;
        public const int SYMBOL_RSS_Expended = 25;
        public const int SYMBOL_QR = 26;
        public const int SYMBOL_MICRO_QR = 27;
        public const int SYMBOL_AZTEC = 28;
        public const int SYMBOL_DATAMATRIX = 29;
        public const int SYMBOL_MAXICODE = 30;
        public const int SYMBOL_HANXIN = 31;
        public const int SYMBOL_GRIDMATRIX = 32;
        public const int SYMBOL_PDF417 = 33;
        public const int SYMBOL_MICROPDF417 = 34;
        public const int SYMBOL_GS1COMPOSITE = 35;
        public const int SYMBOL_CODE32 = 36;
    }

    #endregion 宏定义

    #region 枚举

    public enum Fsa_decoder_strength {
        FSA_DECODER_STRENGTH_LIGHT,
        FSA_DECODER_STRENGTH_NORMAL,
        FSA_DECODER_STRENGTH_HEAVY,
    };

    #endregion 枚举

    // 导入 Windows API 函数
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string dllName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    private byte[] imageBuffer;

    private static void Main(string[] args) {
        IntPtr dllHandle;

        #region 加载dll

        // 动态加载 DLL
        dllHandle = LoadLibrary("srDecoderDll.dll");

        if (dllHandle == IntPtr.Zero)
            Console.WriteLine("Error, specified DLL not found");

        #endregion 加载dll

        #region 获取函数指针

        /*
        var reinitFsaDecoderPtr = GetProcAddress(dllHandle, "_ReinitFsaDecoder@8");
        var fsaDecoderEnableSymbolPtr = GetProcAddress(dllHandle, "FsaDecoderEnableSymbol");
        var fsaDecoderSetStrengthPtr = GetProcAddress(dllHandle, "FsaDecoderSetStrength");
        var fsaDecoderSetTimeoutConfigPtr = GetProcAddress(dllHandle, "FsaDecoderSetTimeoutConfig");
        var fsaDecoderSetMaxBarcodeNumPtr = GetProcAddress(dllHandle, "FsaDecoderSetMaxBarcodeNum");
        var fsaDecodePtr = GetProcAddress(dllHandle, "FsaDecode");
        var fsaDecoderGetResultNumPtr = GetProcAddress(dllHandle, "FsaDecoderGetResultNum");
        var fsaDecoderGetResultLengthPtr = GetProcAddress(dllHandle, "FsaDecoderGetResultLength");
        var fsaDecoderGetResultBoundsPtr = GetProcAddress(dllHandle, "FsaDecoderGetResultBounds");
        var fsaDecoderGetResultStringPtr = GetProcAddress(dllHandle, "FsaDecoderGetResultString");
        var uninstallFsaDecoderPtr = GetProcAddress(dllHandle, "UninstallFsaDecoder");*/

        #endregion 获取函数指针

        #region 将函数指针转换为委托

        /*ReinitFsaDecoderDelegate reinitFsaDecoder = (ReinitFsaDecoderDelegate)Marshal.GetDelegateForFunctionPointer(reinitFsaDecoderPtr, typeof(ReinitFsaDecoderDelegate));
        FsaDecoderEnableSymbolDelegate fsaDecoderEnableSymbol = (FsaDecoderEnableSymbolDelegate)Marshal.GetDelegateForFunctionPointer(fsaDecoderEnableSymbolPtr, typeof(FsaDecoderEnableSymbolDelegate));
        FsaDecoderSetStrengthDelegate fsaDecoderSetStrength = (FsaDecoderSetStrengthDelegate)Marshal.GetDelegateForFunctionPointer(fsaDecoderSetStrengthPtr, typeof(FsaDecoderSetStrengthDelegate));
        FsaDecoderSetTimeoutConfigDelegate fsaDecoderSetTimeoutConfig = (FsaDecoderSetTimeoutConfigDelegate)Marshal.GetDelegateForFunctionPointer(fsaDecoderSetTimeoutConfigPtr, typeof(FsaDecoderSetTimeoutConfigDelegate));
        FsaDecoderSetMaxBarcodeNumDelegate fsaDecoderSetMaxBarcodeNum = (FsaDecoderSetMaxBarcodeNumDelegate)Marshal.GetDelegateForFunctionPointer(fsaDecoderSetMaxBarcodeNumPtr, typeof(FsaDecoderSetMaxBarcodeNumDelegate));
        FsaDecodeDelegate fsaDecode = (FsaDecodeDelegate)Marshal.GetDelegateForFunctionPointer(fsaDecodePtr, typeof(FsaDecodeDelegate));
        FsaDecoderGetResultNumDelegate fsaDecoderGetResultNum = (FsaDecoderGetResultNumDelegate)Marshal.GetDelegateForFunctionPointer(fsaDecoderGetResultNumPtr, typeof(FsaDecoderGetResultNumDelegate));
        FsaDecoderGetResultLengthDelegate fsaDecoderGetResultLength = (FsaDecoderGetResultLengthDelegate)Marshal.GetDelegateForFunctionPointer(fsaDecoderGetResultLengthPtr, typeof(FsaDecoderGetResultLengthDelegate));
        FsaDecoderGetResultBoundsDelegate fsaDecoderGetResultBounds = (FsaDecoderGetResultBoundsDelegate)Marshal.GetDelegateForFunctionPointer(fsaDecoderGetResultBoundsPtr, typeof(FsaDecoderGetResultBoundsDelegate));
        FsaDecoderGetResultStringDelegate fsaDecoderGetResultString = (FsaDecoderGetResultStringDelegate)Marshal.GetDelegateForFunctionPointer(fsaDecoderGetResultStringPtr, typeof(FsaDecoderGetResultStringDelegate));
        UninstallFsaDecoderDelegate uninstallFsaDecoder = (UninstallFsaDecoderDelegate)Marshal.GetDelegateForFunctionPointer(uninstallFsaDecoderPtr, typeof(UninstallFsaDecoderDelegate));*/

        #endregion 将函数指针转换为委托

        //IntPtr decoderHandle = ReinitFsaDecoder(800, 800);

        //读取一个图片到byte[]并获取图片的长度和宽度
        var strings = Directory.GetFiles("Image");
        var list = strings.Where(w => w.EndsWith(".jpg")).ToList();
        foreach (var s in list) {
            using (var bmp = Image.FromFile(s)) {
                var grayscale = ToGrayscale((Bitmap)bmp);
                //var bytes = ConvertToByteArray(grayscale);
                var bytes = ImageToGrayscale(grayscale);
                Decode(bytes, bmp.Width, bmp.Height);
            }
        }

        Console.WriteLine("Hello, World!");
        Console.ReadLine();
    }

    public static void Decode(byte[] pImage, int width, int height) {
        int resultCount = 0;

        // 初始化解码器，传入参数为待解析图片的宽和高
        var decoderHandle = ReinitFsaDecoder(width, height);

        // 设计解码类型，第一个参数为码型，第二个参数1为解析，0为不解析
        FsaDecoderEnableSymbol(SymbolType.SYMBOL_CODE128, 1);
        FsaDecoderEnableSymbol(SymbolType.SYMBOL_CODE39, 1);
        FsaDecoderEnableSymbol(SymbolType.SYMBOL_QR, 0);

        FsaDecoderSetStrength((int)Fsa_decoder_strength.FSA_DECODER_STRENGTH_HEAVY);

        // 设置解码超时时间
        FsaDecoderSetTimeoutConfig(100, 100, 100);

        // 设置最大条形码数目
        FsaDecoderSetMaxBarcodeNum(1);

        // 解码
        var fsaDecode = FsaDecode(pImage);

        // 获取解析成功的条形码数目
        resultCount = FsaDecoderGetResultNum();
        Console.WriteLine(fsaDecode);
        if (resultCount == 0) {
            Console.WriteLine("Decode failed");
            return;
        }

        // 初始化数据类型接收解析结果
        //ResultEntry[] resultList = new ResultEntry[10];
        BoundsInfo bound = new BoundsInfo();
        StringBuilder result = new StringBuilder();

        for (int i = 0; i < resultCount; i++) {
            // 获取条形码的字符长度
            int length = FsaDecoderGetResultLength(i);
            var resultEntry = new ResultEntry {
                length = length,
                data = Marshal.AllocHGlobal(length)
            };

            // 获取条形码位置
            FsaDecoderGetResultBounds(ref bound, i);
            Console.WriteLine($"{JsonConvert.SerializeObject(bound)}");

            // 获取条形码解析内容
            FsaDecoderGetResultString(resultEntry.data, i);

            string filterString = Marshal.PtrToStringAnsi(resultEntry.data);
            /*if (digitsOnlyCheckBox.Checked)
                filterString = Regex.Replace(filterString, "[^0-9]", "");
            filterString = filterString.Substring(0, digitLimitSpinBox.Value);

            Console.WriteLine(filterString);
            result.Append(filterString + "\n");
            resultLabel.Text = result.ToString();*/
            Console.WriteLine(ExtractBarcode(filterString));
        }

        // 资源回收释放
        UninstallFsaDecoder();

        /*foreach (ResultEntry entry in resultList) {
            if (entry.data != IntPtr.Zero) {
                Marshal.FreeHGlobal(entry.data);
            }
        }*/
    }

    public static string ExtractBarcode(string input) {
        // 使用正则表达式匹配数字和字母组合的部分
        string pattern = @"[A-Z0-9]+(?<![A-Z])";
        MatchCollection matches = Regex.Matches(input, pattern);

        // 将匹配到的条码信息连接成一个字符串
        string result = "";
        foreach (Match match in matches) {
            result += match.Value;
        }

        return result;
    }

    public static Bitmap ToGrayscale(Bitmap bmp) {
        //对比度
        Bitmap grayBmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);

        // 将调色板设置为灰度色彩（256个灰度级）
        ColorPalette pal = grayBmp.Palette;
        for (int i = 0; i < 256; i++)
            pal.Entries[i] = Color.FromArgb(i, i, i);
        grayBmp.Palette = pal;

        // 绘制灰度图像
        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        BitmapData grayData = grayBmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
        unsafe {
            byte* pBmp = (byte*)bmpData.Scan0.ToPointer();
            byte* pGray = (byte*)grayData.Scan0.ToPointer();
            int strideBmp = bmpData.Stride;
            int strideGray = grayData.Stride;

            // 调整灰度图像的像素值范围
            for (int y = 0; y < bmpData.Height; y++) {
                for (int x = 0; x < bmpData.Width; x++) {
                    byte r = pBmp[2];
                    byte g = pBmp[1];
                    byte b = pBmp[0];
                    byte gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b); // 灰度化算法
                    // 降低对比度，可以通过减小灰度值范围实现
                    byte adjustedGray = (byte)(gray * 0.6); // 可根据需要调整权重
                    pGray[x] = adjustedGray;
                    pBmp += 3;
                }
                pGray += strideGray;
                pBmp += strideBmp - bmpData.Width * 3;
            }
        }

        bmp.UnlockBits(bmpData);
        grayBmp.UnlockBits(grayData);
        /*Bitmap grayBmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);

        // 将调色板设置为灰度色彩（256个灰度级）
        ColorPalette pal = grayBmp.Palette;
        for (int i = 0; i < 256; i++)
            pal.Entries[i] = Color.FromArgb(i, i, i);
        grayBmp.Palette = pal;

        // 锁定位图数据以便进行灰度转换
        BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        BitmapData grayData = grayBmp.LockBits(new Rectangle(0, 0, grayBmp.Width, grayBmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

        unsafe {
            byte* pBmp = (byte*)bmpData.Scan0.ToPointer();
            byte* pGray = (byte*)grayData.Scan0.ToPointer();
            int strideBmp = bmpData.Stride;
            int strideGray = grayData.Stride;

            // 使用并行处理逐行遍历图像数据，将每个像素从RGB转换为灰度值
            Parallel.For(0, bmpData.Height, y => {
                byte* pBmpRow = pBmp + y * strideBmp;
                byte* pGrayRow = pGray + y * strideGray;

                for (int x = 0; x < bmpData.Width; x++) {
                    // 获取RGB通道值
                    byte r = pBmpRow[2];
                    byte g = pBmpRow[1];
                    byte b = pBmpRow[0];

                    // 计算灰度值
                    byte gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b);

                    // 将灰度值写入灰度图像的像素数据中
                    pGrayRow[x] = gray;

                    // 移动指针到下一个像素
                    pBmpRow += 3;
                }
            });
        }

        // 解锁位图数据
        bmp.UnlockBits(bmpData);
        grayBmp.UnlockBits(grayData);*/

        /*Bitmap grayBmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);
        ColorPalette palette = grayBmp.Palette;
        for (int i = 0; i < 256; i++) {
            palette.Entries[i] = Color.FromArgb(i, i, i);
        }
        grayBmp.Palette = palette;

        // 获取像素数据
        BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
        BitmapData grayBmpData = grayBmp.LockBits(new Rectangle(0, 0, grayBmp.Width, grayBmp.Height), ImageLockMode.WriteOnly, grayBmp.PixelFormat);

        unsafe {
            byte* bmpPtr = (byte*)bmpData.Scan0;
            byte* grayBmpPtr = (byte*)grayBmpData.Scan0;
            int bmpStride = bmpData.Stride;
            int grayBmpStride = grayBmpData.Stride;
            int height = bmpData.Height;

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < bmpData.Width; x++) {
                    byte grayValue = (byte)((bmpPtr[x * 3 + 2] * 11 + bmpPtr[x * 3 + 1] * 59 + bmpPtr[x * 3] * 30) / 100);
                    grayBmpPtr[x] = grayValue;
                }
                bmpPtr += bmpStride;
                grayBmpPtr += grayBmpStride;
            }
        }

        bmp.UnlockBits(bmpData);
        grayBmp.UnlockBits(grayBmpData);*/
        grayBmp.Save($"{AppDomain.CurrentDomain.BaseDirectory}\\{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.jpg");
        return grayBmp;
    }

    public static byte[] ImageToGrayscale(Bitmap image) {
        int width = image.Width;
        int height = image.Height;
        int bufferSize = width * height;
        byte[] imageBuffer = new byte[bufferSize];

        BitmapData imageData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        IntPtr scanLine = imageData.Scan0;

        unsafe {
            byte* pointer = (byte*)scanLine.ToPointer();

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    byte blue = pointer[y * imageData.Stride + x * 3];
                    byte green = pointer[y * imageData.Stride + x * 3 + 1];
                    byte red = pointer[y * imageData.Stride + x * 3 + 2];

                    byte grayValue = (byte)(red * 0.299 + green * 0.587 + blue * 0.114);
                    imageBuffer[y * width + x] = grayValue;
                }
            }
        }

        image.UnlockBits(imageData);

        return imageBuffer;
    }

    public static byte[] ConvertToByteArray(Bitmap bmp) {
        // 将图像保存到内存流中
        using (MemoryStream stream = new MemoryStream()) {
            bmp.Save(stream, ImageFormat.Bmp);

            // 从内存流中获取字节数组
            return stream.ToArray();
        }
    }

    public int GetImageWidth(Bitmap bmp) {
        return bmp.Width;
    }

    public int GetImageHeight(Bitmap bmp) {
        return bmp.Height;
    }

    public static ImageInfo GetImageInfo(string imagePath) {
        var imageInfo = new ImageInfo();

        using (var image = Image.FromFile(imagePath)) {
            // 将图像数据保存到内存流中
            using (MemoryStream ms = new MemoryStream()) {
                image.Save(ms, image.RawFormat);
                imageInfo.ImageData = ms.ToArray();
            }

            // 获取图像的宽度和高度
            imageInfo.Width = image.Width;
            imageInfo.Height = image.Height;
        }

        return imageInfo;
    }

    public class ImageInfo {
        public byte[] ImageData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}