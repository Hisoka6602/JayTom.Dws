using System.Drawing;
using NPOI.SS.UserModel;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.LicenseApiClient.Plugin.Excel {

    public interface IExcelService {

        /// <summary>
        /// 读内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="progressPercentage"></param>
        /// <param name="exceptionFunc"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<T>> ReadExcel<T>(string filePath,
            Func<float, Task> progressPercentage,
            Func<Exception, Task> exceptionFunc, CancellationToken token = default) where T : class, new();

        /// <summary>
        /// 读内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="ignoreContent"></param>
        /// <param name="checkCallback"></param>
        /// <param name="progressPercentage"></param>
        /// <param name="exceptionFunc"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<T>> ReadExcel<T>(string filePath,
            List<string> ignoreContent,
            Func<KeyValuePair<bool, string>, List<T>,
                Task<KeyValuePair<bool, List<T>>>> checkCallback,
            Func<float, Task> progressPercentage,
            Func<Exception, Task> exceptionFunc, CancellationToken token = default) where T : class, new();

        /// <summary>
        /// 读内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="ignoreContent"></param>
        /// <param name="cellFunc"></param>
        /// <param name="completionCallback"></param>
        /// <param name="progressPercentage"></param>
        /// <param name="exceptionFunc"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<T>>? ReadExcel<T>(string filePath, List<string> ignoreContent,
            Func<CellInfo, Task> cellFunc,
            Func<List<T>, Task<KeyValuePair<bool, List<CellInfo>>>> completionCallback,
            Func<float, Task> progressPercentage,
            Func<Exception, Task> exceptionFunc, CancellationToken token = default) where T : class, new();

        Task<List<ColumnInfo>> ColumnIsExist(IWorkbook wk, List<string?> columnNames);

        /// <summary>
        /// 导出文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="title"></param>
        /// <param name="sheetName"></param>
        /// <param name="list"></param>
        /// <param name="excludedHeaders"></param>
        /// <param name="progress"></param>
        /// <param name="exception"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        Task<bool> Export<T>([NotNull] string path,
            string title, string sheetName,
            [NotNull] List<T> list,
            List<string> excludedHeaders,
            Action<int> progress,
            Action<Exception> exception,
            CancellationToken cancelToken = default);

        /// <summary>
        /// 导出文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="title"></param>
        /// <param name="sheetName"></param>
        /// <param name="list"></param>
        /// <param name="excludedHeaders"></param>
        /// <param name="progress"></param>
        /// <param name="exception"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, byte[]?>> Export<T>(string title, string sheetName, List<T> list,
            List<string> excludedHeaders, Action<int> progress,
            Action<Exception> exception, CancellationToken cancelToken = default);
    }

    public class ColumnInfo {

        /// <summary>
        /// 是否存在
        /// </summary>
        public bool IsExist { get; set; }

        /// <summary>
        /// 列索引
        /// </summary>
        public int ColumnIndex { get; set; }

        /// <summary>
        /// 行索引
        /// </summary>

        public int RowIndex { get; set; }

        /// <summary>
        /// 列名
        /// </summary>
        public string ColumnName { get; set; }
    }

    public class CellPositionInfo {
        public int Column { get; set; }
        public int Row { get; set; }
        public Color? Color { get; set; }
        public string Comment { get; set; }
        public object CellType { get; set; }
    }

    public class CellInfo : CellPositionInfo {
        public string CellValue { get; set; }
        public string Tag { get; set; }
        public string ColumnName { get; set; }
    }
}