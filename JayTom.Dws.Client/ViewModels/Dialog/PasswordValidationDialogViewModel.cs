using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.AppDto;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.AppSettingModel;

namespace JayTom.Dws.Client.ViewModels.Dialog
{
    public class PasswordValidationDialogViewModel : BindableBase
    {
        private readonly IConfigRepository _configRepository;
        private bool _isValidationPassed;
        private string _identifier = string.Empty;
        private string _password = string.Empty;
        private string _passwordHint = string.Empty;
        private SnackbarMessageQueue _passwordValidationMessageQueue = new(TimeSpan.FromSeconds(2));
        private PassWordSettingsDto _passWordSettingsDto = new();

        public PasswordValidationDialogViewModel(IConfigRepository configRepository)
        {
            _configRepository = configRepository;
        }

        /// <summary>
        /// 是否通过校验
        /// </summary>
        public bool IsValidationPassed
        {
            get => _isValidationPassed;
            set => SetProperty(ref _isValidationPassed, value);
        }

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// 密码提示
        /// </summary>
        public string PasswordHint
        {
            get => _passwordHint;
            set => SetProperty(ref _passwordHint, value);
        }

        /// <summary>
        /// 提示
        /// </summary>
        public SnackbarMessageQueue PasswordValidationMessageQueue
        {
            get => _passwordValidationMessageQueue;
            set => SetProperty(ref _passwordValidationMessageQueue, value);
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        public async void LoadedDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                _passWordSettingsDto = await _configRepository.FirstOrDefaultEntity<PassWordSettingsDto>("PassWordSettings") ?? new PassWordSettingsDto();
                PasswordHint = _passWordSettingsDto.PasswordHint;
            });
        }

        /// <summary>
        /// 校验密码
        /// </summary>
        public ICommand PasswordValidationCommand => new DelegateCommand<object>(PasswordValidationDelegate);

        private async void PasswordValidationDelegate(object obj)
        {
            //校验密码
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (_passWordSettingsDto.Password.Equals(Password))
                {
                    IsValidationPassed = true;
                    if (_passWordSettingsDto.SkipPasswordValidationForThisSession)
                    {
                        AppContext.SetData("IsValidationPassed", IsValidationPassed);
                    }

                    if (DialogHost.IsDialogOpen(Identifier))
                    {
                        DialogHost.Close(Identifier);
                    }
                }
                else
                {
                    PasswordValidationMessageQueue.Enqueue("密码错误");
                }
            });
        }

        public ICommand ExitValidationCommand => new DelegateCommand<object>(ExitValidationDelegate);

        private void ExitValidationDelegate(object obj)
        {
            //退出校验
            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }
    }
}