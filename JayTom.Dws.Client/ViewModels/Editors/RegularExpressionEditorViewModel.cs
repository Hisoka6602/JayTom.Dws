using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JayTom.Dws.Client.ViewModels.Editors {

    public class RegularExpressionEditorViewModel : BindableBase {
        private string _identifier = string.Empty;
        private bool _isOk;
        private bool _isUseReplace;
        private string _regexPattern = string.Empty;
        private string _replaceContent = string.Empty;
        private string _exceptionContent = string.Empty;

        public string Identifier {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        public bool IsOk {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        /// <summary>
        /// 是否需要替换
        /// </summary>
        public bool IsUseReplace {
            get => _isUseReplace;
            set => SetProperty(ref _isUseReplace, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegexPattern {
            get => _regexPattern;
            set => SetProperty(ref _regexPattern, value);
        }

        /// <summary>
        /// 替换的内容
        /// </summary>
        public string ReplaceContent {
            get => _replaceContent;
            set => SetProperty(ref _replaceContent, value);
        }

        public string ExceptionContent {
            get => _exceptionContent;
            set => SetProperty(ref _exceptionContent, value);
        }

        public ICommand SaveCommand => new DelegateCommand(SaveDelegate);

        private void SaveDelegate() {
            IsOk = true;
            if (string.IsNullOrWhiteSpace(RegexPattern)) {
                ExceptionContent += "表达式不能为空";
                IsOk = false;
            }
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand CancelCommand => new DelegateCommand(CancelDelegate);

        private void CancelDelegate() {
            IsOk = false;
            if (DialogHost.IsDialogOpen(Identifier)) {
                DialogHost.Close(Identifier);
            }
        }
    }
}