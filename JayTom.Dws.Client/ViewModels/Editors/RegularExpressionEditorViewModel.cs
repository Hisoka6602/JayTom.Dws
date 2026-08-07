using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JayTom.Dws.Client.ViewModels.Editors
{

    public class RegularExpressionEditorViewModel : BindableBase
    {
        private string _identifier = string.Empty;
        private bool _isOk;
        private bool _isUseReplace;
        private string _regexPattern = string.Empty;
        private string _replaceContent = string.Empty;
        private string _exceptionContent = string.Empty;
        private int _minimumLength = 10;
        private int _maximumLength = 30;
        private string _anyStartCodes = string.Empty;
        private bool _isQuickSetup;
        private string _remarks = string.Empty;
        private bool _isNumeric;

        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public bool IsOk
        {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        /// <summary>
        /// 是否需要替换
        /// </summary>
        public bool IsUseReplace
        {
            get => _isUseReplace;
            set => SetProperty(ref _isUseReplace, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegexPattern
        {
            get => _regexPattern;
            set => SetProperty(ref _regexPattern, value);
        }

        /// <summary>
        /// 替换的内容
        /// </summary>
        public string ReplaceContent
        {
            get => _replaceContent;
            set => SetProperty(ref _replaceContent, value);
        }

        public string ExceptionContent
        {
            get => _exceptionContent;
            set => SetProperty(ref _exceptionContent, value);
        }

        /// <summary>
        /// 是否快速编辑
        /// </summary>
        public bool IsQuickSetup
        {
            get => _isQuickSetup;
            set => SetProperty(ref _isQuickSetup, value);
        }

        /// <summary>
        /// 最小条码位数
        /// </summary>
        public int MinimumLength
        {
            get => _minimumLength;
            set => SetProperty(ref _minimumLength, value);
        }

        /// <summary>
        /// 最大条码位数
        /// </summary>
        public int MaximumLength
        {
            get => _maximumLength;
            set => SetProperty(ref _maximumLength, value);
        }

        /// <summary>
        /// 开头字符
        /// </summary>
        public string AnyStartCodes
        {
            get => _anyStartCodes;
            set => SetProperty(ref _anyStartCodes, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks
        {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        /// <summary>
        /// 是否纯数字
        /// </summary>
        public bool IsNumeric
        {
            get => _isNumeric;
            set => SetProperty(ref _isNumeric, value);
        }

        public ICommand UpdateRegularExpressionCommand => new DelegateCommand(UpdateRegularExpressionDelegate);

        private async void UpdateRegularExpressionDelegate()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var regularChars = new List<string>();
                if (IsNumeric)
                {
                    regularChars.Add("(?=^([0-9])+$)");
                }
                //条码开头
                if (!string.IsNullOrWhiteSpace(AnyStartCodes))
                {
                    var strings = AnyStartCodes.Replace(";", "|");

                    regularChars.Add($"(^(?={strings}).*)");
                }
                //位数限制
                regularChars.Add($"(^.{{{MinimumLength},{MaximumLength}}}$)");
                RegexPattern = string.Join(string.Empty, regularChars);
            });
        }

        public ICommand SaveCommand => new DelegateCommand(SaveDelegate);

        private void SaveDelegate()
        {
            IsOk = true;
            if (string.IsNullOrWhiteSpace(RegexPattern))
            {
                ExceptionContent += "表达式不能为空";
                IsOk = false;
            }
            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand CancelCommand => new DelegateCommand(CancelDelegate);

        private void CancelDelegate()
        {
            IsOk = false;
            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }
    }
}