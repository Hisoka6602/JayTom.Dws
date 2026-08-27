using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalLog;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.LogsItemModels
{
    public class OutputLogItemModel : BaseLogItemModel
    {
        private OutputType _outputType;
        private string _outputContent = string.Empty;
        private string _destination = string.Empty;

        /// <summary>
        /// 输出类型
        /// </summary>
        public OutputType OutputType
        {
            get => _outputType;
            set => SetProperty(ref _outputType, value);
        }

        /// <summary>
        /// 输出内容
        /// </summary>
        public string OutputContent
        {
            get => _outputContent;
            set => SetProperty(ref _outputContent, value);
        }

        /// <summary>
        /// 目的地
        /// </summary>
        public string Destination
        {
            get => _destination;
            set => SetProperty(ref _destination, value);
        }
    }
}