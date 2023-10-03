using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class BasePackageSortingItemInfoModel : BindableBase {
        private long _id;
        private int _num;
        private string _remarks = string.Empty;
        private DateTime _createTime = DateTime.Now;
        private DateTime _modifyTime = DateTime.Now;

        /// <summary>
        /// Id
        /// </summary>
        public long Id {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 序号
        /// </summary>
        public int Num {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifyTime {
            get => _modifyTime;
            set => SetProperty(ref _modifyTime, value);
        }
    }
}