using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.LogsItemModels {

    public class CameraLogItemModel : BaseLogItemModel {
        private string _cameraSerialNumber = string.Empty;

        public string CameraSerialNumber {
            get => _cameraSerialNumber;
            set => SetProperty(ref _cameraSerialNumber, value);
        }
    }
}