namespace JayTom.Dws.Data {

    public interface IEntity<TPrimaryKey> {
        TPrimaryKey Id { get; set; }
    }
}