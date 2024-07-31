namespace JayTom.Dws.CloudApiClient.Data.Models {

    public class DonutItemModel {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Color { get; set; } = string.Empty;
        public float ExceptionPercentage { get; set; }
        public float OverallPercentage { get; set; }
    }
}