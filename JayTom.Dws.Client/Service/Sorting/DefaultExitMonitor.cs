using System;
using S7.Net;
using ImTools;
using System.IO;
using System.Linq;
using System.Text;
using S7.Net.Types;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using Byte = System.Byte;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using DateTime = System.DateTime;
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

        public event EventHandler<List<PackageExitDefinitionInfoModel>>? Initialized;

        public bool IsConnected { get; private set; } = false;

        public event EventHandler<EventArgs>? Connected;

        public event EventHandler<EventArgs>? Disconnected;

        private static Task? _monitorThread;
        private static CancellationTokenSource? _cancellationTokenSource;
        private static List<PackageExitDefinitionInfoModel> _definitionInfoModels = new();
        private Plc? plc;
        private PackageExitLockSettingsDto? _packageExitLockSettingsDto;
        private List<PackageExitLockBindingInfoModel>? _lockBindingInfoModels;
        private bool _locking = false;

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
                    return new KeyValuePair<bool, string>(true, "锁格配置不存在或读取错误");
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
                                var isInitialized = false;
                                while (!_cancellationTokenSource.IsCancellationRequested) {
                                    var initialized = isInitialized;
                                    if (plc is null) return;

                                    try {
                                        var count = _lockBindingInfoModels.Count / 10 + (_lockBindingInfoModels.Count % 10 > 0 ? 1 : 0);

                                        for (var i = 0; i < count; i++) {
                                            var dataItems = _lockBindingInfoModels.Select(s => new DataItem {
                                                DataType = DataType.DataBlock,
                                                StartByteAdr = Convert.ToInt32(s.Address),
                                                DB = _packageExitLockSettingsDto.S7Config.Db,
                                                Count = s.Length,
                                                VarType = VarType.Byte
                                            }).Skip(i * 10).Take(10).ToList();

                                            var readMultipleVarsAsync = await plc.ReadMultipleVarsAsync(dataItems, token);

                                            foreach (var dataItem in readMultipleVarsAsync) {
                                                var model = _lockBindingInfoModels.FirstOrDefault(f =>
                                                    f.Address.Equals(dataItem.StartByteAdr.ToString()));
                                                if (model != null) {
                                                    var infoModel =
                                                        _definitionInfoModels.FirstOrDefault(f =>
                                                            f.Id.Equals(model.ExitId));

                                                    if (Convert.ToByte(dataItem.Value) == 0) {
                                                        //解锁
                                                        if (!initialized && infoModel is not null) {
                                                            infoModel.IsLockExit = false;
                                                        }

                                                        if (infoModel?.IsLockExit == true) {
                                                            OnUnLockExitEvent(infoModel);
                                                            infoModel.IsLockExit = false;
                                                        }
                                                    }
                                                    else {
                                                        if (!initialized && infoModel is not null) {
                                                            infoModel.IsLockExit = true;
                                                        }

                                                        // 锁
                                                        if (infoModel?.IsLockExit == false) {
                                                            OnLockExitEvent(infoModel);
                                                            infoModel.IsLockExit = true;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception e) {
                                        OnExceptionOccurred(new ExceptionEventArgs() {
                                            ExceptionMessage = e.Message
                                        });
                                    }

                                    await Task.Delay(150, token);
                                    if (!isInitialized) {
                                        isInitialized = true;
                                        OnInitialized(_definitionInfoModels);
                                    }
                                }
                            }, token);
                        }
                        OnConnected();
                        return new KeyValuePair<bool, string>(true, "连接成功");
                    }
                    OnDisconnected();
                    return new KeyValuePair<bool, string>(false, "未识别到连接协议");
                }
                else {
                    OnDisconnected();
                    return new KeyValuePair<bool, string>(true, "无需判断格口锁定");
                }
            }
            catch (Exception e) {
                /*OnExceptionOccurred(new ExceptionEventArgs() {
                    ExceptionMessage = e.Message
                });*/
                OnDisconnected();
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
                OnDisconnected();
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

        public Task<KeyValuePair<bool, List<PackageExitDefinitionInfoModel>>> GetAllPackageExitStatus() {
            return Task.FromResult(new KeyValuePair<bool, List<PackageExitDefinitionInfoModel>>(true, _definitionInfoModels));
        }

        public async Task<KeyValuePair<bool, string>> AllLockExit() {
            if (plc is not null && _lockBindingInfoModels is not null &&
                _packageExitLockSettingsDto is not null) {
                if (_locking) {
                    return new KeyValuePair<bool, string>(false, $"锁格操作中");
                }

                try {
                    /*foreach (var model in _lockBindingInfoModels) {
                        await plc.WriteBytesAsync(DataType.DataBlock,
                             _packageExitLockSettingsDto.S7Config.Db,
                             Convert.ToInt32(model.Address),
                             new ReadOnlyMemory<byte>(new byte[] { 0x1 }));
                        await Task.Delay(200);
                    }*/
                    var allOne = false;
                    do {
                        var data = Enumerable.Repeat((byte)0x1, 100).ToArray();
                        await plc.WriteBytesAsync(DataType.DataBlock,
                            _packageExitLockSettingsDto.S7Config.Db,
                            Convert.ToInt32(0),
                            new ReadOnlyMemory<byte>(data));
                        await Task.Delay(2000);
                        //挑出未成功的单独写

                        var readBytesAsync = await plc.ReadBytesAsync(DataType.DataBlock,
                            _packageExitLockSettingsDto.S7Config.Db,
                            Convert.ToInt32(0), 100);
                        allOne = readBytesAsync.All(b => b == 0x1);
                    } while (!allOne);

                    return new KeyValuePair<bool, string>(true, "锁格成功");
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"锁格失败:{e}");
                    return new KeyValuePair<bool, string>(false, $"锁格失败:{e.Message}");
                }
                finally {
                    _locking = false;
                }
            }
            else {
                return new KeyValuePair<bool, string>(false, "锁格未连接");
            }
        }

        public async Task<KeyValuePair<bool, string>> AllUnLockExit() {
            if (plc is not null && _lockBindingInfoModels is not null &&
                _packageExitLockSettingsDto is not null) {
                if (_locking) {
                    return new KeyValuePair<bool, string>(false, $"解锁操作中");
                }

                try {
                    /*foreach (var model in _lockBindingInfoModels) {
                        await plc.WriteBytesAsync(DataType.DataBlock,
                            _packageExitLockSettingsDto.S7Config.Db,
                            Convert.ToInt32(model.Address),
                            new ReadOnlyMemory<byte>(new byte[100]));
                        await Task.Delay(200);
                    }*/
                    var allZero = false;
                    do {
                        await plc.WriteBytesAsync(DataType.DataBlock,
                            _packageExitLockSettingsDto.S7Config.Db,
                            Convert.ToInt32(0),
                            new ReadOnlyMemory<byte>(new byte[100]));
                        await Task.Delay(2000);
                        var readBytesAsync = await plc.ReadBytesAsync(DataType.DataBlock,
                            _packageExitLockSettingsDto.S7Config.Db,
                            Convert.ToInt32(0), 100);
                        allZero = readBytesAsync.All(b => b == 0x0);
                    } while (!allZero);

                    return new KeyValuePair<bool, string>(true, "解锁成功");
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"解锁失败:{e}");
                    return new KeyValuePair<bool, string>(false, $"解锁失败:{e.Message}");
                }
                finally {
                    _locking = false;
                }
            }
            else {
                return new KeyValuePair<bool, string>(false, "锁格未连接");
            }
        }

        public async Task<KeyValuePair<bool, string>> AllLockExit(int db, int address = 0, int length = 1) {
            await Task.Yield();

            //锁格->256.0

            if (plc is not null) {
                if (_locking) {
                    return new KeyValuePair<bool, string>(false, $"锁格操作中");
                }
                try {
                    _locking = true;
                    db = _packageExitLockSettingsDto?.S7Config.Db ?? 0;

                    await plc.WriteBitAsync(DataType.DataBlock, db,
                        256, 0, true);
                    await Task.Delay(2000);
                    await plc.WriteBitAsync(DataType.DataBlock, db,
                        256, 0, false);
                    return new KeyValuePair<bool, string>(true, $"锁格成功");
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"锁格失败:{e}");
                    return new KeyValuePair<bool, string>(false, $"锁格失败:{e.Message}");
                }
                finally {
                    _locking = false;
                }
            }
            else {
                return new KeyValuePair<bool, string>(false, "锁格未连接");
            }
        }

        public async Task<KeyValuePair<bool, string>> AllUnLockExit(int db, int address = 1, int length = 1) {
            //解锁->256.1
            if (plc is not null) {
                if (_locking) {
                    return new KeyValuePair<bool, string>(false, $"解锁操作中");
                }
                try {
                    _locking = true;
                    db = _packageExitLockSettingsDto?.S7Config.Db ?? 0;
                    await plc.WriteBitAsync(DataType.DataBlock, db,
                        256, 1, true);
                    await Task.Delay(2000);
                    await plc.WriteBitAsync(DataType.DataBlock, db,
                        256, 1, false);
                    return new KeyValuePair<bool, string>(true, $"解锁成功");
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"解锁失败:{e}");
                    return new KeyValuePair<bool, string>(false, $"解锁失败:{e.Message}");
                }
                finally {
                    _locking = false;
                }
            }
            else {
                return new KeyValuePair<bool, string>(false, "锁格未连接");
            }
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
            await Task.Delay(1);
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

        protected virtual async void OnInitialized(List<PackageExitDefinitionInfoModel> e) {
            await Task.Yield();
            Initialized?.Invoke(this, e);
        }

        protected virtual async void OnConnected() {
            await Task.Yield();
            Connected?.Invoke(this, EventArgs.Empty);
        }

        protected virtual async void OnDisconnected() {
            await Task.Yield();
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}