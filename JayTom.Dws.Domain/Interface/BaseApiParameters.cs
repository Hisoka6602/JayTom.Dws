namespace JayTom.Dws.Domain.Interface {

    public class BaseApiParameters {

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// TimeOut
        /// </summary>
        public int TimeOut { get; set; } = 1000;
    }
}