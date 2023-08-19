using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Ftp {

    public interface IFtp {

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="server"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> Connect(string server, string username, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取文件列表
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        List<string>? GetFileList(CancellationToken cancellationToken = default);

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

        //连接状态
        //目录是否存在
        //添加目录
        //删除目录
        //设置当前工作目录
    }
}