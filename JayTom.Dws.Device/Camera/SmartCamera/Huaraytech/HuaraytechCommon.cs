using System.Drawing;
using LogisticsBaseCSharp;

namespace JayTom.Dws.Device.Camera.SmartCamera.Huaraytech {

    [Flags]
    public enum CodeType {
        Barcode = 1,
        DataCode = 2,
    }

    /// <summary>
    /// 配合底层接口回调图片使用, 已经包含拷贝和释放
    /// </summary>
    public class RawImage {

        public RawImage(int width, int height, int type, int datasize, IntPtr data, uint imageIndex) {
            Width = width;
            Height = height;
            Type = type;
            DataSize = datasize;
            ImageData = data;
            ImageIndex = imageIndex;
        }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Type { get; set; }

        public int DataSize { get; set; }

        public IntPtr ImageData { get; set; }

        public uint ImageIndex { get; set; }

        /// <summary>
        /// 深拷贝一份VslbImage, 会在类析构时释放
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static implicit operator RawImage(LogisticsAPIStruct.VslbImage image) {
            var imgcpy = image.Clone();
            return new RawImage(imgcpy.width, imgcpy.height, imgcpy.type, imgcpy.dataSize, imgcpy.ImageData, image.img_idx);
        }

        /// <summary>
        /// 释放内存, 此处内存为非托管内存
        /// </summary>
        ~RawImage() {
            if (ImageData != IntPtr.Zero) {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(ImageData);
            }
        }
    }

    public struct VolumeInfo {
        public double Length { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double Volume { get; set; }

        //public float Planeness { get; set; }

        //public float PlanenessHightDif { get; set; }

        //public Int64 VolTimeStamp { get; set; }
    }

    public struct TimeInfo {
        public Int64 TimeUp { get; set; }
        public Int64 TimeDown { get; set; }
        public Int64 TimeCodeParse { get; set; }
        public Int64 TimeFrameSend { get; set; }
        public Int64 TimeFrameGet { get; set; }
        public Int64 TimeCollect { get; set; }
        public Int64 TimWeight { get; set; }
        public Int64 TimVol { get; set; }
        public Int64 TimeCallback { get; set; }
    }

    public struct WeightData {
        public Int32 Weight { get; set; }
        public byte[] OrigData { get; set; }
        public Int64 WeightTimeStamp { get; set; }
        public bool Flag { get; set; }
    }

    /// <summary>
    /// 包裹的条码重量体积等信息
    /// </summary>
    public class BaseCodeData {

        /// <summary>
        /// 值为0，表示上报包裹的条码信息；值为1，表示上报包裹的条码、重量、体积信息
        /// 一个包裹，先上报包裹的条码信息，再上报绑定后包裹的条码、重量、体积信息
        /// </summary>
        public int OutputResult { get; set; }

        /// <summary>
        /// 包裹的原图
        /// </summary>
        public RawImage OriImage { get; set; }

        /// <summary>
        /// 包裹的面单抠图
        /// </summary>
        public RawImage WayImage { get; set; }

        /// <summary>
        /// 包裹的条码列表，如果没有读到条码那列表中就一个元素，值为 noread
        /// </summary>
        public List<string> CodeList { get; set; }

        /// <summary>
        /// 包裹条码点坐标数组的列表，用于将图片中的条码用画框圈出来
        /// 一个条码的点坐标数组包含五个点坐标，其中有两个点是一样的，便于用直线画出一个完整的框
        /// </summary>
        public List<Point[]> AreaList { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraID { get; set; }

        /// <summary>
        /// DWS扫描到包裹条码的时间戳，这时间戳是底层SDK上报的
        /// </summary>
        public long CodeTimeStamp { get; set; }

        /// <summary>
        /// 包裹的重量，单位为克
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// 包裹的体积信息结构体，长度（毫米）VolumeInfo.length、宽度（毫米）VolumeInfo.width、高度（毫米）VolumeInfo.height、体积（立方毫米）VolumeInfo.volume
        /// </summary>
        public VolumeInfo VolumeInfo { get; set; }

        /// <summary>
        /// 每个条码的信息
        /// </summary>
        public SingleCodeInfo[] CodesInfo { get; set; }

        /// <summary>
        /// 包裹详细时间信息
        /// </summary>
        public TimeInfo BagTimeInfo { get; set; }

        /// <summary>
        /// 重量详细信息
        /// </summary>
        public WeightData WeightInfo { get; set; }

        public Dictionary<string, object> ExtraDict { get; set; }
    }

    public enum ImageSource {
        None = 0,
        OriginalImage = 1,
        MattingImage = 1 << 1,
        PanomicImage = 1 << 2,
        AllCamImage = 1 << 3
    }
}