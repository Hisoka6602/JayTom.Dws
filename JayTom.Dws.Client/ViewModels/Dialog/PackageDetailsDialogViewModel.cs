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
using System.Collections.Generic;
using JayTom.Dws.Client.Models.DataModels;

namespace JayTom.Dws.Client.ViewModels.Dialog {

    public class PackageDetailsDialogViewModel : BindableBase, IDialogAware {
        private PackageItemModel _packageItem = new();
        private string _packageCreationInstruction = string.Empty;
        private string _sentInstruction = string.Empty;
        private string _receivedInstruction = string.Empty;

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

        public PackageItemModel PackageItem {
            get => _packageItem;
            set => SetProperty(ref _packageItem, value);
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
            var createPackageItems = PackageItem.SortingInfo.InstructionInfoItems?.Where(w => w.InstructionType == InstructionType.CreatePackage)
                ?.Select(s =>
                    $"{s.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}->{s.InstructionContent}")
                ?.ToList();
            if (createPackageItems?.Any() == true) {
                PackageCreationInstruction = string.Join("\n", createPackageItems);
            }

            var sendSortingItems = PackageItem.SortingInfo.InstructionInfoItems?.Where(w => w.InstructionType == InstructionType.SendSorting)
                ?.Select(s =>
                    $"{s.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}->{s.InstructionContent}")
                ?.ToList();
            if (sendSortingItems?.Any() == true) {
                SentInstruction = string.Join("\n", sendSortingItems);
            }

            var signalCallbackItems = PackageItem.SortingInfo.InstructionInfoItems?.Where(w => w.InstructionType == InstructionType.SignalCallback)
                ?.Select(s =>
                    $"{s.InstructionGeneratedTime:yyyy-MM-dd HH:mm:ss.fff}->{s.InstructionContent}")
                ?.ToList();
            if (signalCallbackItems?.Any() == true) {
                ReceivedInstruction = string.Join("\n", signalCallbackItems);
            }
        }

        public string Title => "包裹详情";

        public event Action<IDialogResult>? RequestClose;

        public ICommand CloseWinCommand {
            get => new DelegateCommand<object>(CloseWinDelegate);
        }

        private void CloseWinDelegate(object obj) {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<UserControl>(LoadedDelegate);
        }

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