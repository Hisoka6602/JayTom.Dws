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
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using InstructionType = JayTom.Dws.Data.Package.InstructionType;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.DownstreamProtocols.CommunicationProtocols;
using SortingExitType = JayTom.Dws.Client.EventMediators.SortingExitType;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;
using PackageExitUpdateEvent = JayTom.Dws.Client.EventMediators.PackageExitUpdateEvent;
using PackageAbnormalSortingType = JayTom.Dws.Client.EventMediators.PackageAbnormalSortingType;

namespace JayTom.Dws.Client.Service.BackgroundService {

    /// <summary>
    /// 格口更新处理器
    /// </summary>
    public class PackageExitUpdateBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly ISortingInstructionBindingRepository _sortingInstructionBindingRepository;
        private readonly ISortingInstructionRepository _sortingInstructionRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly ICommunicationConnectionConfigRepository _communicationConnectionConfigRepository;
        private InstructionMapSnapshot _instructionMap = InstructionMapSnapshot.Empty;
        private PackageExitDefinitionInfoModel[] _packageExitDefinitions =
            Array.Empty<PackageExitDefinitionInfoModel>();
        private CommunicationConnectionConfigInfoModel[] _connectionConfigurations =
            Array.Empty<CommunicationConnectionConfigInfoModel>();
        private readonly SemaphoreSlim _configurationUpdateGate = new(1, 1);

