namespace JayTom.Dws.Domain.Sign {

    public interface ISign {

        /// <summary>
        /// Md5Sign验证
        /// </summary>
        /// <param name="md5Content"></param>
        /// <param name="secret"></param>
        /// <param name="content"></param>
        /// <param name="constkey"></param>
        /// <returns></returns>
        bool IsValid(string md5Content, string secret, string content, string constkey);

        /// <summary>
        /// Md5Sign验证
        /// </summary>
        /// <param name="md5Content"></param>
        /// <param name="secret"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        bool IsValid(string md5Content, string secret, string content);

        /// <summary>
        /// Md5Sign验证
        /// </summary>
        /// <param name="md5Content"></param>
        /// <param name="secret"></param>
        /// <param name="validTime"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        bool IsValid(string md5Content, string secret, DateTime validTime, string content);
    }
}