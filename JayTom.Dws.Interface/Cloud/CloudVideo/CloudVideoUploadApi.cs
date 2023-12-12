using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Interface.Cloud.CloudVideo {

    public class CloudVideoUploadApi : ICloud {

        public Task<CloudUploadResponse> UploadData(string barcode, DateTime scanTime, double weight, CloudUploadVolumeInfo? volumeInfo = default,
            UploadImageInfo? imageInfo = default, CloudUploadOcrInfo? ocrInfo = default,
            CloudUploadApiInfo? uploadApiInfo = default, CloudUploadSortingInfo? sortingInfo = default, object? other = null,
            CancellationToken token = default) {
            return null;
        }
    }
}