namespace JayTom.Dws.CloudApiClient.Data.Models {

    public class RealTimeDataItemModel {
        public long Timestamp { get; set; }
        public int Quantity { get; set; }
        public string Color { get; set; } = string.Empty;
    }
}