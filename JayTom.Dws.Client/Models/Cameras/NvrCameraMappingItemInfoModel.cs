using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras
{

    public class NvrCameraMappingItemInfoModel : BindableBase
    {
        public int Num
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        /// <summary>
        /// 输入序列号(来源设备唯一标识)
        /// </summary>
        public string SerialNumber
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        /// <summary>
        /// 显示标识
        /// </summary>
        public string DisplayIdentifier
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        /// <summary>
        /// 绑定源
        /// </summary>
        public SourceType BindingSource
        {
            get;
            set => SetProperty(ref field, value);
        } = SourceType.None;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        /// <summary>
        /// 取流通道
        /// </summary>
        public int Channel
        {
            get;
            set => SetProperty(ref field, value);
        }
    }
}
