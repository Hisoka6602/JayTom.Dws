using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.LogsItemModels
{

    public class VolumeLogItemModel : BaseLogItemModel
    {
        public DataSourceType DataSourceType
        {
            get;
            set => SetProperty(ref field, value);
        }
    }
}
