using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {

    public class BaseBarCodeForeignKeyInfo : BaseModel {

        [Column("BarcodeId")]
        public long BarcodeId { get; set; }

        [ForeignKey("Id")]
        public virtual BarCodeInfoModel BarCodeInfo { get; set; } = new();
    }
}