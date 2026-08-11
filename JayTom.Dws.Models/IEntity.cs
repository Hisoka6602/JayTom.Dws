namespace JayTom.Dws.Data {

    /// <summary>定义统一使用 64 位整数主键的持久化实体。</summary>
    public interface IEntity {
        /// <summary>获取或设置实体的 64 位整数主键。</summary>
        long Id { get; set; }
    }
}
