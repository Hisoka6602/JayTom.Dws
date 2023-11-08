using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.LogsItemModels {

    public class VolumeLogItemModel : BaseLogItemModel {
        private DataSourceType _dataSourceType;

        public DataSourceType DataSourceType {
            get => _dataSourceType;
            set => SetProperty(ref _dataSourceType, value);
        }
    }
}