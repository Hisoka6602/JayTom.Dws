using JayTom.Dws.Application.Configuration;
using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.AppDto;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.AppSettingModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings
{

    public class PassWordSettingsViewModel : SettingsPageTemplateViewModel
    {
        public PassWordSettingsModel PassWordSettingsInfo
        {
            get;
            set => SetProperty(ref field, value);
        } = new();

        public PassWordSettingsViewModel(ISettingsStore settingsStore) : base(settingsStore)
        {
        }

        public override string Identifier => "SettingDialog";
        public override string SettingsName => "PassWordSettings";

        public override async void LoadedDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var settingsDto = await _settingsStore.GetAsync<PassWordSettingsDto>(SettingsName) ??
                                  new PassWordSettingsDto();
                PassWordSettingsInfo = new PassWordSettingsModel()
                {
                    Password = settingsDto.Password,
                    IsUsePasswordProtection = settingsDto.IsUsePasswordProtection,
                    PasswordHint = settingsDto.PasswordHint,
                    SkipPasswordValidationForThisSession = settingsDto.SkipPasswordValidationForThisSession,
                };
                foreach (var model in PassWordSettingsInfo.PasswordProtectionModuleItems)
                {
                    model.IsProtected = settingsDto.PasswordProtectionModuleItems.Any(a =>
                        a.PageClassName.Equals(model.PageClassName) && a.IsProtected);
                }
            });
        }

        protected override async Task<bool> SaveSettingsProcess()
        {
            if (!PassWordSettingsInfo.Password.Equals(PassWordSettingsInfo.ConfirmPassword))
            {
                base.MessageQueue.Enqueue("两次输入的密码不一致");
                return false;
            }

            //var encryptString = PluginInterface.Utils.Utils.EncryptString(PassWordSettingsInfo.Password);
            var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,new PassWordSettingsDto()
                {
                    Password = PassWordSettingsInfo.Password,
                    IsUsePasswordProtection = PassWordSettingsInfo.IsUsePasswordProtection,
                    PasswordHint = PassWordSettingsInfo.PasswordHint,
                    SkipPasswordValidationForThisSession = PassWordSettingsInfo.SkipPasswordValidationForThisSession,
                    PasswordProtectionModuleItems = [.. PassWordSettingsInfo.PasswordProtectionModuleItems.Select(s =>
                        new PasswordProtectionModuleInfo {
                            Description = s.Description,
                            IsProtected = s.IsProtected,
                            PageClassName = s.PageClassName
                        })]
                });
            AppContext.SetData("IsValidationPassed", false);
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }
    }
}
