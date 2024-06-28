using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Entities.LogsEntities {

    public class LogsInfoEntities {

        /// <summary>
        /// 数据集合
        /// </summary>
        public object Infos { get; set; } = new();

        /// <summary>
        /// 总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}