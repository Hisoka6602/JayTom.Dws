using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.LicenseApi {

    public class LicenseFeatureDto {
        public string FeatureName { get; set; } = string.Empty;
        public string FeatureGuid { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}