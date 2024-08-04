using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Interface {

    public class BaseApiParameters {

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// TimeOut
        /// </summary>
        public int TimeOut { get; set; }
    }
}