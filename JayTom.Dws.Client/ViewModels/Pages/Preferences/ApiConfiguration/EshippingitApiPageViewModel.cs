using System;
using System.IO;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;
using System.Windows.Input;
using System.Windows.Forms;
using JayTom.Dws.Domain.Dto;
using System.Windows.Shapes;
using Path = System.IO.Path;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalConf;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;
using JayTom.Dws.Interface.ApiImplementations.Eshippingit;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration
{

    public class EshippingitApiPageViewModel : SettingsPageTemplateViewModel {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDeviceService _deviceService;
        private readonly IPackageRepository _packageRepository;
        private readonly IBarCodeRepository _barCodeRepository;
        private EshippingitApiModel _eshippingitApiInfo = new();
        private string _progressText = $"0%";
        private double _progress;
        private bool _isUploadProgress;
        private double _maxProgress;
        private string _imageRootDirectory = string.Empty;
        private int _successfulUploads;
        private int _failedUploads;
        private SemaphoreSlim _uploadSemaphore = new(7);

        public EshippingitApiModel EshippingitApiInfo {
            get => _eshippingitApiInfo;
            set => SetProperty(ref _eshippingitApiInfo, value);
        }

        public string ProgressText {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        public bool IsUploadProgress {
            get => _isUploadProgress;
            set => SetProperty(ref _isUploadProgress, value);
        }

        public double Progress {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public double MaxProgress {
            get => _maxProgress;
            set => SetProperty(ref _maxProgress, value);
        }

        public string ImageRootDirectory {
            get => _imageRootDirectory;
            set => SetProperty(ref _imageRootDirectory, value);
        }

        public int SuccessfulUploads {
            get => _successfulUploads;
            set => SetProperty(ref _successfulUploads, value);
        }

        public int FailedUploads {
            get => _failedUploads;
            set => SetProperty(ref _failedUploads, value);
        }

        public EshippingitApiPageViewModel(IConfigRepository configRepository,
            IHttpClientFactory httpClientFactory,
            IDeviceService deviceService,
            IPackageRepository packageRepository,
            IBarCodeRepository barCodeRepository) : base(configRepository) {
            _httpClientFactory = httpClientFactory;
            _deviceService = deviceService;
            _packageRepository = packageRepository;
            _barCodeRepository = barCodeRepository;
        }

        public override string Identifier => "EshippingitApiParametersDialogHost";
        public override string SettingsName => "EshippingitApiParameters";

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new EshippingitApiDto() {
                    Domain = EshippingitApiInfo.Domain,
                    TimeOut = EshippingitApiInfo.TimeOut,
                    Authorization = EshippingitApiInfo.Authorization,
                    Endpoint = EshippingitApiInfo.Endpoint,
                    BucketName = EshippingitApiInfo.BucketName,
                    RetryCount = EshippingitApiInfo.RetryCount,
                    RetryInterval = EshippingitApiInfo.RetryInterval,
                    Machine = EshippingitApiInfo.Machine
                })
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var settingsDto = await _configRepository.FirstOrDefaultEntity<EshippingitApiDto>(SettingsName) ?? new EshippingitApiDto();
                EshippingitApiInfo = new EshippingitApiModel() {
                    Domain = settingsDto.Domain,
                    TimeOut = settingsDto.TimeOut,
                    Authorization = settingsDto.Authorization,
                    Endpoint = settingsDto.Endpoint,
                    BucketName = settingsDto.BucketName,
                    RetryCount = settingsDto.RetryCount,
                    RetryInterval = settingsDto.RetryInterval,
                    Machine = settingsDto.Machine
                };
                var imageSettingsDto = await _configRepository.FirstOrDefaultEntity<ImageSettingsDto>("SaveImageSettings") ?? new ImageSettingsDto();
                if (!string.IsNullOrEmpty(imageSettingsDto.ImageRootDirectory)) {
                    ImageRootDirectory = imageSettingsDto.ImageRootDirectory;
                }
            });
        }

        public ICommand OpenFolderCommand => new DelegateCommand<object>(OpenFolderDelegate);

        private void OpenFolderDelegate(object obj) {
            var folderBrowserDialog = new FolderBrowserDialog() {
                SelectedPath = string.IsNullOrEmpty(ImageRootDirectory) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : ImageRootDirectory
            };
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                ImageRootDirectory = folderBrowserDialog.SelectedPath;
            }
        }

        public ICommand UploadImageCommand => new DelegateCommand<object>(UploadImageDelegate);

        private async void UploadImageDelegate(object obj) {
            if (IsUploadProgress) {
                return;
            }
            IsUploadProgress = true;
            if (_deviceService.RunningStatus) {
                base.MessageQueue.Enqueue($"设备工作中,为避免文件冲突无法手动上传");
                await Task.Delay(600);
                IsUploadProgress = false;
                return;
            }

            Task.Run(async () => {
                if (!string.IsNullOrEmpty(ImageRootDirectory)) {
                    List<string> uploadedList = new();

                    var jsonFilePath = Path.Combine(ImageRootDirectory, "uploadedList.json");
                    if (File.Exists(jsonFilePath)) {
                        await using var fsa = File.OpenRead(jsonFilePath);
                        uploadedList = await System.Text.Json.JsonSerializer.DeserializeAsync<List<string>>(fsa) ?? new List<string>();
                    }

                    var imageFileRegex = new Regex(@"\.(jpg|jpeg|png|gif|bmp)$", RegexOptions.IgnoreCase);

                    var imageFiles = GetImageFiles(ImageRootDirectory, imageFileRegex).ToList();

                    var difference = imageFiles.Except(uploadedList).ToList();

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        MaxProgress = imageFiles.Count;
                        Progress = 0;
                        SuccessfulUploads = 0;
                        FailedUploads = 0;
                    });

                    //MaxProgress
                    var process = await UploadImageProcess(difference.ToList());

                    //写出Json文件

                    await using var fs = File.Create(jsonFilePath);
                    await System.Text.Json.JsonSerializer.SerializeAsync(fs, process);
                }
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    //恢复按钮
                    IsUploadProgress = false;
                    return Task.CompletedTask;
                });
            });
        }

        public async Task<List<string>> UploadImageProcess(List<string> imagesPath) {
            var uploadedList = new ConcurrentBag<string>();
            var eshippingitApi = new EshippingitApi(_httpClientFactory);
            var (key, value) = await eshippingitApi.SetParameters(new EshippingitApi.ApiParameters() {
                Authorization = EshippingitApiInfo.Authorization,
                BucketName = EshippingitApiInfo.BucketName,
                Domain = EshippingitApiInfo.Domain,
                Endpoint = EshippingitApiInfo.Endpoint,
                Machine = EshippingitApiInfo.Machine,
                RetryCount = EshippingitApiInfo.RetryCount,
                RetryInterval = EshippingitApiInfo.RetryInterval,
                TimeOut = EshippingitApiInfo.TimeOut
            });

            if (!key) {
                MessageQueue.Enqueue("参数错误!");
                return uploadedList.ToList();
            }

            var progress = new Progress<(int, bool)>(async valueTuple => {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    lock (uploadedList) {
                        Progress += 1;
                        ProgressText = $"{(Progress / imagesPath.Count):P2}";
                        if (valueTuple.Item2) {
                            SuccessfulUploads++;
                        }
                        else {
                            FailedUploads++;
                        }
                    }
                });
            });

            var barCodeInfoModels = await _barCodeRepository.SelectOrderByDescending(w =>
                imagesPath.Select(Path.GetFileNameWithoutExtension).Contains(w.Barcode), o => o.ScanTime);

            NLog.LogManager.GetCurrentClassLogger().Error($"barCodeInfoModels:{barCodeInfoModels.Count}");

            /*var tasks = imagesPath.Select((path, i) => UploadImageAsync(path, eshippingitApi,
            uploadedList, progress,
            barCodeInfoModels.FirstOrDefault(f => f.Barcode.Equals(path))?.ScanTime ?? File.GetLastWriteTime(path),
                i));*/
            var tasks = imagesPath.Select(async (path, i) => {
                await Task.Delay(TimeSpan.FromMilliseconds(5)); // 添加时间间隔
                await UploadImageAsync(path, eshippingitApi,
                     uploadedList, progress,
                     barCodeInfoModels.FirstOrDefault(f => f.Barcode.Equals(path))?.ScanTime ??
                     File.GetLastWriteTime(path), i);
            });
            await Task.WhenAll(tasks);

            return uploadedList.ToList();
        }

        private async Task UploadImageAsync(string path, EshippingitApi eshippingitApi, ConcurrentBag<string> uploadedList, IProgress<(int, bool)> progress, DateTime scanTime, int num) {
            try {
                await _uploadSemaphore.WaitAsync();
                using var image = Image.FromFile(path);
                var policyPush = await eshippingitApi.PolicyPush(Path.GetFileNameWithoutExtension(path),
                    scanTime,
                    image);

                if (policyPush) {
                    uploadedList.Add(path);
                }
                // 报告进度和更新成功和失败上传数
                progress.Report((num, policyPush));
            }
            finally {
                _uploadSemaphore.Release();
            }
        }

        private IEnumerable<string> GetImageFiles(string folderPath, Regex regex) {
            var files = Directory.GetFiles(folderPath);
            var subFolders = Directory.GetDirectories(folderPath);

            var imageFiles = files?.Where(fileName => regex.IsMatch(Path.GetExtension(fileName)))?.ToList() ?? new List<string>();

            foreach (var subFolder in subFolders) {
                var subFolderImageFiles = GetImageFiles(subFolder, regex);
                imageFiles.AddRange(subFolderImageFiles);
            }

            return imageFiles;
        }
    }
}