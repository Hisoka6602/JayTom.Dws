using JayTom.Dws.Application.Configuration;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Legacy.Contracts.Dto.BaseInfoModels;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;

namespace JayTom.Dws.Client.Service.Device
{

    public class DefaultGrayscaleService : IGrayscaleService
    {
        private readonly ISettingsStore _settingsStore;
        private readonly IGrayscaleDevice _grayscaleDevice;
        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
        private readonly SemaphoreSlim _sendGate = new(1, 1);
        private int _isConnected;

        public DefaultGrayscaleService(ISettingsStore settingsStore,
            IGrayscaleDevice grayscaleDevice)
        {
            _settingsStore = settingsStore;
            _grayscaleDevice = grayscaleDevice;

            _grayscaleDevice.Connected += (sender, s) =>
            {
                OnConnected(this);
            };
            _grayscaleDevice.Disconnected += (sender, s) =>
            {
                OnDisconnected(this);
            };

            _grayscaleDevice.ParcelLocationReceived += (sender, result) =>
            {
                OnGrayscaleSensorResultReceived(result);
            };
            _grayscaleDevice.ParcelLocationNotReceived += (sender, result) =>
            {
                OnParcelLocationNotReceived();
            };
        }

        public bool IsConnected => Volatile.Read(ref _isConnected) != 0;

        public async Task<KeyValuePair<bool, string>> StartSensor()
        {
            await _lifecycleGate.WaitAsync();
            try
            {
                if (!IsConnected)
                {
                    //连接

                    var grayscaleDeviceSettingsDto = await _settingsStore
                                                         .GetAsync<GrayscaleDeviceSettingsDto>("GrayscaleDeviceSettings") ??
                                                     new GrayscaleDeviceSettingsDto();
                    if (grayscaleDeviceSettingsDto is { IsUseGrayscaleDetector: true, TcpConnectionConfigInfo: not null })
                    {
                        var isConnected = grayscaleDeviceSettingsDto.TcpConnectionConfigInfo.ConnectionMode switch
                        {
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
                        Interlocked.Exchange(ref _isConnected, isConnected ? 1 : 0);
                        _grayscaleDevice.SetCircularArrayCarCount(grayscaleDeviceSettingsDto.LineCarCount, grayscaleDeviceSettingsDto.CarNumberOffset);
                        _grayscaleDevice.SetDirectionReversed(grayscaleDeviceSettingsDto.IsDirectionReversed);
                        _grayscaleDevice.SetRegionCarCount(grayscaleDeviceSettingsDto.RegionCarCount);
                        _grayscaleDevice.SetRectangleSizes(new Coordinates(grayscaleDeviceSettingsDto.AdditionalFrameRegion.X,
                            grayscaleDeviceSettingsDto.AdditionalFrameRegion.Y, grayscaleDeviceSettingsDto.AdditionalFrameRegion.Width,
                            grayscaleDeviceSettingsDto.AdditionalFrameRegion.Height),
                            new Coordinates(grayscaleDeviceSettingsDto.MainFrameRegion.X,
                                grayscaleDeviceSettingsDto.MainFrameRegion.Y, grayscaleDeviceSettingsDto.MainFrameRegion.Width,
                                grayscaleDeviceSettingsDto.MainFrameRegion.Height), grayscaleDeviceSettingsDto.AdditionalBoxSpacePercentage,
                            grayscaleDeviceSettingsDto.MinSendInterval);
                        return new KeyValuePair<bool, string>(IsConnected, IsConnected ? "连接成功" : "连接失败");
                    }

                    return new KeyValuePair<bool, string>(true, string.Empty);
                    //注册事件
                }

                return new KeyValuePair<bool, string>(IsConnected, string.Empty);
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        public async Task<KeyValuePair<bool, string>> StopSensor()
        {
            await _lifecycleGate.WaitAsync();
            try
            {
                await _sendGate.WaitAsync();
                try
                {
                    if (IsConnected)
                    {
                        //断开
                        _grayscaleDevice.Close();
                        Interlocked.Exchange(ref _isConnected, 0);
                    }
                }
                finally
                {
                    _sendGate.Release();
                }

                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        public event EventHandler<IGrayscaleService>? Connected;

        public event EventHandler<IGrayscaleService>? Disconnected;

        public event EventHandler<GrayscaleResult>? GrayscaleSensorResultReceived;

        public event EventHandler? ParcelLocationNotReceived;

        public async Task<GrayscaleResult?> GetSingleGrayscaleSensorResult(object param, int timeOut, CancellationToken token)
        {
            if (param is long carNum)
            {
                await _sendGate.WaitAsync(token);
                try
                {
                    return await _grayscaleDevice.SendCarNumber((int)carNum, timeOut, token);
                }
                finally
                {
                    _sendGate.Release();
                }
            }
            return null;
        }

        public void ContinuousGrayscaleSensorReading(object param, CancellationToken token)
        {
            if (param is int carNum)
            {
                SendCarNumberAsync(carNum, token)
                    .Forget("发送灰度车号");
            }
        }

        private async Task SendCarNumberAsync(int carNum, CancellationToken token)
        {
            var lockTaken = false;
            try
            {
                await _sendGate.WaitAsync(token);
                lockTaken = true;
                await _grayscaleDevice.SendCarNumber(carNum, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 调用方取消属于正常结束。
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error($"灰度仪连续读取异常:{e}");
            }
            finally
            {
                if (lockTaken)
                {
                    _sendGate.Release();
                }
            }
        }

        public int IncreaseCarCount(int carNum, int additionalCarCount)
        {
            return _grayscaleDevice.IncreaseCarCount(carNum, additionalCarCount);
        }

        protected virtual void OnConnected(IGrayscaleService e)
        {
            Interlocked.Exchange(ref _isConnected, 1);
            Connected?.Invoke(this, e);
        }

        protected virtual void OnDisconnected(IGrayscaleService e)
        {
            Interlocked.Exchange(ref _isConnected, 0);
            Disconnected?.Invoke(this, e);
        }

        protected virtual void OnGrayscaleSensorResultReceived(GrayscaleResult e)
        {
            GrayscaleSensorResultReceived?.Invoke(this, e);
        }

        protected virtual void OnParcelLocationNotReceived()
        {
            ParcelLocationNotReceived?.Invoke(this, EventArgs.Empty);
        }
    }
}
