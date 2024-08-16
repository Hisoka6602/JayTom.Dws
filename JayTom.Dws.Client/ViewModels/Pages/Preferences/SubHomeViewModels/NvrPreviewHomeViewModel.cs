using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Client.Models.Cameras.CameraConfiguration;
using ApplicationStatus = JayTom.Dws.Domain.EventMediators.ApplicationStatus;
using ApplicationStatusChanged = JayTom.Dws.Domain.EventMediators.ApplicationStatusChanged;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.SubHomeViewModels {

    public class NvrPreviewHomeViewModel : BindableBase {
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private ObservableCollection<NvrPreviewViewItemInfo> _nvrPreviewViewItems = new();
        private BaseDaHuatech? _baseDaHuatech;

        public ObservableCollection<NvrPreviewViewItemInfo> NvrPreviewViewItems {
            get => _nvrPreviewViewItems;
            set => SetProperty(ref _nvrPreviewViewItems, value);
        }

        public NvrPreviewHomeViewModel(INvrCameraBindingRepository nvrCameraBindingRepository) {
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            //INvr管理器

            //判断绑定和连接

            //判断启停
            _baseDaHuatech ??= BaseDaHuatech.CreateInstance();
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item => {
                if (item is { } info) {
                    if (info.Status == ApplicationStatus.Start) {
                        //启动
                        //获取已经绑定的扫码枪
                        //判断已经绑定的
                    }
                    else if (info.Status == ApplicationStatus.Stop) {
                        //停止
                        var itemInfos = NvrPreviewViewItems.Where(w => w.VideoFrame != null).ToList();
                        foreach (var itemInfo in itemInfos) {
                            _baseDaHuatech?.StopRealtimePreview(itemInfo.SerialNumber, itemInfo.ChannelId);
                            itemInfo.Dispose();
                        }
                        NvrPreviewViewItems.Clear();
                    }
                }
            });
        }
    }
}