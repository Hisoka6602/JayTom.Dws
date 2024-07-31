namespace JayTom.Dws.CloudApi.Do.Conf {

    public class ExceptionInfoDo {

        /// <summary>
        /// 异常名称
        /// </summary>
        public string ExceptionName { get; set; } = string.Empty;

        /// <summary>
        /// 异常颜色
        /// </summary>
        public string ExceptionColor { get; set; } = string.Empty;

        /// <summary>
        /// id
        /// </summary>
        public long ExceptionTypeId { get; set; }
    }
}