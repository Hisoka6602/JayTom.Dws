using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.PackageExits;
using JayTom.Dws.Application.PackageHistory;
using JayTom.Dws.Application.UseCases;
using System;
using DryIoc;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Plugin;
using System.Diagnostics;
using System.Windows.Input;
using JayTom.Dws.Legacy.Contracts.Dto;
using Prism.Services.Dialogs;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Models.Package;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;
using LibreHardwareMonitor.Hardware;
using JayTom.Dws.Client.Views.Dialog;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Client.Models.DataModels;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Excel;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Views.Dialog.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;

namespace JayTom.Dws.Client.Models.DataModels;

/// <summary>在后台创建尚未绑定的历史包裹展示模型，避免在 UI 线程构造整页对象。</summary>
internal static class DataManagementItemMapper
{
    /// <summary>映射一个历史包裹及其现有详情数据。</summary>
    public static PackageItemModel Map(PackageHistoryItem s, int i) => new PackageItemModel
                            {
                                Num = i + 1,
                                TimestampMilliseconds = s.PackageTimestamped,
                                Barcode = s.BarCodeInfo?.Barcode ?? string.Empty,
                                Weight = (decimal)(s.WeightInfo?.FormattedWeight ?? 0),
                                Length = (decimal)(s.VolumeInfo?.FormattedLength ?? 0),
                                Width = (decimal)(s.VolumeInfo?.FormattedWidth ?? 0),
                                Height = (decimal)(s.VolumeInfo?.FormattedHeight ?? 0),
                                Volume = (decimal)(s.VolumeInfo?.FormattedVolume ?? 0),
                                ScanTime = s.BarCodeInfo?.ScanTime ?? s.PackageCreateTime,
                                RequestStatus = s.UploadInfo?.RequestStatus ?? UploadStatus.NotUploaded,
                                BarcodeImagePath = s.ImageInfos?.LastOrDefault(l => l.Type == 0)?.LocalPath ?? string.Empty,
                                IsBarcodeImageExists = !string.IsNullOrWhiteSpace(
                                    s.ImageInfos?.LastOrDefault(l => l.Type == 0)?.LocalPath),
                                Other = s.Other ?? string.Empty,
                                ExitName = s.ExitInfo?.PhysicalExit ?? string.Empty,
                                PanoramaImageItems = s.ImageInfos?.Where(w => w.Type == 1)?.Select(ps =>
                                new PanoramaImageItemModel()
                                {
                                    IsPanoramaImageExists = !string.IsNullOrWhiteSpace(ps.LocalPath),
                                    PanoramaImagePath = ps.LocalPath
                                })?.ToList() ?? new List<PanoramaImageItemModel>(),
                                UploadInfo = new UploadItemModel
                                {
                                    DurationInSeconds = s.UploadInfo?.DurationInSeconds ?? 0,
                                    ExceptionMessage = s.UploadInfo?.ExceptionMessage ?? string.Empty,
                                    InterfaceParameters = s.UploadInfo?.InterfaceParameters ?? string.Empty,
                                    IsSuccess = s.UploadInfo?.RequestStatus == UploadStatus.Succeeded,
                                    RequestContent = s.UploadInfo?.RequestContent ?? string.Empty,
                                    RequestTime = s.UploadInfo?.RequestTime,
                                    RequestUrl = s.UploadInfo?.RequestUrl ?? string.Empty,
                                    ResponseContent = s.UploadInfo?.ResponseContent ?? string.Empty,
                                    ResponseTime = s.UploadInfo?.ResponseTime
                                },
                                WeightInfo = new WeightItemModel()
                                {
                                    CreateTime = s.WeightInfo?.CreateTime,
                                    FormattedWeight = s.WeightInfo?.FormattedWeight ?? 0,
                                    OriginalText = s.WeightInfo?.OriginalText ?? string.Empty,
                                    SourceType = s.WeightInfo?.SourceType ?? SourceType.SerialPort
                                },
                                SortingInfo = new SortingItemModel
                                {
                                    IsSortingUsed = s.SortingInfo?.IsSortingUsed ?? false,
                                    SortingCode = s.SortingInfo?.SortingCode ?? string.Empty,
                                    SortingMode = s.SortingInfo?.SortingMode ?? SortMode.None,
                                    IsCreatedByLowerMachine = s.SortingInfo?.IsCreatedByLowerMachine ?? false,
                                    CommunicationMethod = s.SortingInfo?.CommunicationMethod ?? CommunicationsType.None,
                                    ChecksumProtocolName = s.SortingInfo?.ChecksumProtocolName ?? string.Empty,
                                    ConnectionName = s.SortingInfo?.ConnectionName ?? string.Empty,
                                    IsAbnormalSorting = s.SortingInfo?.IsAbnormalSorting ?? false,
                                    AbnormalSortingType = s.SortingInfo?.AbnormalSortingType ?? AbnormalSortingType.None,
                                    InstructionInfoItems = new ObservableCollection<InstructionInfoItemModel>(s.SortingInfo?.InstructionInfos?
                                        .Select(s1 => new InstructionInfoItemModel()
                                        {
                                            InstructionGeneratedTime = s1.InstructionGeneratedTime,
                                            InstructionType = s1.InstructionType,
                                            InstructionContent = s1.InstructionContent
                                        })?.ToList() ?? new List<InstructionInfoItemModel>())
                                },
                                VolumeInfo = new VolumeItemModel()
                                {
                                    CreateTime = s.VolumeInfo?.CreateTime,
                                    FormattedHeight = s.VolumeInfo?.FormattedHeight ?? 0,
                                    FormattedLength = s.VolumeInfo?.FormattedLength ?? 0,
                                    FormattedVolume = s.VolumeInfo?.FormattedVolume ?? 0,
                                    FormattedWidth = s.VolumeInfo?.FormattedWidth ?? 0,
                                    OriginalText = s.VolumeInfo?.OriginalText ?? string.Empty,
                                    SourceType = s.VolumeInfo?.SourceType ?? SourceType.Camera
                                },
                                OcrInfo = new OcrItemInfo()
                                {
                                    CreateTime = s.OcrInfo?.RecognizeTime,
                                    /*OcrInterfaceName = s.OcrInfo?.OcrInterfaceName ?? string.Empty,
                                    OriginalContent = s.OcrInfo?.OriginalContent ?? string.Empty,
                                    ParsedContent = s.OcrInfo?.ParsedContent ?? string.Empty*/
                                },
                                ExitInfo = new ExitInfoItemModel()
                                {
                                    PhysicalExitId = s.ExitInfo?.PhysicalExitId ?? 0,
                                    PhysicalExit = s.ExitInfo?.PhysicalExit ?? string.Empty,
                                    TheoreticalExit = s.ExitInfo?.TheoreticalExit ?? string.Empty,
                                },
                                IsUploadedToCloudVideo = s.IsUploadedToCloudVideo
                            };
}
