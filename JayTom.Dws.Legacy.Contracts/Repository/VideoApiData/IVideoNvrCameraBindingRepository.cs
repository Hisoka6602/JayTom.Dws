using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.VideoApiData;
using JayTom.Dws.Models.LocalConf.CloudConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.VideoApiData {

    public interface IVideoNvrCameraBindingRepository : IRepository<VideoNvrCameraBindingInfoModel> {
    }
}