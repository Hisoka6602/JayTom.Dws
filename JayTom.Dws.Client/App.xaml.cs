using JayTom.Dws.Application.Configuration;
using DryIoc;
using System;
using System.Linq;
using Example;
using Prism.Ioc;
using System.IO;
using Prism.Mvvm;
using Prism.DryIoc;
using System.Windows;
using JayTom.Dws.Ocr;
using JayTom.Dws.Nvr;
using System.IO.Pipes;
using System.Net.Http;
using System.IO.Ports;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Plugin;
using System.Diagnostics;
using JayTom.Dws.Nvr.Nvr;
using JayTom.Dws.Interface;
using System.Globalization;
using System.Windows.Media;
using JayTom.Dws.Abstractions.Integrations.Ftp;
using System.Threading.Tasks;
using System.Windows.Interop;
using JayTom.Dws.Client.Views;
using JayTom.Dws.Plugin.Excel;
using JayTom.Dws.Plugin.Speech;
using System.Windows.Threading;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Client.Service;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Ocr.ExpressBill;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Plugin.SaveImage;
using JayTom.Dws.Client.ViewModels;
using JayTom.Dws.Interface.License;
using JayTom.Dws.Client.Views.Pages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using JayTom.Dws.Camera.BarCodeReader;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Infrastructure.Service;
using JayTom.Dws.Client.ViewModels.Pages;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Plugin.Scale.StaticScale;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Plugin.Scale.DynamicScale;
using JayTom.Dws.Domain.Repository.LocalLog;
using JayTom.Dws.Interface.Cloud.CloudVideo;
using JayTom.Dws.Client.Service.TestService;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Service.CacheCleanup;
using JayTom.Dws.Client.Service.SyncSettings;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Client.Service.ImageService;
using JayTom.Dws.Plugin.Device.KeyboardDevice;
using Microsoft.Extensions.DependencyInjection;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;
using JayTom.Dws.Client.Views.Pages.Preferences;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Service.Runtime;
using JayTom.Dws.Client.Views.Editors.CloudService;
using JayTom.Dws.Client.Service.ProcessingServices;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Infrastructure.Repository.LocalLog;
using DryIoc.Microsoft.DependencyInjection.Extension;
using JayTom.Dws.Client.ViewModels.Pages.Preferences;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Client.Service.DefaultConfiguration;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Client.ViewModels.Editors.CloudService;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Client.Views.Dialog.CameraConfiguration;
using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.Views;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Client.Views.Pages.Preferences.LogsViews;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Views.Editors.CameraConfiguration;
using JayTom.Dws.Client.Views.Pages.Preferences.AppSettings;
using JayTom.Dws.Client.Views.Pages.Preferences.CloudService;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration;
using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.ViewModels;
using JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using JayTom.Dws.Client.Views.Pages.Preferences.ApiConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.CloudService;
using JayTom.Dws.Infrastructure.SignalR.CloudApi.ClientMessageHub;
using JayTom.Dws.Infrastructure.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.LogsViewModel;
using JayTom.Dws.Client.Views.Pages.Preferences.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration;
using JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Views.Pages.Preferences.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.Views.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages;

namespace JayTom.Dws.Client
{

    /// <summary>
    /// WPF 进程宿主与依赖组合入口。
    /// </summary>
    public partial class App : PrismApplication
    {
        /// <summary>用于阻止桌面进程重复启动的互斥量。</summary>
        private Mutex? _singleInstanceMutex;
        /// <summary>用于通知已运行实例激活窗口的命名管道。</summary>
        private const string PipeName = "DwsPipe";
        /// <summary>单个设备或分拣组件在停机阶段允许占用的最长时间。</summary>
        private static readonly TimeSpan ComponentStopTimeout = TimeSpan.FromSeconds(5);

        /// <summary>向容器注册展示层、应用服务与基础设施。</summary>
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            Composition.ApplicationComposition.Register(containerRegistry);
        }

