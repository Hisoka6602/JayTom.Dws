using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data {

    public class BaseModel : IEntity<long> {

        [Column("Id"), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
    }
}