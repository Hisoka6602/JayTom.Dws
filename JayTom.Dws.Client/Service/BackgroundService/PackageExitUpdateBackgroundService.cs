using System;
using JayTom.Dws.Application.SortingInstructions;
using JayTom.Dws.Application.PackageExits;
using JayTom.Dws.Application.Communications;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.IO.Packaging;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using System.Collections.Generic;
using JayTom.Dws.Integrations.Cloud;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Legacy.Contracts.DownstreamProtocols;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using InstructionType = JayTom.Dws.Models.Package.InstructionType;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.DownstreamProtocols.CommunicationProtocols;
using SortingExitType = JayTom.Dws.Application.Events.SortingExitType;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.ConnectionParams;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;
using PackageExitUpdateEvent = JayTom.Dws.Application.Events.PackageExitUpdateEvent;
using PackageAbnormalSortingType = JayTom.Dws.Application.Events.PackageAbnormalSortingType;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    /// <summary>
    /// 格口更新处理器
    /// </summary>
    public class PackageExitUpdateBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private readonly ISortingInstructionBindingCatalog _sortingInstructionBindingRepository;
        private readonly ISortingInstructionBindingCatalog _sortingInstructionRepository;
        private readonly IPackageExitManagement _packageExitDefinitionRepository;
        private readonly ICommunicationConfigurationCatalog _communicationConnectionConfigRepository;
        private InstructionMapSnapshot _instructionMap = InstructionMapSnapshot.Empty;
        private PackageExitDefinitionInfoModel[] _packageExitDefinitions =
            [];
        private CommunicationConnectionConfigInfoModel[] _connectionConfigurations =
            [];
        /// <summary>原子发布同一配置版本的回调路由索引。</summary>
        private PackageExitRoutingSnapshot _routingSnapshot = PackageExitRoutingSnapshot.Empty;
        private readonly SemaphoreSlim _configurationUpdateGate = new(1, 1);
        /// <summary>发送格口后固定等待 60 秒的落格确认跟踪器。</summary>
        private readonly FallConfirmationTimeoutTracker _fallConfirmationTracker;
        private static readonly TimeSpan FallConfirmationScanInterval =
            TimeSpan.FromMilliseconds(100);

        public PackageExitUpdateBackgroundService(ISortingInstructionBindingCatalog sortingInstructionBindingRepository,
            ISortingInstructionBindingCatalog sortingInstructionRepository,
            IPackageExitManagement packageExitDefinitionRepository,
            ICommunicationConfigurationCatalog communicationConnectionConfigRepository,
            JayTom.Dws.Application.Messaging.IEventBus eventBus,
            TimeProvider? timeProvider = null)
        {
            _eventBus = eventBus;
            _fallConfirmationTracker =
                new FallConfirmationTimeoutTracker(timeProvider);
            _sortingInstructionBindingRepository = sortingInstructionBindingRepository;
            _sortingInstructionRepository = sortingInstructionRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _communicationConnectionConfigRepository = communicationConnectionConfigRepository;

            _eventBus.SubscribeAsync<SettingsChangedEvent>(async item =>
            {
                if (item is { } model)
                {
                    await _configurationUpdateGate.WaitAsync();
                    try
                    {
                        switch (model.SettingsName)
                        {
                            case "SortingInstructionBindingItemSettings":
                                {
                                    var instructions = await _sortingInstructionRepository.ListInstructionsAsync();
                                    var bindings = await _sortingInstructionBindingRepository.ListAsync();
                                    Volatile.Write(
                                        ref _instructionMap,
                                        new InstructionMapSnapshot(
                                            [.. instructions],
                                            [.. bindings]));
                                    break;
                                }
                            case "PackageExitDefinitionItemSettings":
                                Volatile.Write(
                                    ref _packageExitDefinitions,
                                    [.. (await _packageExitDefinitionRepository.ListAsync())]);
                                break;
                            case "CommunicationsItemsSettings":
                                Volatile.Write(
                                    ref _connectionConfigurations,
                                    [.. (await _communicationConnectionConfigRepository
                                        .ListWithDetailsAsync())]);
                                break;
                        }
                        PublishRoutingSnapshot();
                    }
                    finally
                    {
                        _configurationUpdateGate.Release();
                    }
                }
            });

            //分拣指令
            _eventBus.Subscribe<InstructionReceived>(item =>
            {
                if (item is { } model && model.InstructionInfos?.Any() == true
                      )
                {
                    var routing = Volatile.Read(ref _routingSnapshot);
                    var instructionInfoModel = model.InstructionInfos.FirstOrDefault() ?? new InstructionInfoModel();
                    switch (instructionInfoModel.InstructionType)
                    {
                        //异常
                        case InstructionType.PackageException:
                            {
                                if (!_fallConfirmationTracker.TryConfirm(
                                        model.Timestamp))
                                {
                                    LogDuplicateFallConfirmation(model);
                                    break;
                                }
                                //返回异常
                                //判断协议选择(如果无协议则直接使用字符串)
                                routing.ProtocolsByConnection.TryGetValue(model.ConnectionName, out var protocol);
                                var type = protocol?.SortingExceptionReturnTypeConvert(instructionInfoModel.InstructionContent) ?? SortingExceptionReturnType.None;

                                //匹配实际定义格口
                                var exitContentConvert = protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;
                                if (type == SortingExceptionReturnType.VehicleNumberMismatch)
                                {
                                    exitContentConvert = "00 00";
                                }
                                routing.BindingIdsByInstruction.TryGetValue(exitContentConvert, out var instructionBindingId);
                                //异常口

                                var abnormalExit = routing.ActiveAbnormalExit;

                                routing.ExitIdsByBinding.TryGetValue(instructionBindingId, out var exitId);
                                routing.ExitsByIdentifier.TryGetValue(exitId, out var packageExitDefinitionInfoModel);
                                _eventBus.Publish(new PackageExitUpdateEvent
                                {
                                    ExitType = SortingExitType.PhysicalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageAbnormalSortingType = type.ConvertTo<PackageAbnormalSortingType>(),
                                    ExitId = exitId > 0 ? exitId : abnormalExit?.Id ?? 0,
                                    ExitName = packageExitDefinitionInfoModel?.ExitName ?? abnormalExit?.ExitName ?? string.Empty,
                                    InstructionType = instructionInfoModel?.InstructionType ?? InstructionType.None
                                });

                                break;
                            }
                        //异常
                        case InstructionType.PackageExceptionEx:
                            {
                                if (!_fallConfirmationTracker.TryConfirm(
                                        model.Timestamp))
                                {
                                    LogDuplicateFallConfirmation(model);
                                    break;
                                }
                                //返回异常
                                //判断协议选择(如果无协议则直接使用字符串)
                                routing.ProtocolsByConnection.TryGetValue(model.ConnectionName, out var protocol);
                                //异常口

                                var abnormalExit = routing.ActiveAbnormalExit;
                                //匹配实际定义格口
                                var exitContentConvert = protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;

                                routing.BindingIdsByInstruction.TryGetValue(exitContentConvert, out var instructionBindingId);
                                routing.ExitIdsByBinding.TryGetValue(instructionBindingId, out var exitId);
                                routing.ExitsByIdentifier.TryGetValue(exitId, out var packageExitDefinitionInfoModel);
                                _eventBus.Publish(new PackageExitUpdateEvent
                                {
                                    ExitType = SortingExitType.PhysicalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageAbnormalSortingType = PackageAbnormalSortingType.LockExit,
                                    ExitId = exitId > 0 ? exitId : abnormalExit?.Id ?? 0,
                                    ExitName = packageExitDefinitionInfoModel?.ExitName ?? abnormalExit?.ExitName ?? string.Empty,
                                    InstructionType = instructionInfoModel?.InstructionType ?? InstructionType.None
                                });
                                break;
                            }
                        //发送
                        case InstructionType.SendSorting:
                            {
                                //找到包裹->更新到理论格口
                                routing.ProtocolsByConnection.TryGetValue(model.ConnectionName, out var protocol);
                                var exitContentConvert = protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;

                                routing.BindingIdsByInstruction.TryGetValue(exitContentConvert, out var instructionBindingId);
                                routing.ExitIdsByBinding.TryGetValue(instructionBindingId, out var exitId);
                                routing.ExitsByIdentifier.TryGetValue(exitId, out var packageExitDefinitionInfoModel);
                                var theoreticalExit = new PackageExitUpdateEvent
                                {
                                    ExitType = SortingExitType.TheoreticalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageAbnormalSortingType = PackageAbnormalSortingType.None,
                                    ExitId = exitId > 0 ? exitId : model.ExitId,
                                    ExitName = packageExitDefinitionInfoModel?.ExitName ?? model.ExitName,
                                    InstructionType = instructionInfoModel?.InstructionType ?? InstructionType.None
                                };
                                _fallConfirmationTracker.Register(
                                    theoreticalExit);
                                _eventBus.Publish(theoreticalExit);
                                break;
                            }
                        //落格
                        case InstructionType.SignalCallback:
                            {
                                if (!_fallConfirmationTracker.TryConfirm(
                                        model.Timestamp))
                                {
                                    LogDuplicateFallConfirmation(model);
                                    break;
                                }
                                //找到包裹->更新到物理格口
                                //判断协议选择(如果无协议则直接使用字符串)
                                routing.ProtocolsByConnection.TryGetValue(model.ConnectionName, out var protocol);

                                var exitContentConvert = protocol?.ExitContentConvert(instructionInfoModel.InstructionContent) ?? instructionInfoModel.InstructionContent;

                                routing.BindingIdsByInstruction.TryGetValue(exitContentConvert, out var instructionBindingId);
                                routing.ExitIdsByBinding.TryGetValue(instructionBindingId, out var exitId);
                                routing.ExitsByIdentifier.TryGetValue(exitId, out var packageExitDefinitionInfoModel);
                                //更新理论格口
                                _eventBus.Publish(new PackageExitUpdateEvent
                                {
                                    ExitType = SortingExitType.PhysicalExit,
                                    InstructionInfos = new List<InstructionInfoModel>()
                                    {
                                    instructionInfoModel
                                },
                                    Type = packageExitDefinitionInfoModel?.Type ?? ExitType.PackageExit,
                                    Timestamp = model.Timestamp,
                                    PackageAbnormalSortingType = PackageAbnormalSortingType.None,
                                    ExitId = exitId,
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ReadDefaultConfig();
            using var timer = new PeriodicTimer(
                FallConfirmationScanInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken)
                       .ConfigureAwait(false))
            {
                foreach (var fallEvent in
                         _fallConfirmationTracker.TakeExpired(
                             DateTime.Now))
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(
                        $"下位机60秒未回复落格，已生成正常落格事件:Timestamp={fallEvent.Timestamp},ExitName={fallEvent.ExitName},ExitId={fallEvent.ExitId}");
                    _eventBus.Publish(fallEvent);
                }
            }
        }

        /// <summary>记录已被真实回复或超时机制完成的重复落格信号。</summary>
        private static void LogDuplicateFallConfirmation(
            InstructionReceived model)
        {
            NLog.LogManager.GetCurrentClassLogger().Warn(
                $"忽略重复或迟到的落格回复:Timestamp={model.Timestamp},SortingCode={model.SortingCode},ConnectionName={model.ConnectionName}");
        }

        private async Task ReadDefaultConfig()
        {
            //读指令
            var instructions = await _sortingInstructionRepository.ListInstructionsAsync();
            //读绑定
            var bindings = await _sortingInstructionBindingRepository.ListAsync();
            Volatile.Write(
                ref _instructionMap,
                new InstructionMapSnapshot([.. instructions], [.. bindings]));

            //读格口
            Volatile.Write(
                ref _packageExitDefinitions,
                [.. (await _packageExitDefinitionRepository.ListAsync())]);
            //通讯协议
            Volatile.Write(
                ref _connectionConfigurations,
                [.. (await _communicationConnectionConfigRepository
                    .ListWithDetailsAsync())]);
            PublishRoutingSnapshot();
        }

        /// <summary>从当前配置数组构建回调线程只读使用的常数时间路由索引。</summary>
        private void PublishRoutingSnapshot()
        {
            var instructionMap = Volatile.Read(ref _instructionMap);
            var packageExits = Volatile.Read(ref _packageExitDefinitions);
            var connectionConfigurations = Volatile.Read(ref _connectionConfigurations);
            var protocolsByConnection = connectionConfigurations
                .Where(connection => connection.IsActive &&
                    !string.IsNullOrWhiteSpace(connection.ConnectionName))
                .GroupBy(connection => connection.ConnectionName, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => CreateProtocol(group.Last().CommunicationProtocol),
                    StringComparer.Ordinal);
            var bindingIdsByInstruction = instructionMap.Instructions
                .Where(instruction => !string.IsNullOrEmpty(instruction.Instruction))
                .GroupBy(instruction => instruction.Instruction, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Last().InstructionBindingId,
                    StringComparer.Ordinal);
            var exitIdsByBinding = instructionMap.Bindings
                .Where(binding => binding.IsActive && binding.ExitId is > 0)
                .GroupBy(binding => binding.Id)
                .ToDictionary(group => group.Key, group => group.Last().ExitId!.Value);
            var exitsByIdentifier = packageExits
                .Where(packageExit => packageExit.IsActive)
                .GroupBy(packageExit => packageExit.Id)
                .ToDictionary(group => group.Key, group => group.Last());
            var activeAbnormalExit = packageExits
                .FirstOrDefault(packageExit =>
                    packageExit is { IsActive: true, Type: ExitType.AbnormalExit });
            Volatile.Write(
                ref _routingSnapshot,
                new PackageExitRoutingSnapshot(
                    protocolsByConnection,
                    bindingIdsByInstruction,
                    exitIdsByBinding,
                    exitsByIdentifier,
                    activeAbnormalExit));
        }

        private static IDeviceCommunicationProtocol? CreateProtocol(string? protocolName)
        {
            if (!Enum.TryParse<CommunicationProtocol>(protocolName, true, out var communicationProtocol))
            {
                return null;
            }

            return communicationProtocol switch
            {
                CommunicationProtocol.Wxkc => new WxkcCommunicationProtocol(),
                CommunicationProtocol.JT_ST => new JtstCommunicationProtocol(),
                CommunicationProtocol.CaiNiao => new CaiNiaoCommunicationProtocol(),
                _ => null
            };
        }

        private sealed record InstructionMapSnapshot(
            SortingInstructionInfoModel[] Instructions,
            SortingInstructionBindingInfoModel[] Bindings)
        {
            public static readonly InstructionMapSnapshot Empty = new(
                [],
                []);
        }
    }
}