        /// <summary>创建桌面主窗口。</summary>
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        /// <summary>
        /// 创建启用连接池和写锁等待的 SQLite 连接字符串。
        /// </summary>
        /// <param name="databaseFileName">数据库文件名。</param>
        /// <returns>SQLite 连接字符串。</returns>
        private static string CreateSqliteConnectionString(string databaseFileName)
        {
            return new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, databaseFileName),
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();
        }

        /// <summary>建立单实例约束并注册全局异常处理。</summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigureRealtimeThreadPool();
            NLog.LogManager.GetCurrentClassLogger().Info("OnStartup开始");
            _singleInstanceMutex = new Mutex(true, "Dws.Client", out var createdNew);
            if (!createdNew)
            {
                // 另一个实例已经在运行，尝试激活它的窗口
                NotifyExistingInstance();
                NLog.LogManager.GetCurrentClassLogger().Error("阻止多开");
                Shutdown(0);
                return;
            }

            this.DispatcherUnhandledException += delegate (object sender, DispatcherUnhandledExceptionEventArgs args)
            {
                ReportUnhandledException(args.Exception, "UI线程未处理异常");
                args.Handled = !IsFatalException(args.Exception);
                if (!args.Handled)
                {
                    Shutdown(-2);
                }
            };
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args)
            {
                var exception = args.ExceptionObject as Exception ??
                                new InvalidOperationException(args.ExceptionObject?.ToString());
                ReportUnhandledException(exception, "应用域未处理异常");
                NLog.LogManager.Flush(TimeSpan.FromSeconds(5));
            };
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                ReportUnhandledException(args.Exception, "未观察任务异常");
                args.SetObserved();
            };
            base.OnStartup(e);

            NLog.LogManager.GetCurrentClassLogger().Info("OnStartup结束");
        }

        /// <summary>
        /// 预留足够的工作线程和异步 I/O 完成线程，避免 600 路 API 并发扩容期间饿死设备事件。
        /// </summary>
        private static void ConfigureRealtimeThreadPool()
        {
            ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
            var minimumThreads = Math.Max(64, Environment.ProcessorCount * 8);
            ThreadPool.SetMinThreads(
                Math.Max(workerThreads, minimumThreads),
                Math.Max(completionPortThreads, minimumThreads));
        }

        /// <summary>
        /// 记录未处理异常并尽力发布到应用日志，不让日志链路中的二次异常覆盖原始故障。
        /// </summary>
        private static void ReportUnhandledException(Exception exception, string source)
        {
            try
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, source);
                EventAggregator.Instance.Publish(new AppLogInfoModel
                {
                    CreateTime = DateTime.Now,
                    Message = $"{source}:{exception.Message}",
                    Type = LogType.Exception
                });
            }
            catch (Exception loggingException)
            {
                Debug.WriteLine($"记录未处理异常失败:{loggingException};原始异常:{exception}");
            }
        }

        /// <summary>判断异常是否已经不适合在当前进程中继续运行。</summary>
        private static bool IsFatalException(Exception exception) =>
            exception is OutOfMemoryException or AccessViolationException;

        /// <summary>在退出前按顺序停止应用服务并释放进程资源。</summary>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                EventAggregator.Instance.Publish(new AppLogInfoModel
                {
                    CreateTime = DateTime.Now,
                    Message = "程序关闭",
                    Type = LogType.Information
                });

                using var shutdownCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var shutdownTask = StopApplicationServicesAsync(shutdownCancellation.Token);
                var shutdownFrame = new DispatcherFrame();
                var timeoutTimer = new DispatcherTimer(
                    TimeSpan.FromSeconds(20),
                    DispatcherPriority.Send,
                    (_, _) => shutdownFrame.Continue = false,
                    Dispatcher);
                _ = shutdownTask.ContinueWith(
                    _ => shutdownFrame.Continue = false,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.FromCurrentSynchronizationContext());
                timeoutTimer.Start();
                Dispatcher.PushFrame(shutdownFrame);
                timeoutTimer.Stop();
                if (!shutdownTask.IsCompleted)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn("程序关闭资源释放超过 20 秒，继续退出");
                }
                else if (shutdownTask.Exception is not null)
                {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Error(shutdownTask.Exception, "程序关闭资源释放异常");
                }
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, "程序关闭资源释放异常");
            }
            finally
            {
                if (_singleInstanceMutex is not null)
                {
                    try
                    {
                        _singleInstanceMutex.ReleaseMutex();
                    }
                    catch (ApplicationException)
                    {
                        // 当前实例不再持有互斥锁时只需释放句柄。
                    }
                    _singleInstanceMutex.Dispose();
                    _singleInstanceMutex = null;
                }

                NLog.LogManager.Flush(TimeSpan.FromSeconds(5));
                NLog.LogManager.Shutdown();
                base.OnExit(e);
            }
        }

        /// <summary>
        /// 按依赖顺序停止设备、分拣和后台服务。
        /// </summary>
        private async Task StopApplicationServicesAsync(CancellationToken token)
        {
            var serviceProvider = Container.Resolve<IServiceProvider>();
            var deviceService = serviceProvider.GetRequiredService<IDeviceService>();
            if (deviceService.RunningStatus)
            {
                try
                {
                    await deviceService.Stop()
                        .WaitAsync(ComponentStopTimeout, token)
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Error(exception, "停止设备服务失败或超时，继续执行其余停机步骤");
                }
            }

            var sortingService = serviceProvider.GetRequiredService<ISortingService>();
            if (sortingService.RunningStatus)
            {
                try
                {
                    await sortingService.Stop()
                        .WaitAsync(ComponentStopTimeout, token)
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Error(exception, "停止分拣服务失败或超时，继续执行其余停机步骤");
                }
            }

            // 先停止设备和分拣生产者，再停止并刷新后台消费者，避免停机窗口继续产生新工作。
            var hostedServiceSupervisor = serviceProvider.GetRequiredService<IHostedServiceSupervisor>();
            await hostedServiceSupervisor.StopAsync(token).ConfigureAwait(false);
        }

        /// <summary>通过命名管道通知已运行实例激活主窗口。</summary>
        private void NotifyExistingInstance()
        {
            try
            {
                using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                pipeClient.Connect(5000); // 连接到已存在的管道
                using var sw = new StreamWriter(pipeClient);
                sw.Write("ActivateWindow");
            }
            catch (TimeoutException)
            {
                // 如果连接超时，可以处理错误情况
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger()
                    .Warn(exception, "通知已运行实例激活窗口失败");
            }
        }

        /// <summary>注册视图与视图模型的显式映射。</summary>
        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();
            Composition.ViewModelMappingRegistration.Register();
        }

        /// <summary>初始化配置并启动受管后台服务。</summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();
            _ = StartApplicationServicesAsync();
        }

        /// <summary>异步初始化配置并启动受管后台服务。</summary>
        private async Task StartApplicationServicesAsync()
        {
            try
            {
                await Task.Yield();
                await InitializeConfigurationAsync();
                // 获取 IServiceProvider
                var serviceProvider = Container.Resolve<IServiceProvider>();

                var hostedServiceSupervisor = serviceProvider
                    .GetRequiredService<IHostedServiceSupervisor>();
                await hostedServiceSupervisor.StartAsync(CancellationToken.None);
                foreach (var serviceState in hostedServiceSupervisor.GetHealthSnapshot())
                {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Info($"服务名:{serviceState.Key}，状态:{serviceState.Value}");
                }
                NLog.LogManager.GetCurrentClassLogger().Info("全部服务启动完成");
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Fatal(exception, "应用初始化失败");
                Shutdown(-1);
            }
        }

        /// <summary>
        /// 异步创建默认配置并加载当前语言。
        /// </summary>
        private async Task InitializeConfigurationAsync()
        {
            var container = Container.GetContainer();
            var defaultConfigurationService = container.Resolve<IDefaultConfigurationService>();
            await defaultConfigurationService.WriteDefaultConfiguration();

            var settingsStore = container.Resolve<ISettingsStore>();
            var language = await settingsStore.GetRawAsync("Language");
            if (!string.IsNullOrWhiteSpace(language))
            {
                var culture = new CultureInfo(language);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
        }
    }
}
