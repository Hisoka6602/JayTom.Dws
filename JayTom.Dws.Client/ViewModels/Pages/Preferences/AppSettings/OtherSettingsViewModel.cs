using JayTom.Dws.Application.Configuration;
using System;
using System.IO;
using Prism.Mvvm;
using Prism.Commands;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Models.LocalConf;
using System.Security.Principal;
using System.Windows.Media.Imaging;
using JayTom.Dws.Legacy.Contracts.Dto.AppDto;
using JayTom.Dws.Legacy.Contracts.Dto.BaseInfoModels;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Client.Models.AppSettingModel;
using JayTom.Dws.Client.Models.OcrSettingsModel;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings
{

    public class OtherSettingsViewModel : SettingsPageTemplateViewModel
    {
        private OtherSettingsModel _otherSettingsInfo = new();
        private ImageSource? _icon;
        private string _fileName = string.Empty;
        private bool _isLoaded;

        public OtherSettingsViewModel(ISettingsStore settingsStore, JayTom.Dws.Application.Messaging.IEventBus eventBus) : base(settingsStore, eventBus)
        {
        }

        public OtherSettingsModel OtherSettingsInfo
        {
            get => _otherSettingsInfo;
            set => SetProperty(ref _otherSettingsInfo, value);
        }

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        /// <summary>
        /// 图标
        /// </summary>
        public ImageSource? Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public override string Identifier => "OtherSettingsDialogHost";
        public override string SettingsName => "OtherSettings";

        protected override async Task<bool> SaveSettingsProcess()
        {
            try
            {
                if (!Directory.Exists($"{AppContext.BaseDirectory}Logo"))
                {
                    //创建图片路径
                    Directory.CreateDirectory($"{AppContext.BaseDirectory}Logo");
                }
                var dest = string.Empty;
                if (!string.IsNullOrEmpty(FileName))
                {
                    dest = $"{AppContext.BaseDirectory}Logo\\{new FileInfo(FileName).Name}";
                    File.Copy(FileName, dest, true);
                }

                var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,new OtherSettingsDto()
                    {
                        IsAutoMaximize = OtherSettingsInfo.IsAutoMaximize,
                        IsAutoStart = OtherSettingsInfo.IsAutoStart,
                        ProgramTitle = OtherSettingsInfo.ProgramTitle,
                        ProgramLogoPath = dest,
                        IsAutoRunEnabled = OtherSettingsInfo.IsAutoRunEnabled
                    });
                if (insertOrUpdate)
                {
                    SetAutoRun(OtherSettingsInfo.IsAutoRunEnabled);
                    base.MessageQueue.Enqueue($"保存成功!");
                    return true;
                }
                else
                {
                    base.MessageQueue.Enqueue($"保存失败!");
                }
            }
            catch (Exception e)
            {
                base.MessageQueue.Enqueue(e.Message);
            }

            return false;
        }

        public override async void LoadedDelegate(object obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var otherSettingsDto = await _settingsStore.GetAsync<OtherSettingsDto>(SettingsName);
                    if (otherSettingsDto is not null)
                    {
                        OtherSettingsInfo = new OtherSettingsModel()
                        {
                            IsAutoMaximize = otherSettingsDto.IsAutoMaximize,
                            IsAutoStart = otherSettingsDto.IsAutoStart,
                            ProgramLogoPath = otherSettingsDto.ProgramLogoPath,
                            ProgramTitle = otherSettingsDto.ProgramTitle,
                            IsAutoRunEnabled = otherSettingsDto.IsAutoRunEnabled
                        };
                    }
                    else
                    {
                        base.MessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("加载设置失败") ?? string.Empty}");
                    }
                    //检查图片是否存在
                    //加载图片
                    if (File.Exists(OtherSettingsInfo.ProgramLogoPath))
                    {
                        Icon = CreateBitmapImage(new Uri(OtherSettingsInfo.ProgramLogoPath), 30, 30);
                    }
                });
            }
        }

        public ICommand LoadImageCommand
        {
            get => new DelegateCommand<object>(LoadImageDelegate);
        }

        private async void LoadImageDelegate(object obj)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = @"*.PNG|*.PNG",
                InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "请选择图像文件",
                RestoreDirectory = true,
            };
            var showDialog = openFileDialog.ShowDialog();
            if (showDialog == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(openFileDialog.FileName))
                {
                    await UiThread.Dispatcher.InvokeAsync(() =>
                    {
                        Icon = CreateBitmapImage(new Uri(openFileDialog.FileName), 30, 30);
                        FileName = openFileDialog.FileName;
                    });
                }
            }
        }

        public BitmapImage CreateBitmapImage(Uri uri, int width, int height)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = uri;
                image.DecodePixelHeight = height;
                image.DecodePixelWidth = width;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private void SetAutoRun(bool enable)
        {
            var isAdministrator = IsAdministrator();
            var mainModuleFileName = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(mainModuleFileName))
            {
                using (var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (enable)
                    {
                        // 设置开机自动运行
                        key?.SetValue("Dws", System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe"));
                    }
                    else
                    {
                        // 取消开机自动运行
                        key?.DeleteValue("Dws", false);
                    }
                }
            }
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}