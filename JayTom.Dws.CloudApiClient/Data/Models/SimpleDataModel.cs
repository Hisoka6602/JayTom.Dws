namespace JayTom.Dws.CloudApiClient.Data.Models {

    public class SimpleDataModel {
        public string Icon { get; set; } = string.Empty;
        public string Value { get; set; } = "ButtonText";
        public string Description { get; set; } = "Description";
        public string Unit { get; set; } = string.Empty;
        public System.Drawing.Color IconColor { get; set; } = System.Drawing.Color.Blue;
        public System.Drawing.Color TrayColor { get; set; } = System.Drawing.Color.Blue;
    }
}