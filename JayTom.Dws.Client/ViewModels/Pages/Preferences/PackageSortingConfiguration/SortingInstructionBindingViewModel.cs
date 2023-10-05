using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.PackageSorting;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    //分拣指令绑定页面
    public class SortingInstructionBindingViewModel : BindableBase {

        private ObservableCollection<SortingInstructionBindingItemInfoModel> _sortingInstructionBindingItems = new()
        {
            new SortingInstructionBindingItemInfoModel()
            {
                CreateTime = DateTime.Now,
                DelaySendMilliseconds = 200,
                ExitId = 1,
                ExitName = "格口1",
                IsActive = true,
                ModifyTime = DateTime.Now,
                Num = 1,
                Remarks = "这是备注",
                SendIntervalMilliseconds = 100,
            }
        };

        public ObservableCollection<SortingInstructionBindingItemInfoModel> SortingInstructionBindingItems {
            get => _sortingInstructionBindingItems;
            set => SetProperty(ref _sortingInstructionBindingItems, value);
        }
    }
}