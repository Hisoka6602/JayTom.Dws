using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.LogsItemModels
{

    public class FtpLogItemModel : BaseLogItemModel
    {
        public FtpCommunicationType FtpCommunicationType
        {
            get;
            set => SetProperty(ref field, value);
        }
    }
}
