using System;
using S7.Net;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Service.Sorting {

    public class DefaultExitMonitor : IExitMonitor {
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;

        public event EventHandler<PackageExitDefinitionInfoModel>? LockExitEvent;

        public event EventHandler<PackageExitDefinitionInfoModel>? UnLockExitEvent;

        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        public bool IsConnected { get; private set; } = false;
        private static Task? _monitorThread;
        private static CancellationTokenSource? _cancellationTokenSource;
        private static List<PackageExitDefinitionInfoModel> _definitionInfoModels = new();
        private Plc? plc;

        public DefaultExitMonitor(IPackageExitDefinitionRepository packageExitDefinitionRepository) {
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
        }

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default) {
            try {
                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath($"{AppContext.BaseDirectory}ConfigurationFiles\\SortingConfigFiles")
                    .AddJsonFile("WxckExitMonitorSorting.json", optional: false, reloadOnChange: true)
                    .Build();
                var ip = configuration["S7300:Ip"] ?? "127.0.0.1";
                var timeOut = Convert.ToInt32(configuration["S7300:TimeOut"] ?? "2000");
                var rack = Convert.ToInt16(configuration["S7300:Rack"] ?? "0");
                var slot = Convert.ToInt16(configuration["S7300:Slot"] ?? "2");
                var db = Convert.ToInt32(configuration["S7300:Db"] ?? "1");
                _definitionInfoModels = await _packageExitDefinitionRepository.
                    Select(s => s.Id > 0,
                        o => o.Id, token);
                plc ??= new Plc(CpuType.S7300, ip, rack, slot);
                await plc.OpenAsync(token);

                plc.ReadTimeout = timeOut;
                if (_monitorThread is null) {
                    _cancellationTokenSource = new CancellationTokenSource();
                    _monitorThread = Task.Run(async () => {
                        var max = _definitionInfoModels.Max(f => Regex.Match(f.ExitName, @"\d+").Value) ?? "255";
                        var maxByteId = Convert.ToInt32(max);
                        var indexByteId = 1;
                        while (!_cancellationTokenSource.IsCancellationRequested) {
                            await Task.Delay(80, token);
                            try {
                                if (indexByteId > maxByteId) {
                                    indexByteId = 1;
                                }
                                if (plc is not null) {
                                    var readBytesAsync =
                                        await plc.ReadBytesAsync(DataType.DataBlock, db, indexByteId - 1, 1, token);
                                    var id = indexByteId;
                                    var infoModel = _definitionInfoModels.FirstOrDefault(f => Regex.Match(f.ExitName, @"\d+").Value == id.ToString());

                                    if (readBytesAsync?.FirstOrDefault() == 0) {
                                        //解锁
                                        if (infoModel?.IsLockExit == true) {
                                            OnUnLockExitEvent(infoModel);
                                            infoModel.IsLockExit = false;
                                        }
                                    }
                                    else {
                                        // 锁
                                        if (infoModel?.IsLockExit == false) {
                                            OnLockExitEvent(infoModel);
                                            infoModel.IsLockExit = true;
                                        }
                                    }
                                }
                            }
                            catch (Exception e) {
                                OnExceptionOccurred(new ExceptionEventArgs() {
                                    ExceptionMessage = e.Message
                                });
                            }
                            finally {
                                indexByteId++;
                            }
                        }
                    }, token);
                }
                return new KeyValuePair<bool, string>(true, "连接成功");
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = e.Message
                });
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default) {
            //停止S7
            //停止循环线程
            try {
                await Task.Yield();
                _cancellationTokenSource?.Cancel();
                if (_monitorThread != null) {
                    await _monitorThread;
                    _monitorThread?.Dispose();
                }

                _monitorThread = null;

                plc?.Close();
                plc = null;
                return new KeyValuePair<bool, string>(true, "断开成功");
            }
            catch (Exception e) {
                OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = e.Message
                });
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            throw new NotImplementedException();
        }

        protected virtual async void OnExceptionOccurred(ExceptionEventArgs e) {
            await Task.Yield();
            EventAggregator.Instance.Publish(new SortingLogInfoModel {
                CreateTime = DateTime.Now,
                Message = $"格口监控服务异常:{e.ExceptionMessage}",
                Type = LogType.Exception
            });
            ExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnLockExitEvent(PackageExitDefinitionInfoModel e) {
            await Task.Yield();
            EventAggregator.Instance.Publish(new SortingLogInfoModel {
                CreateTime = DateTime.Now,
                Message = $"格口:[{e.ExitName}],锁定",
                Type = LogType.Information
            });
            LockExitEvent?.Invoke(this, e);
        }

        protected virtual async void OnUnLockExitEvent(PackageExitDefinitionInfoModel e) {
            await Task.Yield();
            EventAggregator.Instance.Publish(new SortingLogInfoModel {
                CreateTime = DateTime.Now,
                Message = $"格口:[{e.ExitName}],解除锁定",
                Type = LogType.Information
            });
            UnLockExitEvent?.Invoke(this, e);
        }
    }
}