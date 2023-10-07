using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class SortingInstructionItemInfoModel : BasePackageSortingItemInfoModel {
        private long _instructionBindingId;
        private string _instruction = string.Empty;
        private string _replyContent = string.Empty;

        /// <summary>
        /// 绑定Id
        /// </summary>

        public long InstructionBindingId {
            get => _instructionBindingId;
            set => SetProperty(ref _instructionBindingId, value);
        }

        /// <summary>
        /// 指令
        /// </summary>

        public string Instruction {
            get => _instruction;
            set => SetProperty(ref _instruction, value);
        }

        /// <summary>
        /// 应答内容
        /// </summary>
        public string ReplyContent {
            get => _replyContent;
            set => SetProperty(ref _replyContent, value);
        }
    }
}