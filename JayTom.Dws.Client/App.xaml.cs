using JayTom.Dws.Application.Messaging;
using Prism.DryIoc;
using Prism.Ioc;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using JayTom.Dws.Client.Events;
using JayTom.Dws.Client.Service.Runtime;
using JayTom.Dws.Client.Views;
using JayTom.Dws.Models.LocalLog;

namespace JayTom.Dws.Client
{

    /// <summary>
    /// WPF 进程宿主与依赖组合入口。
    /// </summary>
    public partial class App : PrismApplication
    {
        /// <summary>优雅停机允许占用的最长时间。</summary>
        private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(20);
        /// <summary>退出事件开始后强制终止残留进程的最后期限。</summary>
        private static readonly TimeSpan ForcedExitDelay = TimeSpan.FromSeconds(30);
        /// <summary>用于阻止桌面进程重复启动的互斥量。</summary>
        private Mutex? _singleInstanceMutex;
        /// <summary>应用级生命周期令牌，关闭请求可取消尚未完成的异步启动。</summary>
        private readonly CancellationTokenSource _applicationLifetimeCancellation = new();
        /// <summary>优雅停机失效时保证进程最终终止的看门狗。</summary>
        private static Timer? _forcedExitTimer;
        /// <summary>标记当前实例是否实际持有单实例互斥量。</summary>
        private bool _ownsSingleInstanceMutex;
        /// <summary>标记 Prism 容器已经可以安全解析应用服务。</summary>
        private int _compositionInitialized;
        /// <summary>标记 WPF 与 Prism 启动流程已经完整结束。</summary>
        private int _startupCompleted;
        /// <summary>标记应用已经收到退出请求。</summary>
        private int _shutdownRequested;
        /// <summary>用于通知已运行实例激活窗口的命名管道。</summary>
        private const string PipeName = "DwsPipe";

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

        /// <summary>建立单实例约束并注册全局异常处理。</summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigureRealtimeThreadPool();
            NLog.LogManager.GetCurrentClassLogger().Info(
                "OnStartup开始:PID={ProcessId},BaseDirectory={BaseDirectory},WorkingDirectory={WorkingDirectory}",
                Environment.ProcessId,
                AppContext.BaseDirectory,
                Environment.CurrentDirectory);
            _singleInstanceMutex = new Mutex(true, "Dws.Client", out var createdNew);
            _ownsSingleInstanceMutex = createdNew;
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
                args.Handled = Volatile.Read(ref _startupCompleted) != 0 &&
                               Volatile.Read(ref _shutdownRequested) == 0 &&
                               !IsFatalException(args.Exception);
                if (!args.Handled)
                {
                    RequestApplicationShutdown(-2);
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
            try
            {
                base.OnStartup(e);
                Volatile.Write(ref _startupCompleted, 1);
                NLog.LogManager.GetCurrentClassLogger().Info("OnStartup结束:PID={ProcessId}", Environment.ProcessId);
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Fatal(exception, "应用启动失败，正在退出");
                RequestApplicationShutdown(-1);
            }
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
        private void ReportUnhandledException(Exception exception, string source)
        {
            try
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, source);
                Container.Resolve<IEventBus>().Publish(new AppLogInfoModel
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
            Interlocked.Exchange(ref _shutdownRequested, 1);
            ArmForcedExitWatchdog(e.ApplicationExitCode);
            NLog.LogManager.GetCurrentClassLogger().Info(
                "OnExit开始:PID={ProcessId},ExitCode={ExitCode},CompositionInitialized={CompositionInitialized}",
                Environment.ProcessId,
                e.ApplicationExitCode,
                Volatile.Read(ref _compositionInitialized) != 0);
            try
            {
                _applicationLifetimeCancellation.Cancel();
                if (Volatile.Read(ref _compositionInitialized) != 0)
                {
                    var lifecycleCoordinator = Container.Resolve<IApplicationLifecycleCoordinator>();
                    lifecycleCoordinator.BeginShutdown();
                    PublishCloseNotification();
                    using var shutdownCancellation = new CancellationTokenSource(ShutdownTimeout);
                    WaitForShutdown(
                        lifecycleCoordinator.StopAsync(shutdownCancellation.Token),
                        ShutdownTimeout);
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
                    if (_ownsSingleInstanceMutex)
                    {
                        try
                        {
                            _singleInstanceMutex.ReleaseMutex();
                        }
                        catch (ApplicationException exception)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Warn(exception, "释放单实例互斥量失败");
                        }
                    }
                    _singleInstanceMutex.Dispose();
                    _singleInstanceMutex = null;
                    _ownsSingleInstanceMutex = false;
                }

                NLog.LogManager.GetCurrentClassLogger().Info("OnExit完成:PID={ProcessId}", Environment.ProcessId);
                NLog.LogManager.Flush(TimeSpan.FromSeconds(5));
                NLog.LogManager.Shutdown();
                base.OnExit(e);
            }
        }

