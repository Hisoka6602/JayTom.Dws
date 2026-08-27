using JayTom.Dws.Application.Configuration;
using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using JayTom.Dws.Legacy.Contracts.Dto.AppDto;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Client.Models.AppSettingModel;

namespace JayTom.Dws.Client.ViewModels.Dialog
{
    public class PasswordValidationDialogViewModel : BindableBase
    {
        private readonly ISettingsStore _settingsStore;
        private bool _isValidationPassed;
        private string _identifier = string.Empty;
        private string _password = string.Empty;
        private string _passwordHint = string.Empty;
        private SnackbarMessageQueue _passwordValidationMessageQueue = new(TimeSpan.FromSeconds(2));
        private PassWordSettingsDto _passWordSettingsDto = new();
        /// <summary>
        /// 当前连续校验失败次数。
        /// </summary>
        private int _failedAttempts;
        /// <summary>
        /// 当前临时锁定结束时间。
        /// </summary>
        private DateTimeOffset _lockedUntil = DateTimeOffset.MinValue;
        /// <summary>
        /// 触发临时锁定前允许的连续失败次数。
        /// </summary>
        private const int MaxFailedAttempts = 5;

        public PasswordValidationDialogViewModel(ISettingsStore settingsStore)
        {
            _settingsStore = settingsStore;
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
            try
            {
                _passWordSettingsDto = await _settingsStore.GetAsync<PassWordSettingsDto>("PassWordSettings") ?? new PassWordSettingsDto();
                PasswordHint = _passWordSettingsDto.PasswordHint;
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, "加载密码保护配置失败");
                PasswordValidationMessageQueue.Enqueue("密码保护配置加载失败");
            }
        }

        /// <summary>
        /// 校验密码
        /// </summary>
        public ICommand PasswordValidationCommand => new DelegateCommand<object>(PasswordValidationDelegate);

        private async void PasswordValidationDelegate(object obj)
        {
            try
            {
                await ValidatePasswordAsync();
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, "密码校验失败");
                PasswordValidationMessageQueue.Enqueue("密码校验失败");
            }
        }

        /// <summary>
        /// 执行带失败退避和临时锁定的密码校验。
        /// </summary>
        private async Task ValidatePasswordAsync()
        {
            var now = DateTimeOffset.Now;
            if (now < _lockedUntil)
            {
                var remainingSeconds = Math.Max(1, (int)Math.Ceiling((_lockedUntil - now).TotalSeconds));
                PasswordValidationMessageQueue.Enqueue($"尝试次数过多，请在 {remainingSeconds} 秒后重试");
                return;
            }

            if (_passWordSettingsDto.Password.Equals(Password))
            {
                _failedAttempts = 0;
                _lockedUntil = DateTimeOffset.MinValue;
                IsValidationPassed = true;
                if (_passWordSettingsDto.SkipPasswordValidationForThisSession)
                {
                    AppContext.SetData("IsValidationPassed", true);
                }

                if (DialogHost.IsDialogOpen(Identifier))
                {
                    DialogHost.Close(Identifier);
                }
                return;
            }

            _failedAttempts++;
            await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(_failedAttempts * 300, 1500)));
            if (_failedAttempts >= MaxFailedAttempts)
            {
                _lockedUntil = DateTimeOffset.Now.AddSeconds(30);
                _failedAttempts = 0;
                PasswordValidationMessageQueue.Enqueue("尝试次数过多，已锁定 30 秒");
                return;
            }

            PasswordValidationMessageQueue.Enqueue("密码错误");
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
