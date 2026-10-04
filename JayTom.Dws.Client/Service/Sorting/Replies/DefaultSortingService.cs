using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Packages;
using JayTom.Dws.Legacy.Contracts.DownstreamProtocols;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Integrations.Cloud;
using JayTom.Dws.Client.Observability;
using JayTom.Dws.Models.LocalLog;

namespace JayTom.Dws.Client.Service.Sorting
{
    /// <summary>集中维护完成包裹的单次 PLC 回复、异步 OCR 等待及异常口兜底。</summary>
    public partial class DefaultSortingService
    {
        /// <summary>在原包裹锁内保存分拣所需身份与数据，避免异步结果改变关联。</summary>
        private static SortingParam CreateSortingParam(PackageInfo model)
        {
            lock (model.SyncRoot)
            {
                return new SortingParam
                {
                    Guid = model.Guid,
                    BarCode = model.BarCodeInfo?.Barcode ?? string.Empty,
                    Height = (decimal)(model.VolumeInfo?.FormattedHeight ?? 0),
                    ScanTime = model.BarCodeInfo?.ScanTime,
                    Weight = (decimal)(model.WeightInfo?.FormattedWeight ?? 0),
                    Length = (decimal)(model.VolumeInfo?.FormattedLength ?? 0),
                    Width = (decimal)(model.VolumeInfo?.FormattedWidth ?? 0),
                    Volume = (decimal)(model.VolumeInfo?.FormattedVolume ?? 0),
                    PackageCreationTime = model.CreateTime,
                    PackageCreationInstruction = model.PackageCreationInstruction,
                    IsCreatedByLowerMachine = model.IsCreatedByLowerMachine,
                    IsStackedPackage = model.IsStackedPackage ?? false,
                    LinkedCarCount = model.LinkedCarCount,
                    Timestamp = model.Timestamp
                };
            }
        }

        /// <summary>等待携带原提交时间戳的 OCR 结果，到达配置超时后转入异常口。</summary>
        private void WaitForOcrSorting(SortingParam param)
        {
            if (!_ocrSettingsDto.IsUseOcr)
            {
                ExceptionSorting(param, PackageCloudAbnormalSortingType.UnmatchedRule);
                return;
            }
            var pending = new PendingOcrSorting(param);
            if (!_pendingOcrSorting.TryAdd(param.Timestamp, pending)) return;
            var timeout = TimeSpan.FromMilliseconds(
                _ocrSettingsDto.RecognitionTimeout > 0 ? _ocrSettingsDto.RecognitionTimeout : 30_000);
            pending.SetDeadline(_sortingDeadlineScheduler.Schedule(timeout, () =>
            {
                if (_pendingOcrSorting.TryRemove(param.Timestamp, out var expired))
                {
                    expired.Dispose();
                    TraceSorting("sorting.ocr.timeout", expired.Param, "OcrTimeout", LogType.Warning,
                        ("timeoutMilliseconds", timeout.TotalMilliseconds));
                    ExceptionSorting(expired.Param, PackageCloudAbnormalSortingType.UnmatchedRule);
                }
            }));
            TraceSorting("sorting.ocr.waiting", param, "WaitingForOcr", LogType.Information,
                ("timeoutMilliseconds", timeout.TotalMilliseconds));
        }

