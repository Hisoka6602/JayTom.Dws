using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Camera.Cameras.VolumeCamera.Dimension {

    public class DimensionVolumeSdk : IDisposable {

        //关闭后台应用程序
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "KillProcess", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool KillProcess();

        //开启后台应用程序
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "StartProcess", CallingConvention = CallingConvention.Cdecl)]
        private static extern void StartProcess();

        //扫描设备，返回在线设备数量
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "ScanDevice", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ScanDevice();

        //开启设备
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "OpenDevice", CallingConvention = CallingConvention.Cdecl)]
        private static extern int OpenDevice();

        //关闭设备
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "CloseDevice", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CloseDevice();

        //计算一次体积测量，调用该接口后GetDmsResult将变成阻塞接口，直到数据不为0时才返回
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "ComputeOnce", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ComputeOnce();

        //计算一次体积测量，调用该接口后GetDmsResult将变成非阻塞接口，数据为0也会返回
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "ComputeOnceNoBlock", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ComputeOnceNoBlock();

        //获取体积测量结果，参数dimensionData数组用于存储长宽高数据，参数imageData用于存储图像数据
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "GetDmsResult", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetDmsResult([Out, MarshalAs(UnmanagedType.LPArray)] float[] dimensionData, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] imageData);

        //获取体积测量结果错误信息，当体积测量结果为0时，可以通过该接口获取对应的错误信息，参数errMes用于存储错误信息
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "GetErrorMes", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetErrorMes([Out, MarshalAs(UnmanagedType.LPArray)] byte[] errMes);

        //设置体积测量结果图像中显示工作区域框
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "ShowWorkingArea", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ShowWorkingArea();

        //设置体积测量结果图像中隐藏工作区域框
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "HideWorkingArea", CallingConvention = CallingConvention.Cdecl)]
        private static extern int HideWorkingArea();

        //获取用于获取实时图像信息，参数imageData用于存储实时图像
        [DllImport("DimensionMessumentDll.dll", EntryPoint = "GetImage", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetImage([Out, MarshalAs(UnmanagedType.LPArray)] byte[] imageData);

        public event EventHandler<DimensionVolumeInfo>? VolumeCaptured;

        private static bool _isInitialized = false;
        private static bool _isRuning = false;
        private static Task? _volumeThread;
        private static CancellationTokenSource? _cancellationTokenSource;
        private byte[] _realTimeImageData = new byte[1280 * 800 * 3];
        private static int _deviceNum = 0;

        public async Task<KeyValuePair<bool, int>> Initialize() {
            if (_isInitialized) {
                return new KeyValuePair<bool, int>(true, _deviceNum);
            }
            KillProcess();//关闭电脑中可能已开启的DimensionMessument.exe后台应用程序
            StartProcess();//开启DimensionMessument.exe后台应用程序
            await Task.Delay(200);
            _deviceNum = ScanDevice();
            if (_deviceNum > 0) {
                var res = OpenDevice();//打开设备
                if (res == 0) {//打开设备成功
                    //等待3s设备打开并初始化完成
                    await Task.Delay(2000);
                    ShowWorkingArea();//设置在体积测量结果图像中显示工作区域框
                    await Task.Delay(100);
                    _isInitialized = true;
                    return new KeyValuePair<bool, int>(true, _deviceNum);
                }
            }

            return new KeyValuePair<bool, int>(false, 0);
        }

        public async void Dispose() {
            //释放
            await StopVolumeCapture();
            CloseDevice();//关闭设备
            KillProcess();//关闭后台应用程序
            _isInitialized = false;
        }

        public void StartVolumeCapture() {
            //开启体积线程
            if (!_isRuning) {
                _isRuning = true;
                _cancellationTokenSource = new CancellationTokenSource();
                _volumeThread = Task.Factory.StartNew(async () => {
                    await VolumeThread(_cancellationTokenSource.Token);
                }, _cancellationTokenSource.Token);
            }
        }

        public async Task StopVolumeCapture() {
            _cancellationTokenSource?.Cancel();
            await Task.Delay(200);
            if (_volumeThread != null) {
                await _volumeThread;
                _volumeThread?.Dispose();
            }
            _isRuning = false;
        }

        public async Task TriggerMeasurementPhotoAsync(CancellationToken cancellation = default) {
            try {
                var rec = ComputeOnceNoBlock(); //触发计算一次
                var dimensionData = new float[3]; //存储长、宽、高数据
                var imageData = new byte[5120000]; //存储图像数据
                var len = GetDmsResult(dimensionData, imageData); //获取测量结果与测量结果的图像
                if (len > 0) {
                    using var bmpStream = new MemoryStream(imageData, 0, len);
                    var image = System.Drawing.Image.FromStream(bmpStream);
                    // 处理图像
                    OnVolumeCaptured(new DimensionVolumeInfo() {
                        Length = dimensionData[0],
                        Width = dimensionData[1],
                        Height = dimensionData[2],
                        Image = (Bitmap?)image
                    });
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                await Task.Delay(50, cancellation);
            }
        }

        public async Task VolumeThread(CancellationToken token) {
            await Task.Yield();
            try {
                while (!token.IsCancellationRequested) {
                    try {
                        var rec = ComputeOnceNoBlock(); //触发计算一次
                        var dimensionData = new float[3]; //存储长、宽、高数据
                        var imageData = new byte[5120000]; //存储图像数据
                        var len = GetDmsResult(dimensionData, imageData); //获取测量结果与测量结果的图像
                        if (len > 0) {
                            using var bmpStream = new MemoryStream(imageData, 0, len);
                            var image = System.Drawing.Image.FromStream(bmpStream);
                            // 处理图像
                            OnVolumeCaptured(new DimensionVolumeInfo() {
                                Length = dimensionData[0],
                                Width = dimensionData[1],
                                Height = dimensionData[2],
                                Image = (Bitmap?)image
                            });
                        }
                    }
                    catch (Exception e) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    }
                    finally {
                        await Task.Delay(50, token);
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
        }

        protected virtual void OnVolumeCaptured(DimensionVolumeInfo e) {
            VolumeCaptured?.Invoke(this, e);
        }
    }

    public class DimensionVolumeInfo {

        /// <summary>
        /// 长
        /// </summary>
        public float Length { get; set; }

        /// <summary>
        /// 宽
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// 高
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        /// 图片
        /// </summary>
        public Bitmap? Image { get; set; }
    }
}