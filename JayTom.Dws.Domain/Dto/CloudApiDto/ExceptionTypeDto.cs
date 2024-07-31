using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.CloudApiDto {

    public class ExceptionTypeDto {

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
        public long Id { get; set; }
    }
}