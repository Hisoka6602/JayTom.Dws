using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.LogsItemModels {

    public class WeighingLogItemModel : BaseLogItemModel {
        private string? _source;
        private CommunicationType? _communicationType;
        private DataFormatType? _dataFormatType;
        private DataSourceType? _dataSourceType;
        private double _formatWeight;

        /// <summary>
        /// 源数据
        /// </summary>
        public string? Source {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        /// <summary>
        /// 通讯类型
        /// </summary>
        public CommunicationType? CommunicationType {
            get => _communicationType;
            set => SetProperty(ref _communicationType, value);
        }

        /// <summary>
        /// 数据类型
        /// </summary>
        public DataFormatType? DataFormatType {
            get => _dataFormatType;
            set => SetProperty(ref _dataFormatType, value);
        }

        /// <summary>
        /// 数据来源类型
        /// </summary>
        public DataSourceType? DataSourceType {
            get => _dataSourceType;
            set => SetProperty(ref _dataSourceType, value);
        }

        /// <summary>
        /// 格式化后的重量
        /// </summary>
        public double FormatWeight {
            get => _formatWeight;
            set => SetProperty(ref _formatWeight, value);
        }
    }
}