using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;

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
        /// 备注
        /// </summary>
        [DisplayName("备注"), MemberNotNull, ExcelInfo(Width = 6000)]
        public string Remarks {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        /// <summary>
        /// 序号
        /// </summary>
        [DisplayName("序号"), ExcelInfo(Width = 2800)]
        public int Num {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间"), ExcelInfo(Width = 5000)]
        public DateTime CreateTime {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        /// <summary>
        /// 修改时间
        /// </summary>
        [DisplayName("修改时间"), ExcelInfo(Width = 5000)]
        public DateTime ModifyTime {
            get => _modifyTime;
            set => SetProperty(ref _modifyTime, value);
        }
    }
}