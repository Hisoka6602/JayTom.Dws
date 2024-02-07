namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class ApplicationItemInfoModel {
        public long Id { get; set; }
        public string ApplicationName { get; set; } = "Title";

        public string Description { get; set; } = "Description";

        public string ButtonText { get; set; } = "ButtonText";
        public Stream? Image { get; set; }

        public List<FeatureItemModel> LicenseFeatureInfos { get; set; } = new();
    }
}