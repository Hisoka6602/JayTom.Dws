using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.LogsItemModels
{

    public class CameraLogItemModel : BaseLogItemModel
    {
        public string CameraSerialNumber
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;
    }
}
