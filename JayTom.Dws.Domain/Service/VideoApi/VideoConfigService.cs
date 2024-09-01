using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Repository.VideoApi;
using JayTom.Dws.Domain.Repository.CloudApi;

namespace JayTom.Dws.Domain.Service.VideoApi {

    public class VideoConfigService : IVideoConfigService {
        private readonly IVideoConfigRepository _videoConfigRepository;

        public VideoConfigService(IVideoConfigRepository videoConfigRepository) {
            _videoConfigRepository = videoConfigRepository;
        }

        public async Task<KeyValuePair<bool, object>> GetVideoConfig(string settingsName, CancellationToken token = default) {
            var configInfoModels = await _videoConfigRepository.MemoryCacheData();
            var configInfoModel = configInfoModels.FirstOrDefault(f =>
                f.ConfigName.Equals(settingsName, StringComparison.CurrentCultureIgnoreCase));

            return new KeyValuePair<bool, object>(configInfoModel != null, configInfoModel ?? new ConfigInfoModel());
        }

        public async Task<KeyValuePair<bool, object>> SetVideoConfig(string settingsName, ConfigInfoModel configInfo, CancellationToken token = default) {
            bool insertOrUpdate;
            var configInfoModel = await _videoConfigRepository.FirstOrDefault(f =>
                f.ConfigName.Equals(settingsName,
                    StringComparison.CurrentCultureIgnoreCase), token);

            if (configInfoModel is not null) {
                configInfoModel.Value = configInfo.Value;
                insertOrUpdate = await _videoConfigRepository.Update(configInfoModel, token);
            }
            else {
                insertOrUpdate = await _videoConfigRepository.Insert(configInfo, token);
            }
            return new KeyValuePair<bool, object>(insertOrUpdate, $"保存{(insertOrUpdate ? "成功" : "失败")}");
        }
    }
}