using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Interface {

    [AttributeUsage(AttributeTargets.Class)]
    public class ApiClassAttribute : Attribute {

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// 执行类型
        /// </summary>
        public ExecutionType ExecTypes { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <param name="name">名称</param>
        /// <param name="version">版本号</param>
        /// <param name="execTypes">执行类型</param>
        public ApiClassAttribute(string displayName, string name, string version = "", ExecutionType execTypes = ExecutionType.UploadInformation) {
            DisplayName = displayName;
            Name = name;
            Version = version;
            ExecTypes = execTypes;
        }
    }

    [Flags]
    public enum ExecutionType {

        /// <summary>
        /// 无执行类型
        /// </summary>
        None = 0,

        /// <summary>
        /// 上传信息请求接口
        /// </summary>
        UploadInformation = 1,

        /// <summary>
        /// 扫描包裹
        /// </summary>
        ScanPackage = 2,

        /// <summary>
        /// 发送分拣报告
        /// </summary>
        SendSortingReport = 4,

        /// <summary>
        /// 发送揽件报告
        /// </summary>
        SendPickupReport = 8,

        /// <summary>
        /// 发送集包报告
        /// </summary>
        SendConsolidationReport = 16
    }
}