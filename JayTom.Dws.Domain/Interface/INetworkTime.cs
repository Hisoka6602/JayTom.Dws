namespace JayTom.Dws.Domain.Interface {

    public interface INetworkTime {

        /// <summary>
        /// 获取时间
        /// </summary>
        /// <returns></returns>
        public Task<DateTime> GetTime();
    }
}