        /// <summary>广播兼容的窗口关闭事件；监督器停机屏障必须先于此事件建立。</summary>
        private void PublishCloseNotification()
        {
            try
            {
                Container.Resolve<IEventBus>().Publish(new WindowsAction
                {
                    Type = WindowsActionType.Close
                });
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, "发布程序关闭通知失败");
            }
        }

        /// <summary>在 UI 消息循环仍可调度时有界等待异步资源释放。</summary>
        private void WaitForShutdown(Task shutdownTask, TimeSpan timeout)
        {
            var shutdownFrame = new DispatcherFrame();
            var timeoutTimer = new DispatcherTimer(
                timeout,
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
                NLog.LogManager.GetCurrentClassLogger().Warn("程序关闭资源释放超过 {TimeoutSeconds} 秒", timeout.TotalSeconds);
            }
            else if (shutdownTask.IsFaulted)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(shutdownTask.Exception, "程序关闭资源释放异常");
            }
            else if (shutdownTask.IsCanceled)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn("程序关闭资源释放已取消");
            }
        }

        /// <summary>即使遗留前台线程拒绝退出，也在最后期限到达后终止当前进程。</summary>
        private void ArmForcedExitWatchdog(int exitCode)
        {
            _forcedExitTimer = new Timer(
                static state =>
                {
                    Trace.WriteLine("优雅停机超过最终期限，强制终止进程。");
                    Environment.Exit((int)state!);
                },
                exitCode,
                ForcedExitDelay,
                Timeout.InfiniteTimeSpan);
        }

        /// <summary>从任意线程安全地请求 WPF 应用退出。</summary>
        private void RequestApplicationShutdown(int exitCode)
        {
            Interlocked.Exchange(ref _shutdownRequested, 1);
            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
            {
                return;
            }
            if (Dispatcher.CheckAccess())
            {
                Shutdown(exitCode);
                return;
            }
            _ = Dispatcher.BeginInvoke(() => Shutdown(exitCode));
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
                NLog.LogManager.GetCurrentClassLogger().Warn("已运行实例未响应窗口激活请求");
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
            Volatile.Write(ref _compositionInitialized, 1);
            StartApplicationServicesAsync().Forget("启动应用服务");
        }

        /// <summary>异步初始化配置并启动受管后台服务。</summary>
        private async Task StartApplicationServicesAsync()
        {
            try
            {
                await Container.Resolve<IApplicationLifecycleCoordinator>()
                    .StartAsync(_applicationLifetimeCancellation.Token);
                NLog.LogManager.GetCurrentClassLogger().Info("全部服务启动完成");
            }
            catch (OperationCanceledException) when (_applicationLifetimeCancellation.IsCancellationRequested)
            {
                NLog.LogManager.GetCurrentClassLogger().Info("应用关闭期间取消后台服务启动");
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Fatal(exception, "应用初始化失败");
                RequestApplicationShutdown(-1);
            }
        }

    }
}
