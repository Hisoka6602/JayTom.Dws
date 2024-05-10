using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.IO.Packaging;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using InstructionType = JayTom.Dws.Data.Package.InstructionType;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// 格口更新处理器
    /// </summary>
    public class PackageExitUpdateBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly ISortingInstructionBindingRepository _sortingInstructionBindingRepository;
        private readonly ISortingInstructionRepository _sortingInstructionRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly ICommunicationConnectionConfigRepository _communicationConnectionConfigRepository;
        private List<SortingInstructionInfoModel> _sortingInstructionInfoModels = new();
        private List<SortingInstructionBindingInfoModel> _sortingInstructionBindingInfoModels = new();
        private List<PackageExitDefinitionInfoModel> _packageExitDefinitionInfoModels = new();
        private List<CommunicationConnectionConfigInfoModel> _communicationConnectionConfigInfoModels = new();

        private IDeviceCommunicationProtocol? _protocol;

        public PackageExitUpdateBackgroundService(ISortingInstructionBindingRepository sortingInstructionBindingRepository,
            ISortingInstructionRepository sortingInstructionRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            ICommunicationConnectionConfigRepository communicationConnectionConfigRepository) {
            _sortingInstructionBindingRepository = sortingInstructionBindingRepository;
            _sortingInstructionRepository = sortingInstructionRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _communicationConnectionConfigRepository = communicationConnectionConfigRepository;

            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is SettingsChangedEvent model) {
                    switch (model.SettingsName) {
                        case "SortingInstructionBindingItemSettings":
                            //更新绑定信息
                            //读指令
                            _sortingInstructionInfoModels = await _sortingInstructionRepository.Select(s => s.Id > 0,
                                o => o.Id);
                            //读绑定
                            _sortingInstructionBindingInfoModels = await _sortingInstructionBindingRepository.Select(s => s.Id > 0,
                                o => o.Id);
                            break;

                        case "PackageExitDefinitionItemSettings":
                            //更新格口信息
                            //读格口
                            _packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                                o => o.Id > 0);
                            break;

                        case "CommunicationsItemsSettings":
                            //更新协议信息
                            //读格口
                            _communicationConnectionConfigInfoModels = await _communicationConnectionConfigRepository.CommunicationConnectionConfigItems(s => s.Id > 0);
                            break;
                    }
                }
            });

            //分拣指令
            EventAggregator.Instance.Subscribe<InstructionReceived>(async item => {
                await Task.Yield();
                if (item is InstructionReceived model && model.InstructionInfos?.Any() == true
                      ) {
                    var instructionInfoModel = model.InstructionInfos.FirstOrDefault() ?? new InstructionInfoModel();
                    switch (instructionInfoModel.InstructionType) {
                        //异常
                        case InstructionType.PackageException: {
                                await Task.Delay(500);
                                //返回异常
                                //判断协议选择(如果无协议则直接使用字符串)
                                var connectionConfigInfoModel = _communicationConnectionConfigInfoModels.FirstOrDefault(f =>
                                    f.ConnectionName.Equals(model.ConnectionName));
                                var communicationProtocol = (CommunicationProtocol)Enum.Parse(typeof(CommunicationProtocol),
                                    connectionConfigInfoModel?.CommunicationProtocol ?? "None");
                                _protocol ??= communicationProtocol switch {
                                    CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                                    CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                                    CommunicationProtocol.CaiNiao => new CaiNiaoCommunicationProtocol(),
                                    _ => null
                                };
                                var type = _protocol?.SortingExceptionReturnTypeConvert(instructionInfoModel.InstructionContent) ?? SortingExceptionReturnType.None;

                                //匹配实际定义格口
                                var exitContentConvert = _protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;
                                if (type == SortingExceptionReturnType.VehicleNumberMismatch) {
                                    exitContentConvert = "00 00";
                                }
                                var instructionBindingId = _sortingInstructionInfoModels.FirstOrDefault(f =>
                                    f.Instruction.Equals(exitContentConvert))?.InstructionBindingId ?? 0;
                                //异常口

                                var abnormalExit = _packageExitDefinitionInfoModels.FirstOrDefault(f =>
                                    f is { IsActive: true, Type: ExitType.AbnormalExit });

                                var exitId = _sortingInstructionBindingInfoModels
                                    .FirstOrDefault(f =>
                                        f.Id.Equals(instructionBindingId))
                                    ?.ExitId;
                                var packageExitDefinitionInfoModel = _packageExitDefinitionInfoModels.FirstOrDefault(f =>
                                    f.Id.Equals(exitId ?? 0));
                                EventAggregator.Instance.Publish(new PackageExitUpdateEvent {
                                    ExitType = SortingExitType.PhysicalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageCloudAbnormalSortingType = type.ConvertTo<PackageCloudAbnormalSortingType>(),
                                    ExitId = exitId ?? abnormalExit?.Id ?? 0,
                                    ExitName = packageExitDefinitionInfoModel?.ExitName ?? abnormalExit?.ExitName ?? string.Empty,
                                    InstructionType = instructionInfoModel?.InstructionType ?? InstructionType.None
                                });
                                NLog.LogManager.GetCurrentClassLogger().Error($"{model.Timestamp}");
                                NLog.LogManager.GetCurrentClassLogger().Error($"PackageCloudAbnormalSortingType:{type.ConvertTo<PackageCloudAbnormalSortingType>()}");
                                break;
                            }
                        //异常
                        case InstructionType.PackageExceptionEx: {
                                //返回异常
                                //判断协议选择(如果无协议则直接使用字符串)
                                var connectionConfigInfoModel = _communicationConnectionConfigInfoModels.FirstOrDefault(f =>
                                    f.ConnectionName.Equals(model.ConnectionName));
                                var communicationProtocol = (CommunicationProtocol)Enum.Parse(typeof(CommunicationProtocol),
                                    connectionConfigInfoModel?.CommunicationProtocol ?? "None");
                                _protocol ??= communicationProtocol switch {
                                    CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                                    CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                                    CommunicationProtocol.CaiNiao => new CaiNiaoCommunicationProtocol(),
                                    _ => null
                                };
                                //异常口

                                var abnormalExit = _packageExitDefinitionInfoModels.FirstOrDefault(f =>
                                    f is { IsActive: true, Type: ExitType.AbnormalExit });
                                //匹配实际定义格口
                                var exitContentConvert = _protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;

                                var instructionBindingId = _sortingInstructionInfoModels.FirstOrDefault(f =>
                                    f.Instruction.Equals(exitContentConvert))?.InstructionBindingId ?? 0;
                                var exitId = _sortingInstructionBindingInfoModels
                                    .FirstOrDefault(f =>
                                        f.Id.Equals(instructionBindingId))
                                    ?.ExitId;
                                var packageExitDefinitionInfoModel = _packageExitDefinitionInfoModels.FirstOrDefault(f =>
                                    f.Id.Equals(exitId ?? 0));
                                EventAggregator.Instance.Publish(new PackageExitUpdateEvent {
                                    ExitType = SortingExitType.PhysicalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageCloudAbnormalSortingType = PackageCloudAbnormalSortingType.LockExit,
                                    ExitId = exitId ?? abnormalExit?.Id ?? 0,
                                    ExitName = packageExitDefinitionInfoModel?.ExitName ?? abnormalExit?.ExitName ?? string.Empty,
                                    InstructionType = instructionInfoModel?.InstructionType ?? InstructionType.None
                                });
                                break;
                            }
                        //发送
                        case InstructionType.SendSorting: {
                                //找到包裹->更新到理论格口
                                var connectionConfigInfoModel = _communicationConnectionConfigInfoModels.FirstOrDefault(f =>
                                    f.ConnectionName.Equals(model.ConnectionName));
                                var communicationProtocol = (CommunicationProtocol)Enum.Parse(typeof(CommunicationProtocol),
                                    connectionConfigInfoModel?.CommunicationProtocol ?? "None");
                                _protocol ??= communicationProtocol switch {
                                    CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                                    CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                                    CommunicationProtocol.CaiNiao => new CaiNiaoCommunicationProtocol(),
                                    _ => null
                                };
                                var exitContentConvert = _protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;

                                var instructionBindingId = _sortingInstructionInfoModels.FirstOrDefault(f =>
                                    f.Instruction.Equals(exitContentConvert))?.InstructionBindingId ?? 0;
                                var exitId = _sortingInstructionBindingInfoModels
                                    .FirstOrDefault(f =>
                                        f.Id.Equals(instructionBindingId))
                                    ?.ExitId;
                                var packageExitDefinitionInfoModel = _packageExitDefinitionInfoModels.FirstOrDefault(f =>
                                    f.Id.Equals(exitId ?? 0));
                                EventAggregator.Instance.Publish(new PackageExitUpdateEvent {
                                    ExitType = SortingExitType.TheoreticalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageCloudAbnormalSortingType = PackageCloudAbnormalSortingType.None,
                                    ExitId = exitId ?? 0,
                                    ExitName = packageExitDefinitionInfoModel?.ExitName ?? string.Empty,
                                    InstructionType = instructionInfoModel?.InstructionType ?? InstructionType.None
                                });
                                break;
                            }
                        //落格
                        case InstructionType.SignalCallback: {
                                //找到包裹->更新到物理格口
                                //判断协议选择(如果无协议则直接使用字符串)
                                var connectionConfigInfoModel = _communicationConnectionConfigInfoModels.FirstOrDefault(f =>
                                    f.ConnectionName.Equals(model.ConnectionName));
                                var communicationProtocol = (CommunicationProtocol)Enum.Parse(typeof(CommunicationProtocol),
                                    connectionConfigInfoModel?.CommunicationProtocol ?? "None");
                                _protocol ??= communicationProtocol switch {
                                    CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                                    CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                                    CommunicationProtocol.CaiNiao => new CaiNiaoCommunicationProtocol(),
                                    _ => null
                                };

                                var exitContentConvert = _protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;

                                var instructionBindingId = _sortingInstructionInfoModels.FirstOrDefault(f =>
                                    f.Instruction.Equals(exitContentConvert))?.InstructionBindingId ?? 0;
                                var exitId = _sortingInstructionBindingInfoModels
                                    .FirstOrDefault(f =>
                                        f.Id.Equals(instructionBindingId))
                                    ?.ExitId;
                                var packageExitDefinitionInfoModel = _packageExitDefinitionInfoModels.FirstOrDefault(f =>
                                    f.Id.Equals(exitId ?? 0));
                                NLog.LogManager.GetCurrentClassLogger().Error($"判断到落格信号");
                                //更新理论格口
                                EventAggregator.Instance.Publish(new PackageExitUpdateEvent {
                                    ExitType = SortingExitType.PhysicalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageCloudAbnormalSortingType = PackageCloudAbnormalSortingType.None,
                                    ExitId = exitId ?? 0,
                                    ExitName = packageExitDefinitionInfoModel?.ExitName ?? string.Empty,
                                    InstructionType = instructionInfoModel?.InstructionType ?? InstructionType.None
                                });

                                break;
                            }
                    }
                    //异常2
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await ReadDefaultConfig();
        }

        private async Task ReadDefaultConfig() {
            //读指令
            _sortingInstructionInfoModels = await _sortingInstructionRepository.Select(s => s.Id > 0,
                o => o.Id);
            //读绑定
            _sortingInstructionBindingInfoModels = await _sortingInstructionBindingRepository.Select(s => s.Id > 0,
                o => o.Id);

            //读格口
            _packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                o => o.Id > 0);
            //通讯协议
            _communicationConnectionConfigInfoModels = await _communicationConnectionConfigRepository.CommunicationConnectionConfigItems(s => s.Id > 0);
        }
    }
}