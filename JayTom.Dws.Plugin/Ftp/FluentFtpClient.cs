using System;
using FluentFTP;
using System.Net;
using System.Linq;
using System.Text;
using FluentFTP.Helpers;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Ftp {

    public class FluentFtpClient : IFtp {
        private readonly FtpClient _ftpClient;
        private SemaphoreSlim _semaphore = new(1);
        private SemaphoreSlim _connectSlim = new(1);

        public FluentFtpClient() {
            _ftpClient = new FtpClient() {
                Encoding = Encoding.UTF8
            };
        }

        public bool IsConnected => _ftpClient.IsConnected;

        public event EventHandler<EventArgs>? Connected;

        public event EventHandler<EventArgs>? Disconnected;

        public async Task<KeyValuePair<bool, string>> Connect(string server, int port, string username, string password, CancellationToken cancellationToken = default) {
            try {
                await _connectSlim.WaitAsync(cancellationToken);
                if (_ftpClient.IsConnected) {
                    OnConnected();
                    return new KeyValuePair<bool, string>(true, "连接成功!");
                }
                _ftpClient.Host = server;
                _ftpClient.Port = port;
                _ftpClient.Config.DataConnectionConnectTimeout = 60 * 1000;
                _ftpClient.Config.ConnectTimeout = 60 * 1000;
                _ftpClient.Config.SocketKeepAlive = true;
                _ftpClient.Config.EncryptionMode = FtpEncryptionMode.None;
                _ftpClient.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                _ftpClient.Credentials = new NetworkCredential(username, password);
                _ftpClient.Connect();
                OnConnected();
                return new KeyValuePair<bool, string>(true, "连接成功!");
            }
            catch (Exception ex) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{ex}");
                OnDisconnected();
                return new KeyValuePair<bool, string>(false, ex.Message);
            }
            finally {
                _connectSlim.Release();
            }
        }

        public List<string>? GetFileList(CancellationToken cancellationToken = default) {
            try {
                return EnumerateDirectory(_ftpClient, _ftpClient.GetWorkingDirectory());
            }
            catch {
                return null;
            }
        }

        public async Task<KeyValuePair<bool, string>> UploadFile(string localFilePath, string remoteFilePath, CancellationToken cancellationToken = default) {
            await Task.Yield();
            try {
                await _semaphore.WaitAsync(cancellationToken);
                if (!_ftpClient.IsConnected) {
                    OnDisconnected();
                    return new KeyValuePair<bool, string>(false, "Ftp未连接!");
                }

                var status = _ftpClient.UploadFile(localFilePath, remoteFilePath, FtpRemoteExists.Overwrite, true);
                return status is FtpStatus.Success or FtpStatus.Skipped
                    ? new KeyValuePair<bool, string>(true, "上传成功")
                    : new KeyValuePair<bool, string>(false, "上传失败");
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _semaphore.Release();
            }
        }

        public async Task<KeyValuePair<bool, string>> DeleteFile(string filePath, CancellationToken cancellationToken = default) {
            await Task.Yield();
            try {
                if (!_ftpClient.IsConnected) {
                    return new KeyValuePair<bool, string>(false, "Ftp未连接!");
                }
                //判断文件是否存在,不存在则返回
                _ftpClient.DeleteFile(filePath);
                return new KeyValuePair<bool, string>(true, "删除成功");
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public async Task<long> GetDirectorySize(string directoryPath) {
            Task.Yield();
            long totalSize = 0;
            foreach (var item in _ftpClient.GetListing(directoryPath)) {
                switch (item.Type) {
                    case FtpObjectType.File:
                        totalSize += item.Size;
                        break;

                    case FtpObjectType.Directory:
                        totalSize += await GetDirectorySize(item.FullName);
                        break;
                }
            }

            return totalSize;
        }

        public async Task<bool> DirectoryExists(string directoryPath) {
            await Task.Yield();
            return _ftpClient.DirectoryExists(directoryPath);
        }

        public async Task<List<FtpFileInfo>?> GetFileInfoList(string directoryPath, CancellationToken cancellationToken = default) {
            await Task.Yield();
            try {
                return EnumerateDirectoryFileInfo(_ftpClient, string.IsNullOrEmpty(directoryPath) ? _ftpClient.GetWorkingDirectory() : directoryPath);
            }
            catch (Exception) {
            }

            return null;
        }

        public async Task<FtpDiskInfo?> GetDiskUsage() {
            await Task.Yield();
            try {
                if (IsConnected) {
                    var ftpDiskInfo = new FtpDiskInfo();

                    // 发送STAT命令以获取当前工作目录的磁盘使用情况
                    var response = _ftpClient.Execute("STAT");

                    if (response.Success && response.Message.StartsWith("211- Disk information:", StringComparison.OrdinalIgnoreCase)) {
                        var diskInfoLine = response.Message;

                        var parts = diskInfoLine.Split('/');
                        if (parts.Length == 2 && long.TryParse(parts[0], out var totalSize) && long.TryParse(parts[1], out var usedSize)) {
                            ftpDiskInfo.TotalSize = totalSize * 1024 * 1024; // 将单位转换为字节
                            ftpDiskInfo.UsedSize = usedSize * 1024 * 1024; // 将单位转换为字节

                            // 计算已使用容量百分比
                            ftpDiskInfo.UsedPercentage = (double)ftpDiskInfo.UsedSize / ftpDiskInfo.TotalSize * 100;
                        }
                    }

                    return ftpDiskInfo;
                }
            }
            catch (Exception) {
                // ignored
            }

            return null;
        }

        private List<string> EnumerateDirectory(IFtpClient client, string directory) {
            // 获取目录中的文件和文件夹列表
            var fileNames = new List<string>();
            var items = client.GetListing(directory);
            // 遍历列表并处理每个项
            foreach (var item in items) {
                if (item.Type == FtpObjectType.Directory) {
                    // 递归遍历子文件夹
                    var list = EnumerateDirectory(client, item.FullName);
                    fileNames.AddRange(list);
                }
                else if (item.Type == FtpObjectType.File) {
                    fileNames.Add(item.FullName);
                }
            }
            return fileNames;
        }

        private List<FtpFileInfo> EnumerateDirectoryFileInfo(IFtpClient client, string directory) {
            // 获取目录中的文件和文件夹列表
            var fileNames = new List<FtpFileInfo>();
            var items = client.GetListing(directory);
            // 遍历列表并处理每个项
            foreach (var item in items) {
                if (item.Type == FtpObjectType.Directory) {
                    // 递归遍历子文件夹
                    var list = EnumerateDirectoryFileInfo(client, item.FullName);
                    fileNames.AddRange(list);
                }
                else if (item.Type == FtpObjectType.File) {
                    fileNames.Add(new FtpFileInfo {
                        FileName = item.Name,
                        CreatedTime = item.Created,
                        FileSize = item.Size,
                        FullPath = item.FullName,
                        LastModifiedTime = item.Modified
                    });
                }
            }
            return fileNames;
        }

        protected virtual async void OnConnected() {
            await Task.Yield();
            Connected?.Invoke(this, EventArgs.Empty);
        }

        protected virtual async void OnDisconnected() {
            await Task.Yield();
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}