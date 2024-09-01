using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Service.VideoApi;

namespace JayTom.Dws.Application.Service.VideoApi {

    public class VideoConfigAppService : IVideoConfigAppService {
        private readonly IVideoConfigService _videoConfigService;

        public VideoConfigAppService(IVideoConfigService videoConfigService) {
            _videoConfigService = videoConfigService;
        }

        public Task<KeyValuePair<bool, object>> GetVideoConfig(string settingsName, CancellationToken token = default) {
            return _videoConfigService.GetVideoConfig(settingsName, token);
        }

        public Task<KeyValuePair<bool, object>> SetVideoConfig(string settingsName, string configJson, CancellationToken token = default) {
            return _videoConfigService.SetVideoConfig(settingsName, new ConfigInfoModel {
                ConfigName = settingsName,
                Value = configJson
            }, token);
        }
    }
}