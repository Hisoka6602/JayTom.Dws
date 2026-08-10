using System;
using System.IO;
using System.Windows;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    public class SingleInstanceBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private static volatile bool _isWindowsClose;
        private const string PipeName = "DwsPipe";

        public SingleInstanceBackgroundService()
        {
            EventAggregator.Instance.Subscribe<WindowsAction>(item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    _isWindowsClose = true;
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose)
            {
                try
                {
                    await using var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await pipeServer.WaitForConnectionAsync(stoppingToken).ConfigureAwait(false);

                    using var reader = new StreamReader(pipeServer);
                    var message = await reader.ReadToEndAsync(stoppingToken).ConfigureAwait(false);
                    if (message == "ActivateWindow")
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (System.Windows.Application.Current.MainWindow is { } mainWindow)
                            {
                                if (mainWindow.WindowState == WindowState.Minimized)
                                {
                                    mainWindow.WindowState = WindowState.Normal;
                                }
                                mainWindow.Activate();
                            }
                        });
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (IOException)
                {
                    //客户端提前断开时继续等待下一次激活请求。
                }
                catch (Exception e)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    // 命名管道若持续初始化失败，避免无延迟重试耗尽 CPU 并形成日志洪峰。
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                }
            }
        }
    }
}
