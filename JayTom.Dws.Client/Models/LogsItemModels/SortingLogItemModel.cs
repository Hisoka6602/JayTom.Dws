using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.LogsItemModels
{

    public class SortingLogItemModel : BaseLogItemModel
    {
        private CommunicationType? _communicationType;
        private DataFormatType? _dataFormatType;

        /// <summary>
        /// 通讯类型(收发类型)
        /// </summary>
        public CommunicationType? CommunicationType
        {
            get => _communicationType;
            set => SetProperty(ref _communicationType, value);
        }

        /// <summary>
        /// 数据类型
        /// </summary>
        public DataFormatType? DataFormatType
        {
            get => _dataFormatType;
            set => SetProperty(ref _dataFormatType, value);
        }
    }
}