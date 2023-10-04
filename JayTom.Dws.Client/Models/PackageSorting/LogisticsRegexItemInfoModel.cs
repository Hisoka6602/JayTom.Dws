using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class LogisticsRegexItemInfoModel : BasePackageSortingItemInfoModel {
        private long _logisticsId;
        private string _regexPattern = string.Empty;

        /// <summary>
        /// 物流Id
        /// </summary>
        public long LogisticsId {
            get => _logisticsId;
            set => SetProperty(ref _logisticsId, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegexPattern {
            get => _regexPattern;
            set => SetProperty(ref _regexPattern, value);
        }
    }
}