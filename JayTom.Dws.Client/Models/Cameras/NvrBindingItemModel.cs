using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras {

    public class NvrBindingItemModel : IpcNvrItemInfoModel {
        private bool _isNvrBound;

        /// <summary>
        /// 是否已绑定Nvr
        /// </summary>
        public bool IsNvrBound {
            get => _isNvrBound;
            set => SetProperty(ref _isNvrBound, value);
        }
    }
}