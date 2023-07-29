using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.Generic;

namespace WpfApp1 {

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        protected override void OnStartup(StartupEventArgs e) {
            // 订阅 DispatcherUnhandledException 事件
            this.DispatcherUnhandledException += delegate (object sender, DispatcherUnhandledExceptionEventArgs args) {
                File.AppendAllLines($"{Directory.GetCurrentDirectory()}\\异常日志.txt",
                    new[] { args.Exception.ToString() });
            };
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args) {
                File.AppendAllLines($"{Directory.GetCurrentDirectory()}\\异常日志.txt",
                    new[] { args.ExceptionObject.ToString() });
            };
            ThreadPool.SetMinThreads(300, 300);
            base.OnStartup(e);
        }
    }
}