        public PackageExitUpdateBackgroundService(ISortingInstructionBindingRepository sortingInstructionBindingRepository,
            ISortingInstructionRepository sortingInstructionRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            ICommunicationConnectionConfigRepository communicationConnectionConfigRepository) {
            _sortingInstructionBindingRepository = sortingInstructionBindingRepository;
            _sortingInstructionRepository = sortingInstructionRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _communicationConnectionConfigRepository = communicationConnectionConfigRepository;

            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is { } model) {
                    await _configurationUpdateGate.WaitAsync();
                    try {
                        switch (model.SettingsName) {
                            case "SortingInstructionBindingItemSettings": {
                                var instructions = await _sortingInstructionRepository.Select(
                                    entity => entity.Id > 0,
                                    entity => entity.Id);
                                var bindings = await _sortingInstructionBindingRepository.Select(
                                    entity => entity.Id > 0,
                                    entity => entity.Id);
                                Volatile.Write(
                                    ref _instructionMap,
                                    new InstructionMapSnapshot(
                                        instructions.ToArray(),
                                        bindings.ToArray()));
                                break;
                            }
                            case "PackageExitDefinitionItemSettings":
                                Volatile.Write(
                                    ref _packageExitDefinitions,
                                    (await _packageExitDefinitionRepository.Select(
                                        entity => entity.Id > 0,
                                        entity => entity.Id > 0)).ToArray());
                                break;
                            case "CommunicationsItemsSettings":
                                Volatile.Write(
                                    ref _connectionConfigurations,
                                    (await _communicationConnectionConfigRepository
                                        .CommunicationConnectionConfigItems(
                                            entity => entity.Id > 0)).ToArray());
                                break;
                        }
                    }
                    finally {
                        _configurationUpdateGate.Release();
                    }
                }
            });

            //分拣指令
            EventAggregator.Instance.Subscribe<InstructionReceived>(item => {
                if (item is { } model && model.InstructionInfos?.Any() == true
                      ) {
                    var instructionMap = Volatile.Read(ref _instructionMap);
                    var packageExitDefinitions = Volatile.Read(ref _packageExitDefinitions);
                    var connectionConfigurations = Volatile.Read(ref _connectionConfigurations);
                    var instructionInfoModel = model.InstructionInfos.FirstOrDefault() ?? new InstructionInfoModel();
                    switch (instructionInfoModel.InstructionType) {
                        //异常
                        case InstructionType.PackageException: {
                                //返回异常
                                //判断协议选择(如果无协议则直接使用字符串)
                                var connectionConfigInfoModel = connectionConfigurations.FirstOrDefault(f =>
                                    f.ConnectionName.Equals(model.ConnectionName));
                                var protocol = CreateProtocol(connectionConfigInfoModel?.CommunicationProtocol);
                                var type = protocol?.SortingExceptionReturnTypeConvert(instructionInfoModel.InstructionContent) ?? SortingExceptionReturnType.None;

                                //匹配实际定义格口
                                var exitContentConvert = protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;
                                if (type == SortingExceptionReturnType.VehicleNumberMismatch) {
                                    exitContentConvert = "00 00";
                                }
                                var instructionBindingId = instructionMap.Instructions.FirstOrDefault(f =>
                                    f.Instruction.Equals(exitContentConvert))?.InstructionBindingId ?? 0;
                                //异常口

                                var abnormalExit = packageExitDefinitions.FirstOrDefault(f =>
                                    f is { IsActive: true, Type: ExitType.AbnormalExit });

                                var exitId = instructionMap.Bindings
                                    .FirstOrDefault(f =>
                                        f.Id.Equals(instructionBindingId))
                                    ?.ExitId;
                                var packageExitDefinitionInfoModel = packageExitDefinitions.FirstOrDefault(f =>
                                    f.Id.Equals(exitId ?? 0));
                                EventAggregator.Instance.Publish(new PackageExitUpdateEvent {
                                    ExitType = SortingExitType.PhysicalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageAbnormalSortingType = type.ConvertTo<PackageAbnormalSortingType>(),
                                    ExitId = exitId ?? abnormalExit?.Id ?? 0,
                                    ExitName = packageExitDefinitionInfoModel?.ExitName ?? abnormalExit?.ExitName ?? string.Empty,
                                    InstructionType = instructionInfoModel?.InstructionType ?? InstructionType.None
                                });

                                break;
                            }
                        //异常
                        case InstructionType.PackageExceptionEx: {
                                //返回异常
                                //判断协议选择(如果无协议则直接使用字符串)
                                var connectionConfigInfoModel = connectionConfigurations.FirstOrDefault(f =>
                                    f.ConnectionName.Equals(model.ConnectionName));
                                var protocol = CreateProtocol(connectionConfigInfoModel?.CommunicationProtocol);
                                //异常口

                                var abnormalExit = packageExitDefinitions.FirstOrDefault(f =>
                                    f is { IsActive: true, Type: ExitType.AbnormalExit });
                                //匹配实际定义格口
                                var exitContentConvert = protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;

                                var instructionBindingId = instructionMap.Instructions.FirstOrDefault(f =>
                                    f.Instruction.Equals(exitContentConvert))?.InstructionBindingId ?? 0;
                                var exitId = instructionMap.Bindings
                                    .FirstOrDefault(f =>
                                        f.Id.Equals(instructionBindingId))
                                    ?.ExitId;
                                var packageExitDefinitionInfoModel = packageExitDefinitions.FirstOrDefault(f =>
                                    f.Id.Equals(exitId ?? 0));
                                EventAggregator.Instance.Publish(new PackageExitUpdateEvent {
                                    ExitType = SortingExitType.PhysicalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageAbnormalSortingType = PackageAbnormalSortingType.LockExit,
                                    ExitId = exitId ?? abnormalExit?.Id ?? 0,
                                    ExitName = packageExitDefinitionInfoModel?.ExitName ?? abnormalExit?.ExitName ?? string.Empty,
                                    InstructionType = instructionInfoModel?.InstructionType ?? InstructionType.None
                                });
                                break;
                            }
                        //发送
                        case InstructionType.SendSorting: {
                                //找到包裹->更新到理论格口
                                var connectionConfigInfoModel = connectionConfigurations.FirstOrDefault(f =>
                                    f.ConnectionName.Equals(model.ConnectionName));
                                var protocol = CreateProtocol(connectionConfigInfoModel?.CommunicationProtocol);
                                var exitContentConvert = protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;

                                var instructionBindingId = instructionMap.Instructions.FirstOrDefault(f =>
                                    f.Instruction.Equals(exitContentConvert))?.InstructionBindingId ?? 0;
                                var exitId = instructionMap.Bindings
                                    .FirstOrDefault(f =>
                                        f.Id.Equals(instructionBindingId))
                                    ?.ExitId;
                                var packageExitDefinitionInfoModel = packageExitDefinitions.FirstOrDefault(f =>
                                    f.Id.Equals(exitId ?? 0));
                                EventAggregator.Instance.Publish(new PackageExitUpdateEvent {
                                    ExitType = SortingExitType.TheoreticalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageAbnormalSortingType = PackageAbnormalSortingType.None,
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
                                var connectionConfigInfoModel = connectionConfigurations.FirstOrDefault(f =>
                                    f.ConnectionName.Equals(model.ConnectionName));
                                var protocol = CreateProtocol(connectionConfigInfoModel?.CommunicationProtocol);

                                var exitContentConvert = protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;

                                var instructionBindingId = instructionMap.Instructions.FirstOrDefault(f =>
                                    f.Instruction.Equals(exitContentConvert))?.InstructionBindingId ?? 0;
                                var exitId = instructionMap.Bindings
                                    .FirstOrDefault(f =>
                                        f.Id.Equals(instructionBindingId))
                                    ?.ExitId;
                                var packageExitDefinitionInfoModel = packageExitDefinitions.FirstOrDefault(f =>
                                    f.Id.Equals(exitId ?? 0));
                                //更新理论格口
                                EventAggregator.Instance.Publish(new PackageExitUpdateEvent {
                                    ExitType = SortingExitType.PhysicalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageAbnormalSortingType = PackageAbnormalSortingType.None,
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
            var instructions = await _sortingInstructionRepository.Select(s => s.Id > 0,
                o => o.Id);
            //读绑定
            var bindings = await _sortingInstructionBindingRepository.Select(s => s.Id > 0,
                o => o.Id);
            Volatile.Write(
                ref _instructionMap,
                new InstructionMapSnapshot(instructions.ToArray(), bindings.ToArray()));

            //读格口
            Volatile.Write(
                ref _packageExitDefinitions,
                (await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                    o => o.Id > 0)).ToArray());
            //通讯协议
            Volatile.Write(
                ref _connectionConfigurations,
                (await _communicationConnectionConfigRepository
                    .CommunicationConnectionConfigItems(s => s.Id > 0)).ToArray());
        }

        private static IDeviceCommunicationProtocol? CreateProtocol(string? protocolName) {
            if (!Enum.TryParse<CommunicationProtocol>(protocolName, true, out var communicationProtocol)) {
                return null;
            }

            return communicationProtocol switch {
                CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                CommunicationProtocol.CaiNiao => new CaiNiaoCommunicationProtocol(),
                _ => null
            };
        }

        private sealed record InstructionMapSnapshot(
            SortingInstructionInfoModel[] Instructions,
            SortingInstructionBindingInfoModel[] Bindings) {
            public static readonly InstructionMapSnapshot Empty = new(
                Array.Empty<SortingInstructionInfoModel>(),
                Array.Empty<SortingInstructionBindingInfoModel>());
        }
    }
}
