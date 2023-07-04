using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class PluginMarketplaceViewModel : BindableBase {
        private ObservableCollection<PluginItemInfoModel> _pluginItems;

        public ObservableCollection<PluginItemInfoModel> PluginItems {
            get => _pluginItems;
            set => SetProperty(ref _pluginItems, value);
        }

        public PluginMarketplaceViewModel() {
            _pluginItems = new ObservableCollection<PluginItemInfoModel>()
            {
                new()
                {
                    Name = "IInnerPlugin",
                    Author = "Hisoka"
                }
            };
        }
    }
}