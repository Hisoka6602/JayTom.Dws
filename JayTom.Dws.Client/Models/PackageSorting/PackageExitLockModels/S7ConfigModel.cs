using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.PackageSorting.PackageExitLockModels
{

    public class S7ConfigModel : BindableBase
    {
        private string _ip = string.Empty;
        private int _db;
        private int _rack;
        private int _slot;
        private int _timeout;

        /// <summary>
        /// IP 地址
        /// </summary>
        public string Ip
        {
            get => _ip;
            set => SetProperty(ref _ip, value);
        }

        /// <summary>
        /// 数据库
        /// </summary>
        public int Db
        {
            get => _db;
            set => SetProperty(ref _db, value);
        }

        /// <summary>
        /// 机架号
        /// </summary>
        public int Rack
        {
            get => _rack;
            set => SetProperty(ref _rack, value);
        }

        /// <summary>
        /// 插槽号
        /// </summary>
        public int Slot
        {
            get => _slot;
            set => SetProperty(ref _slot, value);
        }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int Timeout
        {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }
    }
}