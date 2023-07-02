using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

namespace JayTom.Dws.PluginInterface {

    public interface IViewIPlugin : IPlugin {

        /// <summary>
        /// 视图内容
        /// </summary>
        UserControl Content { get; }

        /// <summary>
        /// View、ViewModel绑定配置
        /// </summary>
        /// <returns></returns>
        List<KeyValuePair<string, Type>> ConfigureViewModelLocator();
    }
}