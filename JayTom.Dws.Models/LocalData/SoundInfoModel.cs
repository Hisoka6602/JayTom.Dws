using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalData {

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
        [NotMapped]
        public byte[]? SoundFile { get; set; }

        /// <summary>数据库外部声音文件的稳定引用。</summary>
        [Column("SoundFileReference"), Required, InsertOrUpdate]
        public string SoundFileReference { get; set; } = string.Empty;
    }
}
