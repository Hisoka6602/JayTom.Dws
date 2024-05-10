using System;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;

namespace JayTom.Dws.Client.Service.Device {

    public class DefaultGrayscaleService : IGrayscaleService {
        private readonly IConfigRepository _configRepository;
        private readonly IGrayscaleDevice _grayscaleDevice;

        public DefaultGrayscaleService(IConfigRepository configRepository,
            IGrayscaleDevice grayscaleDevice) {
            _configRepository = configRepository;
            _grayscaleDevice = grayscaleDevice;

            _grayscaleDevice.Connected += (sender, s) => {
                OnConnected(this);
            };
            _grayscaleDevice.Disconnected += (sender, s) => {
                OnDisconnected(this);
            };

            _grayscaleDevice.ParcelLocationReceived += (sender, result) => {
                OnGrayscaleSensorResultReceived(result);
            };
            _grayscaleDevice.ParcelLocationNotReceived += (sender, result) => {
                OnParcelLocationNotReceived();
            };
        }

        public bool IsConnected { get; private set; }

        public async Task<KeyValuePair<bool, string>> StartSensor() {
            if (!IsConnected) {
                //连接

                var grayscaleDeviceSettingsDto = await _configRepository
                                                     .FirstOrDefaultEntity<GrayscaleDeviceSettingsDto>("GrayscaleDeviceSettings") ??
                                                 new GrayscaleDeviceSettingsDto();
                if (grayscaleDeviceSettingsDto is { IsUseGrayscaleDetector: true, TcpConnectionConfigInfo: not null }) {
                    IsConnected = grayscaleDeviceSettingsDto.TcpConnectionConfigInfo.ConnectionMode switch {
                        TcpConnectionMode.Client => await _grayscaleDevice.Connect(
                            grayscaleDeviceSettingsDto.TcpConnectionConfigInfo.ClientConfig.IpAddress,
                            grayscaleDeviceSettingsDto.TcpConnectionConfigInfo.ClientConfig.Port, ConnectionType.Client,
                            1000, (FormatType)grayscaleDeviceSettingsDto.TcpConnectionConfigInfo.DataFormat),
                        TcpConnectionMode.Server => await _grayscaleDevice.Connect(
                            grayscaleDeviceSettingsDto.TcpConnectionConfigInfo.ServerConfig.IpAddress,
                            grayscaleDeviceSettingsDto.TcpConnectionConfigInfo.ServerConfig.Port, ConnectionType.Server,
                            1000, (FormatType)grayscaleDeviceSettingsDto.TcpConnectionConfigInfo.DataFormat),
                        _ => false
                    };
                    return new KeyValuePair<bool, string>(IsConnected, IsConnected ? "连接成功" : "连接失败");
                }

                return new KeyValuePair<bool, string>(true, string.Empty);
                //注册事件
            }

            return new KeyValuePair<bool, string>(IsConnected, string.Empty);
        }

        public Task<KeyValuePair<bool, string>> StopSensor() {
            if (IsConnected) {
                //断开
                _grayscaleDevice.Close();
                IsConnected = false;
            }

            return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
        }

        public event EventHandler<IGrayscaleService>? Connected;

        public event EventHandler<IGrayscaleService>? Disconnected;

        public event EventHandler<GrayscaleResult>? GrayscaleSensorResultReceived;

        public event EventHandler? ParcelLocationNotReceived;

        public async Task<GrayscaleResult?> GetSingleGrayscaleSensorResult(object param, int timeOut, CancellationToken token) {
            if (param is long carNum) {
                return await _grayscaleDevice.SendCarNumber((int)carNum, timeOut, token);
            }
            return null;
        }

        public async void ContinuousGrayscaleSensorReading(object param, CancellationToken token) {
            if (param is int carNum) {
                await _grayscaleDevice.SendCarNumber(carNum, token);
            }
        }

        protected virtual async void OnConnected(IGrayscaleService e) {
            await Task.Yield();
            Connected?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(IGrayscaleService e) {
            await Task.Yield();
            Disconnected?.Invoke(this, e);
        }

        protected virtual async void OnGrayscaleSensorResultReceived(GrayscaleResult e) {
            await Task.Yield();
            GrayscaleSensorResultReceived?.Invoke(this, e);
        }

        protected virtual async void OnParcelLocationNotReceived() {
            await Task.Yield();
            ParcelLocationNotReceived?.Invoke(this, EventArgs.Empty);
        }
    }
}