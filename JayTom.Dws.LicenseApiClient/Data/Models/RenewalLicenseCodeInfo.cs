namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class RenewalLicenseCodeInfo {
        public LicenseCodeItemInfoModel Info { get; set; } = new();
        public int Days { get; set; }
    }
}