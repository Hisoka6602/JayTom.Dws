using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.ViewModels.Editors.Enums
{

    public enum EditorOperationType
    {

        /// <summary>
        /// 新增
        /// </summary>
        [Description("新增")]
        Add,

        /// <summary>
        /// 编辑
        /// </summary>
        [Description("编辑")]
        Edit,

        /// <summary>
        /// 批量改密
        /// </summary>
        [Description("批量改密")]
        BatchChangePassword
    }
}