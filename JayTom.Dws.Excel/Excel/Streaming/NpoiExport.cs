using System.ComponentModel;
using System.Reflection;
using JayTom.Dws.Plugin.Excel.Attributes;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.Streaming;

namespace JayTom.Dws.Plugin.Excel;

/// <summary>提供有界内存的历史记录流式导出。</summary>
public partial class NpoiExport
{
    /// <summary>只缓存最近一百行并直接写磁盘；出错或取消时清理临时文件，保留原目标文件。</summary>
    public async Task<bool> ExportStreamingAsync<T>(string path, string title, string sheetName,
        IAsyncEnumerable<T> records, IReadOnlyCollection<string> excludedHeaders,
        Action<int> progress, Action<Exception> exception, CancellationToken cancelToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (!string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("流式导出只支持 xlsx 文件。");
        await _exportSemaphore.WaitAsync(cancelToken);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            TitleStyle = HeaderStyle = ContentStyle = null;
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetCustomAttribute<DisplayNameAttribute>() is not null &&
                    p.GetCustomAttribute<ExcelInfoAttribute>() is not null && !excludedHeaders.Contains(p.Name)).ToArray();
            if (properties.Length == 0) return false;
            using var book = new SXSSFWorkbook(100);
            var sheetNumber = 1;
            var sheet = CreateStreamingSheet(book, sheetName, title, properties);
            var rowNumber = 2;
            var written = 0;
            await foreach (var record in records.WithCancellation(cancelToken))
            {
                cancelToken.ThrowIfCancellationRequested();
                if (rowNumber >= 500002)
                {
                    sheet = CreateStreamingSheet(book, sheetName + (++sheetNumber).ToString(), title, properties);
                    rowNumber = 2;
                }
                var row = sheet.CreateRow(rowNumber++);
                row.Height = 500;
                for (var column = 0; column < properties.Length; column++)
                {
                    var property = properties[column];
                    var value = property.GetValue(record);
                    var attribute = property.GetCustomAttribute<ExcelInfoAttribute>()!;
                    var cell = row.CreateCell(column);
                    if (value is Enum && attribute.IsEnumToInt) cell.SetCellValue(Convert.ToInt32(value));
                    else if (value is bool boolean && attribute.IsBooleanToInt) cell.SetCellValue(boolean ? 1 : 0);
                    else cell.SetCellValue(value?.ToString());
                    cell.CellStyle = CreateContentStyle(book);
                }
                written++;
            }
            if (written == 0) return false;
            cancelToken.ThrowIfCancellationRequested();
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 65536, FileOptions.SequentialScan)) book.Write(stream, true);
            cancelToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, true);
            progress(100);
            return true;
        }
        catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            exception(error);
            return false;
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            finally { _exportSemaphore.Release(); }
        }
    }

    /// <summary>创建固定宽度的表头，避免流式工作簿每行重新计算列宽。</summary>
    private ISheet CreateStreamingSheet(IWorkbook book, string name, string title, PropertyInfo[] properties)
    {
        var sheet = book.CreateSheet(name);
        sheet.DisplayGridlines = false;
        sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, properties.Length - 1));
        var header = sheet.CreateRow(0);
        header.Height = 650;
        var titleCell = header.CreateCell(0);
        titleCell.SetCellValue(title);
        titleCell.CellStyle = CreateHeaderStyle(book);
        var columns = sheet.CreateRow(1);
        columns.Height = 500;
        for (var column = 0; column < properties.Length; column++)
        {
            sheet.SetColumnWidth(column, properties[column].GetCustomAttribute<ExcelInfoAttribute>()!.Width is > 0 and var width ? width : 5000);
            var cell = columns.CreateCell(column);
            cell.SetCellValue(properties[column].GetCustomAttribute<DisplayNameAttribute>()!.DisplayName);
            cell.CellStyle = CreateTitleStyle(book);
        }
        return sheet;
    }
}
