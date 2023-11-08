using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.LogsItemModels {

    public class FtpLogItemModel : BaseLogItemModel {
        private FtpCommunicationType _ftpCommunicationType;

        public FtpCommunicationType FtpCommunicationType {
            get => _ftpCommunicationType;
            set => SetProperty(ref _ftpCommunicationType, value);
        }
    }
}