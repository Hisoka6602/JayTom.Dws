using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>集中完成持久化实体到应用读模型的映射。</summary>
internal static class PackageHistoryMapper
{
    /// <summary>创建完全脱离 EF 导航集合的不可变读模型。</summary>
    internal static PackageHistoryItem Map(PackageInfoModel source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new PackageHistoryItem(
            source.PackageTimestamped,
            source.PackageCreateTime,
            source.Other ?? string.Empty,
            source.BarCodeInfo is null
                ? null
                : new PackageHistoryBarcode(
                    source.BarCodeInfo.Barcode,
                    source.BarCodeInfo.ScanTime,
                    source.BarCodeInfo.SerialNumber),
            source.WeightInfo is null
                ? null
                : new PackageHistoryWeight(
                    source.WeightInfo.FormattedWeight,
                    source.WeightInfo.OriginalText,
                    source.WeightInfo.CreateTime,
                    source.WeightInfo.SourceType),
            source.VolumeInfo is null
                ? null
                : new PackageHistoryVolume(
                    source.VolumeInfo.FormattedLength,
                    source.VolumeInfo.FormattedWidth,
                    source.VolumeInfo.FormattedHeight,
                    source.VolumeInfo.FormattedVolume,
                    source.VolumeInfo.OriginalText,
                    source.VolumeInfo.CreateTime,
                    source.VolumeInfo.SourceType),
            source.UploadInfo is null
                ? null
                : new PackageHistoryUpload(
                    source.UploadInfo.RequestStatus,
                    source.UploadInfo.RequestContent,
                    source.UploadInfo.ResponseContent,
                    source.UploadInfo.RequestTime,
                    source.UploadInfo.ResponseTime,
                    source.UploadInfo.DurationInSeconds,
                    source.UploadInfo.InterfaceParameters,
                    source.UploadInfo.RequestUrl,
                    source.UploadInfo.ExceptionMessage),
            source.ExitInfo is null
                ? null
                : new PackageHistoryExit(
                    source.ExitInfo.TheoreticalExit,
                    source.ExitInfo.PhysicalExit,
                    source.ExitInfo.PhysicalExitId),
            source.SortingInfo is null
                ? null
                : new PackageHistorySorting(
                    source.SortingInfo.IsSortingUsed,
                    source.SortingInfo.SortingCode,
                    source.SortingInfo.SortingMode,
                    source.SortingInfo.IsCreatedByLowerMachine,
                    source.SortingInfo.CommunicationMethod,
                    source.SortingInfo.ChecksumProtocolName,
                    source.SortingInfo.ConnectionName,
                    source.SortingInfo.IsAbnormalSorting,
                    source.SortingInfo.AbnormalSortingType,
                    Array.AsReadOnly(source.SortingInfo.InstructionInfos?
                        .Select(item => new PackageHistoryInstruction(
                            item.InstructionContent,
                            item.InstructionGeneratedTime,
                            item.InstructionType))
                        .ToArray() ?? [])),
            source.OcrInfo is null
                ? null
                : new PackageHistoryOcr(source.OcrInfo.RecognizeTime),
            Array.AsReadOnly(source.ImageInfos?
                .Select(item => new PackageHistoryImage(item.Type, item.LocalPath))
                .ToArray() ?? []),
            source.CloudVideoUploadInfo?.UploadTime is not null);
    }
}
