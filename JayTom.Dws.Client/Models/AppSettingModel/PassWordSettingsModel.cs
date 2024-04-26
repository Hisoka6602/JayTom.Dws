using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.AppDto;

namespace JayTom.Dws.Client.Models.AppSettingModel {

    public class PassWordSettingsModel : BindableBase {

        private List<PasswordProtectionModuleItemInfoModel> _passwordProtectionModuleItems = new()
        {
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "数据管理",
                PageClassName = "DataManagementPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "相机配置",
                PageClassName = "CameraConfigurationPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "Api接口",
                PageClassName = "APISettingsPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "存图设置",
                PageClassName = "SaveImageSettingsPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "过滤设置",
                PageClassName = "BarcodeFilterSettingsPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "称重设置",
                PageClassName = "WeightSettingPages"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "结果输出",
                PageClassName = "ResultOutputSettingsPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "内容输入",
                PageClassName = "ContentInputSettingsPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "分拣设置",
                PageClassName = "PackageSortingSettingsPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "组包设置",
                PageClassName = "CreatePackageSettingsPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "Ocr设置",
                PageClassName = "OcrSettingsPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "云端服务",
                PageClassName = "CloudServicePage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "程序设置",
                PageClassName = "AppSettingsPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "日志管理",
                PageClassName = "LogManagerPage"
            },
            new PasswordProtectionModuleItemInfoModel()
            {
                Description = "空间清理",
                PageClassName = "CacheClearSettingsPage"
            },
        };

        private bool _isUsePasswordProtection;
        private string _password = "123";
        private string _confirmPassword = string.Empty;
        private string _passwordHint = string.Empty;
        private bool _skipPasswordValidationForThisSession = true;

        /// <summary>
        /// 保护模块
        /// </summary>
        public List<PasswordProtectionModuleItemInfoModel> PasswordProtectionModuleItems {
            get => _passwordProtectionModuleItems;
            set => SetProperty(ref _passwordProtectionModuleItems, value);
        }

        /// <summary>
        /// 获取或设置一个值，该值指示是否使用密码保护。
        /// </summary>
        public bool IsUsePasswordProtection {
            get => _isUsePasswordProtection;
            set => SetProperty(ref _isUsePasswordProtection, value);
        }

        /// <summary>
        /// 获取或设置密码。
        /// </summary>
        public string Password {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// 获取或设置确认密码。
        /// </summary>
        public string ConfirmPassword {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        /// <summary>
        /// 获取或设置密码提示。
        /// </summary>
        public string PasswordHint {
            get => _passwordHint;
            set => SetProperty(ref _passwordHint, value);
        }

        /// <summary>
        /// 获取或设置一个值，该值指示是否验证密码后，在本次启动期间的其他操作中不重复验证。
        /// </summary>
        public bool SkipPasswordValidationForThisSession {
            get => _skipPasswordValidationForThisSession;
            set => SetProperty(ref _skipPasswordValidationForThisSession, value);
        }
    }

    public class PasswordProtectionModuleItemInfoModel : BindableBase {
        private bool _isProtected;
        private string _pageClassName = string.Empty;
        private string _description = string.Empty;

        /// <summary>
        /// 是否使用密码保护
        /// </summary>
        public bool IsProtected {
            get => _isProtected;
            set => SetProperty(ref _isProtected, value);
        }

        /// <summary>
        /// 页面类名
        /// </summary>
        public string PageClassName {
            get => _pageClassName;
            set => SetProperty(ref _pageClassName, value);
        }

        /// <summary>
        /// 描述信息
        /// </summary>
        public string Description {
            get => _description;
            set => SetProperty(ref _description, value);
        }
    }
}