using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.DataModels {

    public class InstructionInfoItemModel : BindableBase {
        private string _instructionContent = string.Empty;
        private DateTime _instructionGeneratedTime;
        private InstructionTypeType _instructionType = InstructionTypeType.None;

        /// <summary>
        /// 指令内容
        /// </summary>
        public string InstructionContent {
            get => _instructionContent;
            set => SetProperty(ref _instructionContent, value);
        }

        /// <summary>
        /// 指令产生时间
        /// </summary>
        public DateTime InstructionGeneratedTime {
            get => _instructionGeneratedTime;
            set => SetProperty(ref _instructionGeneratedTime, value);
        }

        /// <summary>
        /// 指令类型
        /// </summary>
        public InstructionTypeType InstructionType {
            get => _instructionType;
            set => SetProperty(ref _instructionType, value);
        }
    }
}