        /// <summary>正常口与异常口共用指令构建和发送边界，领取回复前先验证完整配置。</summary>
        private bool TryQueueExitInstruction(
            SortingParam param, PackageExitDefinitionInfoModel exit, bool isFallback, CancellationToken token)
        {
            if (!TryGetExitInstructions(exit.Id, out var binding, out var instructions))
            {
                TraceSorting("sorting.route.rejected", param, "MissingOrInvalidInstructions", LogType.Warning,
                    ("exitId", exit.Id), ("isFallback", isFallback));
                return false;
            }

            var attach = CreateInstructionsAttach(param, exit);
            if (!TrySelectSortingReply(param, attach)) return true;
            var sendFailureReported = 0;
            attach.ValidateBeforeSend = () =>
            {
                if (!IsCurrentPackageIdentity(attach, out var reason)) return reason;
                var liveExits = Volatile.Read(ref _exitSnapshot);
                return liveExits.ExitLookup.TryGetValue(exit.Id, out var liveExit) && liveExit.IsActive &&
                       (isFallback || !liveExit.IsLockExit)
                    ? null : $"待发送目标格口已停用、被锁定或移除:ExitId={exit.Id}";
            };
            attach.OnSendSucceeded = () =>
            {
                MarkSortingInstructionSent(attach);
                TraceSorting("sorting.reply.sent", param, "InstructionsWritten", LogType.Information,
                    ("exitId", exit.Id), ("exitName", exit.ExitName), ("isFallback", isFallback),
                    ("instructionCount", instructions.Length));
            };
            attach.OnSendFailedBeforeWrite = exception =>
            {
                if (Interlocked.Exchange(ref sendFailureReported, 1) != 0) return;
                TraceSorting("sorting.reply.failed", param, "SendFailedBeforeWrite", LogType.Exception,
                    ("exitId", exit.Id), ("isFallback", isFallback), ("writeStarted", false), ("error", exception.ToString()));
                if (isFallback)
                {
                    ReportUnavailableAbnormalExit(exit.Id, exception, param);
                    return;
                }
                // 只有连接层确认尚未开始写入时，才允许把正常口替换为异常口。
                if (!param.IsAlternateExitInstruction)
                    _selectedSortingReplies.TryRemove(param.PackageCreationTime.Ticks, out _);
                HandleSortingFailure(param, exception, token);
            };
            // 先发布理论格口，再入发送队列；物理回执可能在本机瞬间到达。
            try
            {
                _eventBus.Publish(new SortingExitReceived
                {
                    ScanTime = param.ScanTime, BarCode = param.BarCode, Timestamp = param.Timestamp,
                    ExitName = exit.ExitName ?? string.Empty, ExitId = exit.Id,
                    ExitType = SortingExitType.PhysicalExit, Type = exit.Type, SortingParam = param
                });
            }
            catch (Exception exception)
            {
                TraceSorting("sorting.notification.failed", param, "SelectedExitNotificationFailed", LogType.Warning,
                    ("exitId", exit.Id), ("error", exception.ToString()));
                NLog.LogManager.GetCurrentClassLogger().Warn(exception,
                    "理论格口通知失败，继续发送已选定的 PLC 格口");
            }
            QueuePackageSortingWork(param, () =>
            {
                if (!IsCurrentPackageIdentity(attach, out var reason))
                {
                    TraceSorting("sorting.reply.rejected", param, "IdentityChangedBeforeDispatch", LogType.Warning,
                        ("exitId", exit.Id), ("reason", reason));
                    NLog.LogManager.GetCurrentClassLogger().Warn(
                        $"发送前包裹身份复核失败，已禁止格口指令:{reason}");
                    return Task.CompletedTask;
                }
                if (attach.ValidateBeforeSend() is { } unavailableReason)
                {
                    attach.OnSendFailedBeforeWrite(new InvalidOperationException(unavailableReason));
                    return Task.CompletedTask;
                }
                try
                {
                    _sortingConnectionService.SendInstructions(param.Tag ?? new object(), exit.Id,
                        instructions, TimeSpan.FromMilliseconds(binding.SendIntervalMilliseconds), attach);
                }
                catch (Exception exception)
                {
                    attach.OnSendFailedBeforeWrite(exception);
                }
                return Task.CompletedTask;
            }, token, isFallback, TimeSpan.FromMilliseconds(binding.DelaySendMilliseconds));
            TraceSorting("sorting.reply.queued", param, isFallback ? "AbnormalExitSelected" : "NormalExitSelected",
                LogType.Information, ("exitId", exit.Id), ("exitName", exit.ExitName), ("isFallback", isFallback),
                ("bindingId", binding.Id), ("instructionCount", instructions.Length),
                ("delayMilliseconds", binding.DelaySendMilliseconds), ("sendIntervalMilliseconds", binding.SendIntervalMilliseconds));

            return true;
        }

        /// <summary>使用预建索引确认格口具有有效绑定及非空指令内容。</summary>
        private bool TryGetExitInstructions(long exitId, out SortingInstructionBindingInfoModel binding,
            out SortingInstructionInfoModel[] instructions)
        {
            binding = null!;
            instructions = [];
            if (!Volatile.Read(ref _sortingInstructionBindingByExit).TryGetValue(exitId, out var configuredBinding) ||
                !Volatile.Read(ref _sortingInstructionsByBinding).TryGetValue(configuredBinding.Id, out var configuredInstructions) ||
                configuredInstructions.Length == 0 || configuredInstructions.Any(item => string.IsNullOrWhiteSpace(item.Instruction)))
                return false;
            binding = configuredBinding;
            instructions = configuredInstructions;
            return true;
        }

