namespace JayTom.Dws.Client.Models.Cameras
{

    public class BarcodeScannerCameraItemInfoModel : BaseCameraItemInfoModel
    {
        /// <summary>
        /// 是否显示实时图像
        /// </summary>
        public bool IsShowRealTimeImage
        {
            get;
            set => SetProperty(ref field, value);
        }
    }
}
