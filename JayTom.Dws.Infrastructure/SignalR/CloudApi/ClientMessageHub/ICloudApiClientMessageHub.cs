using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;

namespace JayTom.Dws.Infrastructure.SignalR.CloudApi.ClientMessageHub {

    public interface ICloudApiClientMessageHub : IBaseClientMessageHub {

        Task<bool> SyncSettingsInfo<T>(string settingsName, T message);
    }
}