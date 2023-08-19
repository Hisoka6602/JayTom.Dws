using System;
using FluentFTP;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Ftp {

    public class FluentFtpClient : IFtp {
        private readonly FtpClient _ftpClient;

        public FluentFtpClient() {
            _ftpClient = new FtpClient() {
                Encoding = Encoding.UTF8
            };
        }

        public async Task<KeyValuePair<bool, string>> Connect(string server, string username, string password, CancellationToken cancellationToken = default) {
            try {
                await Task.Yield();
                if (_ftpClient.IsConnected) {
                    return new KeyValuePair<bool, string>(true, "连接成功!");
                }
                _ftpClient.Host = server;
                _ftpClient.Config.DataConnectionConnectTimeout = 60 * 1000;
                _ftpClient.Config.SocketKeepAlive = true;
                _ftpClient.Config.EncryptionMode = FtpEncryptionMode.None;
                _ftpClient.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                _ftpClient.Credentials = new NetworkCredential(username, password);
                _ftpClient.Connect();
                return new KeyValuePair<bool, string>(true, "连接成功!");
            }
            catch (Exception ex) {
                return new KeyValuePair<bool, string>(false, ex.Message);
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
                if (!_ftpClient.IsConnected) {
                    return new KeyValuePair<bool, string>(false, "Ftp未连接!");
                }
                var status = _ftpClient.UploadFile(localFilePath, remoteFilePath, FtpRemoteExists.Overwrite, true);
                return status is FtpStatus.Success or FtpStatus.Skipped ? new KeyValuePair<bool, string>(true, "上传成功") : new KeyValuePair<bool, string>(false, "上传失败");
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
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
                return new KeyValuePair<bool, string>(false, e.Message);
            }
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
    }
}