        /// <summary>为目标口和异常口构建同一原车身份的发送附件。</summary>
        private InstructionsAttach CreateInstructionsAttach(SortingParam param, PackageExitDefinitionInfoModel? exit) => new()
        {
            BarCode = param.BarCode, ExitName = exit?.ExitName ?? string.Empty,
            Guid = param.Guid, Timestamp = param.Timestamp, Height = param.Height, Length = param.Length,
            Volume = param.Volume, Width = param.Width, Weight = param.Weight, ScanTime = param.ScanTime,
            ExitId = exit?.Id ?? 0, SortingMode = CurrentSortingMethod.SortMode,
            PackageCreationTime = param.PackageCreationTime,
            PackageCreationInstruction = param.PackageCreationInstruction ?? string.Empty,
            IsCreatedByLowerMachine = param.IsCreatedByLowerMachine, LinkedCarCount = param.LinkedCarCount
        };

        /// <summary>活动身份验证成功后原子领取回复，保留显式备用口重发。</summary>
        private bool TrySelectSortingReply(SortingParam param, InstructionsAttach attach)
        {
            if (!IsCurrentPackageIdentity(attach, out var reason))
            {
                TraceSorting("sorting.reply.rejected", param, "OriginalPackageUnavailable", LogType.Warning,
                    ("exitId", attach.ExitId), ("reason", reason));
                return false;
            }
            if (!param.IsAlternateExitInstruction &&
                !_selectedSortingReplies.TryAdd(param.PackageCreationTime.Ticks, 0))
            {
                TraceSorting("sorting.reply.duplicate", param, "ReplyAlreadySelected", LogType.Information,
                    ("exitId", attach.ExitId));
                return false;
            }
            CancelPendingOcrSorting(param.Timestamp);
            return true;
        }

        /// <summary>选定最终格口或移除原车时取消对应 OCR 超时任务。</summary>
        private void CancelPendingOcrSorting(long timestamp)
        {
            if (_pendingOcrSorting.TryRemove(timestamp, out var pending)) pending.Dispose();
        }

        /// <summary>记录已处理的分拣失败并为原包裹排队异常口兜底。</summary>
        private void HandleSortingFailure(SortingParam param, Exception exception, CancellationToken token)
        {
            TraceSorting("sorting.failure", param, "SortingProcessingFailed", LogType.Exception,
                ("error", exception.ToString()));
            NLog.LogManager.GetCurrentClassLogger().Warn(exception,
                $"分拣处理失败，使用原包裹异常口兜底:Timestamp={param.Timestamp},Guid={param.Guid}");
            ExceptionSorting(param, PackageCloudAbnormalSortingType.NoSortingInstruction, token);
        }

        /// <summary>异常口配置或连接不可用时明确报告故障，不能伪造发送成功。</summary>
        private void ReportUnavailableAbnormalExit(long exitId, Exception? exception = null, SortingParam? param = null)
        {
            var message = $"已设置异常口但无法发送，请检查其指令绑定、指令内容与通信连接:ExitId={exitId}";
            if (param is not null) TraceSorting("sorting.fallback.unavailable", param, "ConfiguredAbnormalExitUnavailable",
                LogType.Exception, ("exitId", exitId), ("error", exception?.ToString()));
            NLog.LogManager.GetCurrentClassLogger().Error(exception, message);
            OnExceptionOccurred(new ExceptionEventArgs { ExceptionMessage = message });
        }

        /// <summary>隔离单包裹处理异常，使后续包裹继续执行并且本车取得兜底结果。</summary>
        private void QueuePackageSortingWork(SortingParam param, Func<Task> work,
            CancellationToken token, bool isFallback = false, TimeSpan delay = default)
        {
            QueueSortingWork(async () =>
            {
                try { await work().ConfigureAwait(false); }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { }
                catch (Exception exception)
                {
                    if (isFallback) ReportUnavailableAbnormalExit(
                        Volatile.Read(ref _exitSnapshot).ActiveAbnormalExit?.Id ?? 0, exception, param);
                    else HandleSortingFailure(param, exception, token);
                }
            }, delay);
        }

        /// <summary>用原提交身份串起分拣决策、拒绝、写入结果与兜底原因，文件目标异步写盘。</summary>
        private void TraceSorting(string stage, SortingParam param, string reasonCode, LogType type,
            params (string Key, object? Value)[] additionalFields)
        {
            PipelineFileLog.Instance.Write(stage, "原包裹分拣决策", type, param.Timestamp, param.BarCode, param.Guid.ToString(),
                fields: [("reasonCode", reasonCode), ("createdAt", param.PackageCreationTime),
                    ("creationTimeTicks", param.PackageCreationTime.Ticks), ("scanTime", param.ScanTime),
                    ("mode", CurrentSortingMethod.SortMode), ("isLowerMachine", param.IsCreatedByLowerMachine),
                    .. additionalFields]);
        }


    }
}
