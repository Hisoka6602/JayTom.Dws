using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.LogsItemModels;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {
    public class RealTimeLogViewModel : BindableBase {

        private ObservableCollection<BaseLogItemModel> _logItems = new()
        {
            new BaseLogItemModel()
            {
                CreateTime = DateTime.Now,
                Message = $"如果您需要在 TextBlock 控件中支持文本的复制操作，您可以将 TextBlock 放置在一个 Border 内，然后设置 Border 的 Background 以及 BorderThickness 属性，使其看起来像一个文本框。这样，用户可以选择并复制 TextBlock 内的文本。以下是一个示例："
            },
            new BaseLogItemModel()
            {
                CreateTime = DateTime.Now,
                Message = $"如果您需要在 TextBlock 控件中支持文本的复制操作，您可以将 TextBlock 放置在一个 Border 内，然后设置 Border 的 Background 以及 BorderThickness 属性，使其看起来像一个文本框。这样，用户可以选择并复制 TextBlock 内的文本。以下是一个示例："
            },
            new BaseLogItemModel()
            {
                CreateTime = DateTime.Now,
                Message = $"如果您需要在 TextBlock 控件中支持文本的复制操作，您可以将 TextBlock 放置在一个 Border 内，然后设置 Border 的 Background 以及 BorderThickness 属性，使其看起来像一个文本框。这样，用户可以选择并复制 TextBlock 内的文本。以下是一个示例："
            },
            new BaseLogItemModel()
            {
                CreateTime = DateTime.Now,
                Message = $"如果您需要在 TextBlock 控件中支持文本的复制操作，您可以将 TextBlock 放置在一个 Border 内，然后设置 Border 的 Background 以及 BorderThickness 属性，使其看起来像一个文本框。这样，用户可以选择并复制 TextBlock 内的文本。以下是一个示例："
            },
            new BaseLogItemModel()
            {
                CreateTime = DateTime.Now,
                Message = $"如果您需要在 TextBlock 控件中支持文本的复制操作，您可以将 TextBlock 放置在一个 Border 内，然后设置 Border 的 Background 以及 BorderThickness 属性，使其看起来像一个文本框。这样，用户可以选择并复制 TextBlock 内的文本。以下是一个示例："
            },
            new BaseLogItemModel()
            {
                CreateTime = DateTime.Now,
                Message = $"如果您需要在 TextBlock 控件中支持文本的复制操作，您可以将 TextBlock 放置在一个 Border 内，然后设置 Border 的 Background 以及 BorderThickness 属性，使其看起来像一个文本框。这样，用户可以选择并复制 TextBlock 内的文本。以下是一个示例："
            },
            new BaseLogItemModel()
            {
                CreateTime = DateTime.Now,
                Message = $"如果您需要在 TextBlock 控件中支持文本的复制操作，您可以将 TextBlock 放置在一个 Border 内，然后设置 Border 的 Background 以及 BorderThickness 属性，使其看起来像一个文本框。这样，用户可以选择并复制 TextBlock 内的文本。以下是一个示例："
            },
            new BaseLogItemModel()
            {
                CreateTime = DateTime.Now,
                Message = $"如果您需要在 TextBlock 控件中支持文本的复制操作，您可以将 TextBlock 放置在一个 Border 内，然后设置 Border 的 Background 以及 BorderThickness 属性，使其看起来像一个文本框。这样，用户可以选择并复制 TextBlock 内的文本。以下是一个示例："
            },
            new BaseLogItemModel()
            {
                CreateTime = DateTime.Now,
                Message = $"如果您需要在 TextBlock 控件中支持文本的复制操作，您可以将 TextBlock 放置在一个 Border 内，然后设置 Border 的 Background 以及 BorderThickness 属性，使其看起来像一个文本框。这样，用户可以选择并复制 TextBlock 内的文本。以下是一个示例："
            },
            new BaseLogItemModel()
            {
                CreateTime = DateTime.Now,
                Message = $"如果您需要在 TextBlock 控件中支持文本的复制操作，您可以将 TextBlock 放置在一个 Border 内，然后设置 Border 的 Background 以及 BorderThickness 属性，使其看起来像一个文本框。这样，用户可以选择并复制 TextBlock 内的文本。以下是一个示例："
            },
        };

        public RealTimeLogViewModel() {
        }

        public ObservableCollection<BaseLogItemModel> LogItems {
            get => _logItems;
            set => SetProperty(ref _logItems, value);
        }
    }
}