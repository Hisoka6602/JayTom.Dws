using JayTom.Dws.Application.Configuration;
using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Unicode;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using System.Collections.ObjectModel;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ImageSettingModels;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration
{

    public class DefaultApiPageViewModel : SettingsPageTemplateViewModel
    {
        private DefaultApiModel _defaultApiInfo = new();
        private string _jsonContent = string.Empty;
        private bool _isLoaded;

        public DefaultApiPageViewModel(ISettingsStore settingsStore) : base(settingsStore)
        {
        }

        /// <summary>
        /// 数据
        /// </summary>
        public DefaultApiModel DefaultApiInfo
        {
            get => _defaultApiInfo;
            set => SetProperty(ref _defaultApiInfo, value);
        }

        /// <summary>
        /// Json模板内容
        /// </summary>
        public string JsonContent
        {
            get => _jsonContent;
            set => SetProperty(ref _jsonContent, value);
        }

        /// <summary>
        /// 添加数据模板
        /// </summary>
        public ICommand AddDataItemCommand
        {
            get => new DelegateCommand<string>(AddDataItemDelegate);
        }

        private async void AddDataItemDelegate(string obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                obj = obj.Replace("'", string.Empty);
                if (!DefaultApiInfo.DataTemplate.Any(a => a.Content.Equals(obj)))
                {
                    DefaultApiInfo.DataTemplate.Add(new ItemBaseTemplateModel
                    {
                        Content = obj,
                        Id = DefaultApiInfo.DataTemplate.Count,
                        Type = 1,
                        ApplicationType = ItemApplicationType.ApiData
                    });
                    JsonContent = ChangeJsonContent(DefaultApiInfo.DataTemplate);
                }
            });
        }

        /// <summary>
        /// 添加数据模板(自定义)
        /// </summary>
        public ICommand AddCustomItemCommand
        {
            get => new DelegateCommand<string>(AddCustomItemDelegate);
        }

        private async void AddCustomItemDelegate(string obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DefaultApiInfo.DataTemplate.Add(new ItemBaseTemplateModel
                {
                    Content = obj,
                    Id = DefaultApiInfo.DataTemplate.Count,
                    Type = 3,
                    ApplicationType = ItemApplicationType.ApiData
                });
                JsonContent = ChangeJsonContent(DefaultApiInfo.DataTemplate);
            });
        }

        /// <summary>
        /// 移除标记
        /// </summary>
        public ICommand RemoveTemplateItemCommand
        {
            get => new DelegateCommand<ItemBaseTemplateModel>(RemoveTemplateItemDelegate);
        }

        private async void RemoveTemplateItemDelegate(ItemBaseTemplateModel model)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (model.ApplicationType == ItemApplicationType.ApiData)
                {
                    DefaultApiInfo.DataTemplate.Remove(model);
                    foreach (var item in DefaultApiInfo.DataTemplate)
                    {
                        if (item.Type == 0 && string.IsNullOrEmpty(item.Content) &&
                            DefaultApiInfo.DataTemplate.LastOrDefault() != item)
                        {
                            DefaultApiInfo.DataTemplate.Remove(item);
                        }
                    }
                }
                JsonContent = ChangeJsonContent(DefaultApiInfo.DataTemplate);
            });
        }

        public override string Identifier => "DefaultApiParametersDialogHost";
        public override string SettingsName => "DefaultApiParameters";

        protected override async Task<bool> SaveSettingsProcess()
        {
            var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,new DefaultApiDto
                {
                    CompleteMatch = DefaultApiInfo.CompleteMatch,
                    DataTemplate = DefaultApiInfo.DataTemplate.Select(s =>
                        new ItemTemplateInfo()
                        {
                            ApplicationType = s.ApplicationType,
                            Content = s.Content,
                            Type = s.Type
                        })?.ToList() ?? new List<ItemTemplateInfo>(),
                    IsUseJsonUpload = DefaultApiInfo.IsUseJsonUpload,
                    RegularExpression = DefaultApiInfo.RegularExpression,
                    StringContains = DefaultApiInfo.StringContains,
                    Timeout = TimeSpan.FromMilliseconds(DefaultApiInfo.Timeout),
                    Url = DefaultApiInfo.Url,
                    ValidationMode = DefaultApiInfo.ValidationMode,
                    JsonTemplate = JsonContent,
                    StringTemplate = string.Join(",", DefaultApiInfo.DataTemplate.Where(w => w.ApplicationType == ItemApplicationType.ApiData)
                        .Select(s => s.Content)?.ToList() ?? new List<string>()),
                    IsUploadPanoramaImage = DefaultApiInfo.IsUploadPanoramaImage,
                    IsUploadScanImage = DefaultApiInfo.IsUploadScanImage,
                    IsUseUploadImage = DefaultApiInfo.IsUseUploadImage
                });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;

                await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var settingsDto = await _settingsStore.GetAsync<DefaultApiDto>(SettingsName) ?? new DefaultApiDto();

                    var templateModels = settingsDto.DataTemplate.Select(s => new ItemBaseTemplateModel()
                    {
                        ApplicationType = s.ApplicationType,
                        Content = s.Content,
                        Type = s.Type
                    })?.ToList();

                    DefaultApiInfo = new DefaultApiModel()
                    {
                        CompleteMatch = settingsDto.CompleteMatch,
                        DataTemplate = new ObservableCollection<ItemBaseTemplateModel>(),
                        IsUseJsonUpload = settingsDto.IsUseJsonUpload,
                        RegularExpression = settingsDto.RegularExpression,
                        StringContains = settingsDto.StringContains,
                        Timeout = (int)settingsDto.Timeout.TotalMilliseconds,
                        Url = settingsDto.Url,
                        ValidationMode = settingsDto.ValidationMode,
                        IsUploadPanoramaImage = settingsDto.IsUploadPanoramaImage,
                        IsUploadScanImage = settingsDto.IsUploadScanImage,
                        IsUseUploadImage = settingsDto.IsUseUploadImage
                    };
                    DefaultApiInfo.DataTemplate.AddRange(templateModels);
                    JsonContent = ChangeJsonContent(DefaultApiInfo.DataTemplate);
                });
            }
        }

        private string ChangeJsonContent(ObservableCollection<ItemBaseTemplateModel> dataTemplate)
        {
            var dictionary = new Dictionary<string, object>();
            var num = 1;
            foreach (var templateModel in dataTemplate)
            {
                if (templateModel.ApplicationType == ItemApplicationType.ApiData)
                {
                    switch (templateModel.Type)
                    {
                        case 1:
                            {
                                var replace = templateModel.Content.Replace("{", string.Empty).Replace("}", string.Empty);
                                dictionary.Add(replace, $"{replace}Value");
                                break;
                            }
                        case 3:
                            dictionary.Add($"Custom{num}", $"{templateModel.Content}");
                            num++;
                            break;
                    }
                }
            }
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };
            return JsonSerializer.Serialize(dictionary, options);
        }
    }
}