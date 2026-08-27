using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.CameraConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CameraConfig {

    public interface IUsbCameraConfigRepository : IRepository<UsbCameraConfigInfoModel> {
    }
}