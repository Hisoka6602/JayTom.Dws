namespace JayTom.Dws.Client.Models.Cameras {

    public class VolumeCameraItemInfoModel : BaseCameraItemInfoModel {
        private int _volumeMeasurementMode;
        private int _minSyncTime;
        private int _maxSyncTime;
        private double _minLength;
        private double _maxLength;
        private int _triggerMode;

        /// <summary>
        /// 体积测量模式
        /// </summary>
        public int VolumeMeasurementMode {
            get => _volumeMeasurementMode;
            set => SetProperty(ref _volumeMeasurementMode, value);
        }

        /// <summary>
        /// 最小同步时间（单位：毫秒）
        /// </summary>
        public int MinSyncTime {
            get => _minSyncTime;
            set => SetProperty(ref _minSyncTime, value);
        }

        /// <summary>
        /// 最大同步时间（单位：毫秒）
        /// </summary>
        public int MaxSyncTime {
            get => _maxSyncTime;
            set => SetProperty(ref _maxSyncTime, value);
        }

        /// <summary>
        /// 最小长度
        /// </summary>
        public double MinLength {
            get => _minLength;
            set => SetProperty(ref _minLength, value);
        }

        /// <summary>
        /// 最大长度
        /// </summary>
        public double MaxLength {
            get => _maxLength;
            set => SetProperty(ref _maxLength, value);
        }

        /// <summary>
        /// 触发模式
        /// </summary>
        public int TriggerMode {
            get => _triggerMode;
            set => SetProperty(ref _triggerMode, value);
        }
    }
}