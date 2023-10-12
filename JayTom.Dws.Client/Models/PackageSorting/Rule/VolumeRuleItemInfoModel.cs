using System;
using System.Linq;
using System.Text;
using NPOI.SS.Formula;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.PackageSorting.Rule {

    public class VolumeRuleItemInfoModel : BasePackageSortingItemInfoModel {
        private long _volumeSortingId;
        private string _formula = string.Empty;

        /// <summary>
        /// 体积分拣Id
        /// </summary>
        public long VolumeSortingId {
            get => _volumeSortingId;
            set => SetProperty(ref _volumeSortingId, value);
        }

        /// <summary>
        /// 规则
        /// </summary>
        public string Formula {
            get => _formula;
            set => SetProperty(ref _formula, value);
        }
    }
}