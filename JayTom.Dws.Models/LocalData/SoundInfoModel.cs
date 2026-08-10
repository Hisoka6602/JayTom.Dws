using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {

    [Table("Data_SoundInfo", Schema = "dbo")]
    public class SoundInfoModel : BaseModel {

        /// <summary>
        /// 声音名称
        /// </summary>
        [Column("SoundName"), Required, UpdateBy]
        public string SoundName { get; set; } = string.Empty;

        /// <summary>
        /// 声音文件
        /// </summary>
        [Column("SoundFile"), Required, InsertOrUpdate]
        public byte[]? SoundFile { get; set; }
    }
}