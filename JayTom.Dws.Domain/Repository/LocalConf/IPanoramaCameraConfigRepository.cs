using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.CameraConfig;

namespace JayTom.Dws.Domain.Repository.LocalConf {

    public interface IPanoramaCameraConfigRepository : IRepository<PanoramaCameraConfigInfoModel> {
    }
}