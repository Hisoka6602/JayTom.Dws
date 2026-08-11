using System;
using FluentFTP;
using JayTom.Dws.Abstractions.Integrations.Ftp;
using System.Net;
using System.Linq;
using System.Text;
using FluentFTP.Helpers;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Ftp {

    public class FluentFtpClient : IFtp {
        private readonly AsyncFtpClient _ftpClient;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly SemaphoreSlim _connectSlim = new(1, 1);

        public FluentFtpClient() {
            _ftpClient = new AsyncFtpClient() {
                Encoding = Encoding.UTF8
            };
        }

        public bool IsConnected => _ftpClient.IsConnected;

        public event EventHandler<EventArgs>? Connected;

        public event EventHandler<EventArgs>? Disconnected;

        public async Task<KeyValuePair<bool, string>> Connect(string server, int port, string username, string password, CancellationToken cancellationToken = default) {
            var lockTaken = false;
            try {
                await _connectSlim.WaitAsync(cancellationToken);
                lockTaken = true;
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
                await _ftpClient.Connect(cancellationToken);
                OnConnected();
                return new KeyValuePair<bool, string>(true, "连接成功!");
            }
            catch (Exception ex) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{ex}");
                OnDisconnected();
                return new KeyValuePair<bool, string>(false, ex.Message);
            }
            finally {
                if (lockTaken) {
                    _connectSlim.Release();
                }
            }
        }

        public async Task<List<string>?> GetFileList(CancellationToken cancellationToken = default) {
            try {
                var workingDirectory = await _ftpClient.GetWorkingDirectory(cancellationToken);
                return await EnumerateDirectory(workingDirectory, cancellationToken);
            }
            catch {
                return null;
            }
        }

        public async Task<KeyValuePair<bool, string>> UploadFile(string localFilePath, string remoteFilePath, CancellationToken cancellationToken = default) {
            var lockTaken = false;
            try {
                await _semaphore.WaitAsync(cancellationToken);
                lockTaken = true;
                if (!_ftpClient.IsConnected) {
                    OnDisconnected();
                    return new KeyValuePair<bool, string>(false, "Ftp未连接!");
                }

                var status = await _ftpClient.UploadFile(
                    localFilePath,
                    remoteFilePath,
                    FtpRemoteExists.Overwrite,
                    createRemoteDir: true,
                    token: cancellationToken);
                return status is FtpStatus.Success or FtpStatus.Skipped
                    ? new KeyValuePair<bool, string>(true, "上传成功")
                    : new KeyValuePair<bool, string>(false, "上传失败");
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                if (lockTaken) {
                    _semaphore.Release();
                }
            }
        }

        public async Task<KeyValuePair<bool, string>> DeleteFile(string filePath, CancellationToken cancellationToken = default) {
            try {
                if (!_ftpClient.IsConnected) {
                    return new KeyValuePair<bool, string>(false, "Ftp未连接!");
                }
                //判断文件是否存在,不存在则返回
                await _ftpClient.DeleteFile(filePath, cancellationToken);
                return new KeyValuePair<bool, string>(true, "删除成功");
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public async Task<long> GetDirectorySize(string directoryPath) {
            long totalSize = 0;
            foreach (var item in await _ftpClient.GetListing(directoryPath)) {
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
            return await _ftpClient.DirectoryExists(directoryPath);
        }

        public async Task<List<FtpFileInfo>?> GetFileInfoList(string directoryPath, CancellationToken cancellationToken = default) {
            try {
                var targetDirectory = string.IsNullOrEmpty(directoryPath)
                    ? await _ftpClient.GetWorkingDirectory(cancellationToken)
                    : directoryPath;
                return await EnumerateDirectoryFileInfo(targetDirectory, cancellationToken);
            }
            catch (Exception) {
            }

            return null;
        }

        public async Task<FtpDiskInfo?> GetDiskUsage() {
            try {
                if (IsConnected) {
                    var ftpDiskInfo = new FtpDiskInfo();

                    // 发送STAT命令以获取当前工作目录的磁盘使用情况
                    var response = await _ftpClient.Execute("STAT", CancellationToken.None);

                    if (response.Success && response.Message.StartsWith("211- Disk information:", StringComparison.OrdinalIgnoreCase)) {
                        var diskInfoLine = response.Message;

                        var parts = diskInfoLine.Split('/');
                        if (parts.Length == 2 && long.TryParse(parts[0], out var totalSize) && long.TryParse(parts[1], out var usedSize)) {
                            ftpDiskInfo.TotalSize = totalSize * 1024 * 1024; // 将单位转换为字节
                            ftpDiskInfo.UsedSize = usedSize * 1024 * 1024; // 将单位转换为字节

                            // 计算已使用容量百分比
                            ftpDiskInfo.UsedPercentage = (decimal)ftpDiskInfo.UsedSize / ftpDiskInfo.TotalSize * 100m;
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

        private async Task<List<string>> EnumerateDirectory(
            string directory,
            CancellationToken cancellationToken) {
            var fileNames = new List<string>();
            var items = await _ftpClient.GetListing(directory, cancellationToken);
            foreach (var item in items) {
                if (item.Type == FtpObjectType.Directory) {
                    var list = await EnumerateDirectory(item.FullName, cancellationToken);
                    fileNames.AddRange(list);
                }
                else if (item.Type == FtpObjectType.File) {
                    fileNames.Add(item.FullName);
                }
            }
            return fileNames;
        }

        private async Task<List<FtpFileInfo>> EnumerateDirectoryFileInfo(
            string directory,
            CancellationToken cancellationToken) {
            var fileNames = new List<FtpFileInfo>();
            var items = await _ftpClient.GetListing(directory, cancellationToken);
            foreach (var item in items) {
                if (item.Type == FtpObjectType.Directory) {
                    var list = await EnumerateDirectoryFileInfo(item.FullName, cancellationToken);
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

        protected virtual void OnConnected() {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDisconnected() {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
