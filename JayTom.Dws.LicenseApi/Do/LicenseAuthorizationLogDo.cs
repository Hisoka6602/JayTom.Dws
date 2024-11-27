using JayTom.Dws.LicenseApi.Attributes;

namespace JayTom.Dws.LicenseApi.Do {

    public class LicenseAuthorizationLogDo {

        [TimeSpanRange(TimeType = TimeType.Min, MaxDuration = 60, ErrorMessage = "查询时间跨度不能超60天")]
        public DateTime? StartTime { get; set; }

        [TimeSpanRange(TimeType = TimeType.Max, MaxDuration = 60, ErrorMessage = "查询时间跨度不能超60天")]
        public DateTime? EndTime { get; set; }

        [LicenseCodeExists(IsExists = true, ErrorMessage = "授权码不存在!")]
        public string? LicenseCode { get; set; }

        [UserCodeExists(IsExists = true, ErrorMessage = "租户代码不存在")]
        public string? UserCode { get; set; }
    }
}