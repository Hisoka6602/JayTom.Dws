using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.AppDto {

    public class OtherSettingsDto {

        /// <summary>
        /// 是否自动最大化
        /// </summary>
        public bool IsAutoMaximize { get; set; }

        /// <summary>
        /// 是否自动启动
        /// </summary>
        public bool IsAutoStart { get; set; }

        /// <summary>
        /// 是否开机自动运行
        /// </summary>
        public bool IsAutoRunEnabled { get; set; }

        /// <summary>
        /// 程序标题
        /// </summary>
        public string ProgramTitle { get; set; } = string.Empty;

        /// <summary>
        /// 程序Logo路径
        /// </summary>
        public string ProgramLogoPath { get; set; } = string.Empty;
    }
}