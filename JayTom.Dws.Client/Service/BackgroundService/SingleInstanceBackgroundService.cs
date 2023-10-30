using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class SingleInstanceBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private const string PipeName = "DwsPipe";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte);

                await pipeServer.WaitForConnectionAsync(stoppingToken);

                using (var sr = new StreamReader(pipeServer)) {
                    string message = await sr.ReadToEndAsync(stoppingToken);
                    if (message == "ActivateWindow") {
                        Application.Current.Dispatcher.Invoke(() => {
                            if (Application.Current.MainWindow is Window mainWindow) {
                                if (mainWindow.WindowState == WindowState.Minimized) {
                                    mainWindow.WindowState = WindowState.Normal;
                                }

                                mainWindow.Activate();
                            }
                        });
                    }
                }

                // 关闭命名管道
                pipeServer.Close();
                await pipeServer.DisposeAsync();
                await Task.Delay(50, stoppingToken);
            }
        }
    }
}