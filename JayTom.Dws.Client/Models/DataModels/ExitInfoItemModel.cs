using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.DataModels {

    public class ExitInfoItemModel : BindableBase {
        private string _theoreticalExit = string.Empty;
        private string _physicalExit = string.Empty;
        private long _physicalExitId;

        /// <summary>
        /// 理论格口
        /// </summary>
        public string TheoreticalExit {
            get => _theoreticalExit;
            set => SetProperty(ref _theoreticalExit, value);
        }

        /// <summary>
        /// 物理格口
        /// </summary>
        public string PhysicalExit {
            get => _physicalExit;
            set => SetProperty(ref _physicalExit, value);
        }

        /// <summary>
        /// 物理格口Id
        /// </summary>
        public long PhysicalExitId {
            get => _physicalExitId;
            set => SetProperty(ref _physicalExitId, value);
        }
    }
}