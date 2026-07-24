using System;
using FluentFTP;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Ftp {

    public interface IFtp {

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接事件
        /// </summary>
        event EventHandler<EventArgs> Connected;

        /// <summary>
        /// 断开连接事件
        /// </summary>
        event EventHandler<EventArgs> Disconnected;

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="server"></param>
        /// <param name="port"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> Connect(string server, int port, string username, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取文件列表
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<string>?> GetFileList(CancellationToken cancellationToken = default);

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <param name="remoteFilePath"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> UploadFile(string localFilePath, string remoteFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> DeleteFile(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取目录下文件大小
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        Task<long> GetDirectorySize(string directoryPath);

        /// <summary>
        /// 目录是否存在
        /// </summary>
        /// <returns></returns>
        Task<bool> DirectoryExists(string directoryPath);

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<FtpFileInfo>?> GetFileInfoList(string directoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取磁盘容量信息
        /// </summary>
        /// <returns></returns>
        Task<FtpDiskInfo?> GetDiskUsage();

        //连接状态

        //目录是否存在
        //添加目录
        //删除目录
        //设置当前工作目录
    }

    public class FtpFileInfo {

        /// <summary>
        /// 获取或设置文件名。
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置文件大小。
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 获取或设置文件的创建时间。
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 获取或设置文件的最后修改时间。
        /// </summary>
        public DateTime LastModifiedTime { get; set; }

        /// <summary>
        /// 获取或设置文件的完整路径名。
        /// </summary>
        public string FullPath { get; set; } = string.Empty;
    }

    public class FtpDiskInfo {
        public long TotalSize { get; set; } // 总容量
        public long UsedSize { get; set; } // 已使用容量
        public double UsedPercentage { get; set; } // 已使用容量百分比
    }
}
