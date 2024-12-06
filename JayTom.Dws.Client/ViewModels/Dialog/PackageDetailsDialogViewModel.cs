using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Windows.Controls;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.DataModels;
using JayTom.Dws.Domain.Repository.LocalData;

namespace JayTom.Dws.Client.ViewModels.Dialog {

    public class PackageDetailsDialogViewModel : BindableBase, IDialogAware {
        private readonly INodeRepository _nodeRepository;
        private readonly IApiRepository _apiRepository;
        private readonly ISortingRepository _sortingRepository;
        private PackageItemModel _packageItem = new();
        private string _packageCreationInstruction = string.Empty;
        private string _sentInstruction = string.Empty;
        private string _receivedInstruction = string.Empty;
        private string _exceptionInstruction = string.Empty;
        private ObservableCollection<NodeInfoItemModel> _nodeInfoItems = new();
        private SortingItemModel _sortingItem = new();

        public string PackageCreationInstruction {
            get => _packageCreationInstruction;
            set => SetProperty(ref _packageCreationInstruction, value);
        }

        public string SentInstruction {
            get => _sentInstruction;
            set => SetProperty(ref _sentInstruction, value);
        }

        public string ReceivedInstruction {
            get => _receivedInstruction;
            set => SetProperty(ref _receivedInstruction, value);
        }

        public string ExceptionInstruction {
            get => _exceptionInstruction;
            set => SetProperty(ref _exceptionInstruction, value);
        }

        public ObservableCollection<NodeInfoItemModel> NodeInfoItems {
            get => _nodeInfoItems;
            set => SetProperty(ref _nodeInfoItems, value);
        }

        public PackageItemModel PackageItem {
            get => _packageItem;
            set => SetProperty(ref _packageItem, value);
        }

        public SortingItemModel SortingItem {
            get => _sortingItem;
            set => SetProperty(ref _sortingItem, value);
        }

        public PackageDetailsDialogViewModel(INodeRepository nodeRepository,
            IApiRepository apiRepository, ISortingRepository sortingRepository) {
            _nodeRepository = nodeRepository;
            _apiRepository = apiRepository;
            _sortingRepository = sortingRepository;
        }

        public bool CanCloseDialog() {
            return true;
        }

        public void OnDialogClosed() {
        }

        public void OnDialogOpened(IDialogParameters parameters) {
            foreach (Window window in Application.Current.Windows) {
                if (window.Name.Equals("PackageDetailsDialog")) {
                    window.Close();
                }
            }
            PackageItem = parameters.GetValue<PackageItemModel>("PackageItem");

            //查询节点信息
            Task.Run(async () => {
                var selectOrderByDescending = await _nodeRepository.SelectOrderByDescending(s => s.PackageId.Equals(PackageItem.PackageId),
                    o => o.ScanTime);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    NodeInfoItems.Clear();
                    var nodeInfoItemModels = selectOrderByDescending.OrderBy(o => o.NodeNum)
                        .Select(s => new NodeInfoItemModel {
                            ImagePath = s.ImagePath,
                            NodeNum = s.NodeNum,
                            NodeName = s.NodeName,
                            OriginalText = s.OriginalText,
                            ScanTime = s.ScanTime,
                            SerialNumber = s.SerialNumber
                        })
                        .ToList();
                    NodeInfoItems.AddRange(nodeInfoItemModels);
                });
            });
            //查询分拣信息
            Task.Run(async () => {
                //查询分拣信息

                var sortingInfoModel = await _sortingRepository.FirstOrDefault(f =>
                    f.PackageId.Equals(PackageItem.PackageId));
                if (sortingInfoModel is not null) {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        SortingItem = new SortingItemModel() {
                            IsAbnormalSorting = sortingInfoModel.IsAbnormalSorting,
                            SortingCode = sortingInfoModel.SortingCode,
                            SortingMode = sortingInfoModel.SortingMode,
                            IsCreatedByLowerMachine = sortingInfoModel.IsCreatedByLowerMachine,
                            CommunicationMethod = sortingInfoModel.CommunicationMethod,
                            ChecksumProtocolName = sortingInfoModel.ChecksumProtocolName,
                            ConnectionName = sortingInfoModel.ConnectionName,
                            IsSortingUsed = sortingInfoModel.IsSortingUsed,
                            AbnormalSortingType = sortingInfoModel.AbnormalSortingType
                        };
                        var createPackageItems = sortingInfoModel.InstructionInfos?.Where(w => w.InstructionType == InstructionType.CreatePackage)
                            ?.Select(s =>
                                $"{s.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}->{s.InstructionContent}")
                            ?.ToList();
                        if (createPackageItems?.Any() == true) {
                            PackageCreationInstruction = string.Join("\n", createPackageItems);
                        }

                        var sendSortingItems = sortingInfoModel.InstructionInfos?.Where(w => w.InstructionType == InstructionType.SendSorting)
                            ?.Select(s =>
                                $"{s.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}->{s.InstructionContent}")
                            ?.ToList();
                        if (sendSortingItems?.Any() == true) {
                            SentInstruction = string.Join("\n", sendSortingItems);
                        }
                        var signalCallbackItems = sortingInfoModel.InstructionInfos?.Where(w => w.InstructionType == InstructionType.SignalCallback)
                            ?.Select(s =>
                                $"{s.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}->{s.InstructionContent}")
                            ?.ToList();
                        if (signalCallbackItems?.Any() == true) {
                            ReceivedInstruction = string.Join("\n", signalCallbackItems);
                        }
                        var exceptionInstructionItems = sortingInfoModel.InstructionInfos?.Where(w =>
                                w.InstructionType is InstructionType.PackageException or InstructionType.PackageExceptionEx)
                            ?.Select(s =>
                                $"{s.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}->{s.InstructionContent}")
                            ?.ToList();
                        if (exceptionInstructionItems?.Any() == true) {
                            ExceptionInstruction = string.Join("\n", exceptionInstructionItems);
                        }
                    });
                }

                //填充其他分拣信息
            });
            //查询Api信息
        }

        public string Title => "包裹详情";

        public event Action<IDialogResult>? RequestClose;

        public ICommand CloseWinCommand => new DelegateCommand<object>(CloseWinDelegate);

        private void CloseWinDelegate(object obj) {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        public ICommand LoadedCommand => new DelegateCommand<UserControl>(LoadedDelegate);

        private void LoadedDelegate(UserControl obj) {
            var dialogWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            if (dialogWindow is not null) {
                dialogWindow.Owner = null;
                dialogWindow.Name = "PackageDetailsDialog";
            }
        }

        public class DetailsInstructionInfo {
            public DateTime? InstructionTime { get; set; }
            public string? Instruction { get; set; }
        }
    }
}