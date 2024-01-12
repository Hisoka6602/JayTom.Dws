using JayTom.Dws.LicenseApi.Attributes;

namespace JayTom.Dws.LicenseApi.Do {

    public class DeleteApplicationDo {

        [LicenseApplicationIdExists(IsExists = true, ErrorMessage = "应用Id不存在!")]
        public long DeleteApplicationId { get; set; }
    }
}