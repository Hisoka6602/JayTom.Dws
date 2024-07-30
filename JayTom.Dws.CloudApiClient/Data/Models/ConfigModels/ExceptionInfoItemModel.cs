namespace JayTom.Dws.CloudApiClient.Data.Models.ConfigModels {

    public class ExceptionInfoItemModel {
        public int Num { get; set; }
        public long Id { get; set; }

        /// <summary>
        /// 异常名称
        /// </summary>
        public string ExceptionName { get; set; } = string.Empty;
        /// <summary>
        /// 异常颜色
        /// </summary>
        public string ExceptionColor { get; set; } = string.Empty;
    }
}