using System;
using S7.Net;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JayTom.Dws.Client.EventMediators;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Models.PackageSorting.PackageExitLockModels;

namespace JayTom.Dws.Client.Service.Sorting {

    public class DefaultExitMonitor : IExitMonitor {
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IPackageExitLockBindingRepository _packageExitLockBindingRepository;

        public event EventHandler<PackageExitDefinitionInfoModel>? LockExitEvent;

        public event EventHandler<PackageExitDefinitionInfoModel>? UnLockExitEvent;

        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        public bool IsConnected { get; private set; } = false;
        private static Task? _monitorThread;
        private static CancellationTokenSource? _cancellationTokenSource;
        private static List<PackageExitDefinitionInfoModel> _definitionInfoModels = new();
        private Plc? plc;
        private PackageExitLockSettingsDto? _packageExitLockSettingsDto;
        private List<PackageExitLockBindingInfoModel>? _lockBindingInfoModels;

        public DefaultExitMonitor(IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IConfigRepository configRepository, IPackageExitLockBindingRepository packageExitLockBindingRepository) {
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _configRepository = configRepository;
            _packageExitLockBindingRepository = packageExitLockBindingRepository;
        }

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken token = default) {
            try {
                var configInfoModel = await _configRepository.FirstOrDefault(f =>
                    f.ConfigName.Equals("PackageExitLockSettings"), token);
                if (configInfoModel is not null) {
                    try {
                        _packageExitLockSettingsDto = JsonConvert.DeserializeObject<PackageExitLockSettingsDto>(configInfoModel.Value?.ToString() ?? string.Empty);
                        if (_packageExitLockSettingsDto is null) {
                            return new KeyValuePair<bool, string>(false, "锁格配置不存在");
                        }
                        //取出队列
                        _lockBindingInfoModels = await _packageExitLockBindingRepository.Select(s => s.Id > 0,
                            o => o.Id, token);

                        _definitionInfoModels = await _packageExitDefinitionRepository.
                            Select(s => s.Id > 0,
                                o => o.Id, token);
                    }
                    catch (Exception e) {
                        return new KeyValuePair<bool, string>(false, $"锁格配置读取错误:{e.Message}");
                    }
                }
                else {
                    return new KeyValuePair<bool, string>(false, "锁格配置不存在或读取错误");
                }

                if (_packageExitLockSettingsDto.IsUsePackageExitLock) {
                    //判断连接协议(暂时只写S7)
                    //连接、开启线程(多协议之后这部需要放在各自的协议里面做)
                    if (_packageExitLockSettingsDto.ProtocolType == LockProtocolType.S7) {
                        //S7监测

                        plc ??= new Plc(CpuType.S7300, _packageExitLockSettingsDto.S7Config.Ip, (short)_packageExitLockSettingsDto.S7Config.Rack, (short)_packageExitLockSettingsDto.S7Config.Slot);
                        await plc.OpenAsync(token);

                        plc.ReadTimeout = _packageExitLockSettingsDto.S7Config.Timeout;
                        if (_monitorThread is null) {
                            _cancellationTokenSource = new CancellationTokenSource();
                            _monitorThread = Task.Run(async () => {
                                while (!_cancellationTokenSource.IsCancellationRequested) {
                                    foreach (var model in _lockBindingInfoModels) {
                                        await Task.Delay(50, token);

                                        try {
                                            if (plc is not null) {
                                                int.TryParse(model.Address, out var address);

                                                var readBytesAsync =
                                                    await plc.ReadBytesAsync(DataType.DataBlock, _packageExitLockSettingsDto.S7Config.Db, address, model.Length, token);
                                                var infoModel = _definitionInfoModels.FirstOrDefault(f => f.Id.Equals(model.Id));

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
                                    }

                                    await Task.Delay(20, token);
                                }
                            }, token);
                        }
                        return new KeyValuePair<bool, string>(true, "连接成功");
                    }
                    return new KeyValuePair<bool, string>(false, "未识别到连接协议");
                }
                else {
                    return new KeyValuePair<bool, string>(true, "无需判断格口锁定");
                }
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