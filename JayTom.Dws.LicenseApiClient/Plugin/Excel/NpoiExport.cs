using NPOI.SS.Util;
using System.Drawing;
using NPOI.HSSF.Util;
using System.Reflection;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using JayTom.Dws.LicenseApiClient.Plugin.Excel.Attributes;

namespace JayTom.Dws.LicenseApiClient.Plugin.Excel {

    public class NpoiExport : IExcelService {
        private ICellStyle? TitleStyle { get; set; } = null;
        private ICellStyle? HeaderStyle { get; set; } = null;
        private ICellStyle? ContentStyle { get; set; } = null;

        public async Task<List<T>> ReadExcel<T>(string filePath, Func<float, Task> progressPercentage, Func<Exception, Task> exceptionFunc, CancellationToken token = default) where T : class, new() {
            var cellInfos = new List<CellInfo>();
            var keyCellInfos = new List<CellInfo>();
            var propertyInfos = typeof(T).GetProperties(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            //读数据+检查
            return await ReadExcel<T>(filePath, new List<string>(), cellFunc => {
                //效验公式问题
                if (cellFunc.CellType is CellType type) {
                    if (type == CellType.Error) {
                        cellInfos.Add(new CellInfo() {
                            Row = cellFunc.Row,
                            ColumnName = cellFunc.ColumnName,
                            CellType = type,
                            CellValue = cellFunc.CellValue,
                            Color = Color.Red,
                            Column = cellFunc.Column,
                            Comment = $"[{cellFunc.ColumnName}]不能包含错误内容!"
                        });
                    }
                    else if (type == CellType.Formula) {
                        cellInfos.Add(new CellInfo() {
                            Row = cellFunc.Row,
                            ColumnName = cellFunc.ColumnName,
                            CellType = type,
                            CellValue = cellFunc.CellValue,
                            Color = Color.Red,
                            Column = cellFunc.Column,
                            Comment = $"[{cellFunc.ColumnName}]不能包含错误内容!"
                        });
                    }
                }
                var info = propertyInfos.FirstOrDefault(f => (bool)(f?.GetCustomAttribute<DisplayNameAttribute>()
                    ?.DisplayName?.Equals(cellFunc.ColumnName) ?? false));
                //非空检查
                var requiredAttribute = info?.GetCustomAttribute<RequiredAttribute>();
                if (requiredAttribute is not null) {
                    if (string.IsNullOrEmpty(cellFunc.CellValue)) {
                        cellInfos.Add(new CellInfo() {
                            Row = cellFunc.Row,
                            ColumnName = cellFunc.ColumnName,
                            CellValue = cellFunc.CellValue,
                            Color = Color.Red,
                            Column = cellFunc.Column,
                            Comment = $"[{cellFunc.ColumnName}]不能为空!"
                        });
                    }
                }
                //正则检查
                var expressionAttribute = info?.GetCustomAttribute<RegularExpressionAttribute>();
                if (expressionAttribute is not null) {
                    try {
                        var isMatch = Regex.IsMatch(cellFunc.CellValue ?? string.Empty, expressionAttribute.Pattern);
                        if (!isMatch) {
                            cellInfos.Add(new CellInfo() {
                                Row = cellFunc.Row,
                                ColumnName = cellFunc.ColumnName,
                                CellValue = cellFunc.CellValue,
                                Color = Color.Red,
                                Column = cellFunc.Column,
                                Comment = $"[{cellFunc.ColumnName}]{expressionAttribute.ErrorMessage}!"
                            });
                        }
                    }
                    catch (Exception e) {
                        Console.WriteLine(e);
                    }
                }
                //Key
                var keyAttribute = info?.GetCustomAttribute<KeyAttribute>();
                if (keyAttribute is not null) {
                    keyCellInfos.Add(new CellInfo() {
                        Row = cellFunc.Row,
                        ColumnName = cellFunc.ColumnName,
                        CellValue = cellFunc.CellValue,
                        Column = cellFunc.Column,
                    });
                }

                return Task.CompletedTask;
                //keyCellInfos
            }, async completionCallback => {
                //检查key
                var duplicates = keyCellInfos.GroupBy(
                        x => new { x.Column, x.ColumnName, x.CellValue },
                        (key, group) => {
                            var group1 = group.ToList();
                            return new { Key = key, Count = group1.Count(), Rows = group1.Select(x => x.Row) };
                        })
                    .Where(x => x.Count > 1);
                cellInfos.AddRange(from duplicate in duplicates
                                   from row in duplicate.Rows
                                   select new CellInfo() {
                                       Row = row,
                                       ColumnName = duplicate.Key.ColumnName,
                                       CellValue = duplicate.Key.CellValue,
                                       Color = Color.Red,
                                       Column = duplicate.Key.Column,
                                       Comment = $"[{string.Join(", ", duplicate.Key.ColumnName)}]只能是唯一值,[{string.Join(", ", duplicate.Rows)}]已重复!"
                                   });
                //输出检查
                if (cellInfos?.Any() == true) {
                    await exceptionFunc(new Exception("检查到错误项,请查看源文件!"));

                    return new KeyValuePair<bool, List<CellInfo>>(false, cellInfos);
                }

                return new KeyValuePair<bool, List<CellInfo>>(true, cellInfos);
            }, progressPercentage, exceptionFunc, token);
        }

        public async Task<List<T>> ReadExcel<T>(string filePath, List<string> ignoreContent, Func<KeyValuePair<bool, string>, List<T>,
            Task<KeyValuePair<bool, List<T>>>> checkCallback,
            Func<float, Task> progressPercentage, Func<Exception, Task> exceptionFunc, CancellationToken token = default) where T : class, new() {
            var allCellInfos = new List<CellInfo>();
            var cellInfos = new List<CellInfo>();
            var keyCellInfos = new List<CellInfo>();
            var propertyInfos = typeof(T).GetProperties(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            //读数据+检查
            return await ReadExcel<T>(filePath, ignoreContent, cellFunc => {
                allCellInfos.Add(cellFunc);
                //效验公式问题
                if (cellFunc.CellType is CellType type) {
                    if (type == CellType.Error) {
                        cellInfos.Add(new CellInfo() {
                            Row = cellFunc.Row,
                            ColumnName = cellFunc.ColumnName,
                            CellType = type,
                            CellValue = cellFunc.CellValue,
                            Color = Color.Red,
                            Column = cellFunc.Column,
                            Comment = $"[{cellFunc.ColumnName}]不能包含错误内容!"
                        });
                    }
                    else if (type == CellType.Formula) {
                        cellInfos.Add(new CellInfo() {
                            Row = cellFunc.Row,
                            ColumnName = cellFunc.ColumnName,
                            CellType = type,
                            CellValue = cellFunc.CellValue,
                            Color = Color.Red,
                            Column = cellFunc.Column,
                            Comment = $"[{cellFunc.ColumnName}]不能包含错误内容!"
                        });
                    }
                }
                var info = propertyInfos.FirstOrDefault(f => (bool)f?.GetCustomAttribute<DisplayNameAttribute>()
                    ?.DisplayName?.Equals(cellFunc.ColumnName));
                //非空检查
                var requiredAttribute = info?.GetCustomAttribute<RequiredAttribute>();
                if (requiredAttribute is not null) {
                    if (string.IsNullOrEmpty(cellFunc.CellValue)) {
                        cellInfos.Add(new CellInfo() {
                            Row = cellFunc.Row,
                            ColumnName = cellFunc.ColumnName,
                            CellValue = cellFunc.CellValue,
                            Color = Color.Red,
                            Column = cellFunc.Column,
                            Comment = $"[{cellFunc.ColumnName}]不能为空!"
                        });
                    }
                }
                //正则检查
                var expressionAttribute = info?.GetCustomAttribute<RegularExpressionAttribute>();
                if (expressionAttribute is not null) {
                    try {
                        var isMatch = Regex.IsMatch(cellFunc.CellValue ?? string.Empty, expressionAttribute.Pattern);
                        if (!isMatch) {
                            cellInfos.Add(new CellInfo() {
                                Row = cellFunc.Row,
                                ColumnName = cellFunc.ColumnName,
                                CellValue = cellFunc.CellValue,
                                Color = Color.Red,
                                Column = cellFunc.Column,
                                Comment = $"[{cellFunc.ColumnName}]{expressionAttribute.ErrorMessage}!"
                            });
                        }
                    }
                    catch (Exception e) {
                        Console.WriteLine(e);
                    }
                }
                //Key
                var keyAttribute = info?.GetCustomAttribute<KeyAttribute>();
                if (keyAttribute is not null) {
                    keyCellInfos.Add(new CellInfo() {
                        Row = cellFunc.Row,
                        ColumnName = cellFunc.ColumnName,
                        CellValue = cellFunc.CellValue,
                        Column = cellFunc.Column,
                    });
                }

                return Task.CompletedTask;
                //keyCellInfos
            }, async completionCallback => {
                //检查key
                var duplicates = keyCellInfos.GroupBy(
                        x => new { x.Column, x.ColumnName, x.CellValue },
                        (key, group) => {
                            var group1 = group.ToList();
                            return new { Key = key, Count = group1.Count(), Rows = group1.Select(x => x.Row) };
                        })
                    .Where(x => x.Count > 1);
                cellInfos.AddRange(from duplicate in duplicates
                                   from row in duplicate.Rows
                                   select new CellInfo() {
                                       Row = row,
                                       ColumnName = duplicate.Key.ColumnName,
                                       CellValue = duplicate.Key.CellValue,
                                       Color = Color.Red,
                                       Column = duplicate.Key.Column,
                                       Comment = $"[{string.Join(", ", duplicate.Key.ColumnName)}]只能是唯一值,[{string.Join(", ", duplicate.Rows)}]已重复!"
                                   });
                //输出检查
                if (cellInfos?.Any() == true) {
                    await checkCallback(new KeyValuePair<bool, string>(false, "检查到错误项,请查看源文件!"), new List<T>());
                    return new KeyValuePair<bool, List<CellInfo>>(false, cellInfos);
                }
                //绑定检查
                var (b, value) = await checkCallback(new KeyValuePair<bool, string>(true, string.Empty), completionCallback);
                if (!b) {
                    foreach (var cellInfo in from item in value
                                             let itemType = item.GetType()
                                             let properties = itemType.GetProperties()
                                             from property in properties
                                             where property.GetValue(item) is not null
                                             select allCellInfos.FirstOrDefault(f =>
                                 f.CellValue.Equals(property.GetValue(item)?.ToString()) &&
                                 !f.Color.Equals(Color.Red)) into cellInfo
                                             where cellInfo is not null
                                             select cellInfo) {
                        cellInfo.Color = Color.Red;
                        cellInfos.Add(cellInfo);
                    }

                    if (cellInfos?.Any() == true) {
                        await checkCallback(new KeyValuePair<bool, string>(false, "检查到错误项,请查看源文件!"), new List<T>());
                        return new KeyValuePair<bool, List<CellInfo>>(false, cellInfos);
                    }
                }
                return new KeyValuePair<bool, List<CellInfo>>(true, cellInfos);
            }, progressPercentage, async exception => {
                await checkCallback(new KeyValuePair<bool, string>(false, exception.Message), new List<T>());
                await exceptionFunc(exception);
            }, token);
        }

        public async Task<List<T>>? ReadExcel<T>(string filePath, List<string> ignoreContent, Func<CellInfo, Task> cellFunc, Func<List<T>, Task<KeyValuePair<bool, List<CellInfo>>>> completionCallback, Func<float, Task> progressPercentage,
            Func<Exception, Task> exceptionFunc, CancellationToken token = default) where T : class, new() {
            if (!string.IsNullOrEmpty(filePath)) {
                //反射获取类型
                var propertyInfos = typeof(T).GetProperties(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                //判断列
                var infos = propertyInfos.Where(w =>
                    w.GetCustomAttribute<DisplayNameAttribute>() != null &&
                    w.GetCustomAttribute<MemberNotNullAttribute>() != null)?.ToList();
                //获取对应列名
                var list = infos?.Select(s => s?.GetCustomAttribute<DisplayNameAttribute>()
                    ?.DisplayName)?.ToList();
                if (list?.Any() == true && !token.IsCancellationRequested) {
                    try {
                        //读 IWorkbook
                        var extension = System.IO.Path.GetExtension(filePath);
                        IWorkbook wk;
                        await using (var fs = File.OpenRead(filePath)) {
                            if (extension.Equals(".xls")) {
                                //把xls文件中的数据写入wk中
                                wk = new HSSFWorkbook(fs);
                            }
                            else {
                                //把xlsx文件中的数据写入wk中
                                wk = new XSSFWorkbook(fs);
                            }
                        }
                        //判断列是否都存在
                        var columnIsExist = await ColumnIsExist(wk, list);
                        if (columnIsExist?.Any() == true && !token.IsCancellationRequested) {
                            if (columnIsExist?.All(a => a.IsExist) == true) {
                                if (columnIsExist?.GroupBy(g => g.RowIndex)?.Count() == 1) {
                                    var key = columnIsExist?.GroupBy(g => g.RowIndex)?.FirstOrDefault()?.Key ?? 0;
                                    //读数据
                                    var outList = new List<T>();
                                    //读取当前表数据
                                    var sheet = wk.GetSheetAt(0);
                                    var maxCellCount = sheet.LastRowNum * columnIsExist?.Count;
                                    var progress = 0;
                                    for (var i = key + 1; i <= sheet.LastRowNum; i++) {
                                        var row = sheet.GetRow(i);  //读取当前行数据
                                        if (row == null || token.IsCancellationRequested) continue;
                                        var item = new T();
                                        foreach (var columnInfo in columnIsExist) {
                                            var cell = row.GetCell(columnInfo.ColumnIndex);
                                            var value = cell?.ToString();

                                            var info = propertyInfos.FirstOrDefault(f => f?.GetCustomAttribute<DisplayNameAttribute>()
                                                ?.DisplayName?.Equals(columnInfo.ColumnName) ?? false);

                                            if (info is null || token.IsCancellationRequested) continue;
                                            //替换内容
                                            value = ignoreContent.Aggregate(value, (current, s) => current?.Replace(s, string.Empty));
                                            var type = info.PropertyType.Name;
                                            //判断有没有转换
                                            var infoAttribute = info.GetCustomAttribute<ExcelInfoAttribute>();
                                            if (infoAttribute is not null &&
                                                infoAttribute.IsEnumToInt) {
                                                type = "Int32";
                                            }
                                            else if (infoAttribute is not null &&
                                                     infoAttribute.IsBooleanToInt) {
                                                value = value?.Equals("1") == true ? "true" : "false";
                                                type = "Boolean";
                                            }

                                            switch (type) {
                                                case "Guid":
                                                    info.SetValue(item, new Guid(value ?? string.Empty), null);
                                                    break;

                                                case "Int32":
                                                    info.SetValue(item, int.TryParse(value, out var intResult) ? intResult : 0, null);
                                                    break;

                                                case "Decimal":
                                                    info.SetValue(item, decimal.TryParse(value, out var decimalResult) ? decimalResult : 0, null);
                                                    break;

                                                case "DateTime":
                                                    info.SetValue(item, DateTime.TryParse(value, out var dateTimeResult) ? dateTimeResult : DateTime.MinValue, null);
                                                    break;

                                                case "Double":
                                                    info.SetValue(item, double.TryParse(value, out var doubleResult) ? doubleResult : 0, null);
                                                    break;

                                                case "String":
                                                    info.SetValue(item, value ?? string.Empty, null);
                                                    break;

                                                case "Boolean":
                                                    info.SetValue(item, bool.TryParse(value, out var boolResult) ? boolResult : false, null);
                                                    break;

                                                default:
                                                    info.SetValue(item, DBNull.Value, null);
                                                    break;
                                            }

                                            progress++;
                                            await progressPercentage((float)Math.Round(progress / (decimal)(maxCellCount ?? 0) * 100, 2));
                                            await cellFunc(new CellInfo() {
                                                CellValue = value,
                                                Column = columnInfo.ColumnIndex,
                                                ColumnName = columnInfo.ColumnName,
                                                Row = i,
                                                CellType = cell?.CellType
                                            });
                                        }
                                        outList.Add(item);
                                    }
                                    var callback = await completionCallback(outList);
                                    if (callback.Key) {
                                        await progressPercentage(100);
                                        return outList;
                                    }
                                    else {
                                        foreach (var cellInfo in callback.Value) {
                                            var cell = sheet.GetRow(cellInfo.Row)?.GetCell(cellInfo.Column);
                                            if (cell is not null && !token.IsCancellationRequested) {
                                                //标记颜色
                                                if (cellInfo.Color != null) {
                                                    cell.CellStyle = SetCellStyleColor(wk, cellInfo.Color.Value);
                                                }

                                                if (!cellInfo.Comment.Equals(string.Empty)) {
                                                    cell.CellComment = SetComment(sheet, cell.CellComment, cell.ColumnIndex,
                                                        cell.RowIndex, cellInfo.Comment);
                                                }
                                            }
                                        }
                                        var startNew = await Task.Factory.StartNew(async () => {
                                            //保存文件
                                            await using var ms = new MemoryStream();
                                            wk?.Write(ms, true);
                                            await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                                            var d = ms.ToArray();
                                            await fs.WriteAsync(d, 0, d.Length, token);
                                            await fs.FlushAsync(token);
                                        }, token);
                                        Task.WaitAll(new[] { startNew }, token);
                                    }
                                }
                                else {
                                    await exceptionFunc(new Exception(" 所需要的列标题不在同一行!"));
                                }
                            }
                            else {
                                await exceptionFunc(new Exception($"{string.Join(",", columnIsExist?.Where(w => !w.IsExist)?.Select(s => $"[{s.ColumnName}]")?.ToList())}列不存在!"));
                            }
                        }
                        else {
                            await exceptionFunc(new Exception("未找到任何列"));
                        }
                    }
                    catch (Exception e) {
                        await exceptionFunc(e);
                    }
                }
            }
            return null;
        }

        public Task<List<ColumnInfo>> ColumnIsExist(IWorkbook wk, List<string?> columnNames) {
            if (columnNames?.Any() == true) {
                var columnInfos = columnNames?.Select(s => new ColumnInfo {
                    ColumnName = s,
                    ColumnIndex = 0,
                    IsExist = false,
                    RowIndex = 0
                })?.ToList();
                try {
                    //读取当前表数据
                    var sheet = wk.GetSheetAt(0);
                    //IRow row;  //读取当前行数据
                    //LastRowNum 是当前表的总行数-1（注意）
                    for (var i = 0; i <= sheet.LastRowNum; i++) {
                        var row = sheet.GetRow(i);  //读取当前行数据
                        if (row == null) continue;
                        //LastCellNum 是当前行的总列数
                        for (var j = 0; j < row.LastCellNum; j++) {
                            //读取该行的第j列数据
                            var value = row.GetCell(j)?.ToString();
                            var firstOrDefault = columnInfos?.FirstOrDefault(f => f.ColumnName.Equals(value) && f.IsExist == false);
                            if (firstOrDefault is not null) {
                                firstOrDefault.IsExist = true;
                                firstOrDefault.RowIndex = i;
                                firstOrDefault.ColumnIndex = j;
                            }
                            if (columnInfos?.All(a => a.IsExist) == true) {
                                return Task.FromResult(columnInfos);
                            }
                        }
                    }
                }
                catch {
                    //
                    Console.WriteLine("异常");
                }
                return Task.FromResult(columnInfos ?? new List<ColumnInfo>());
            }
            return Task.FromResult(new List<ColumnInfo>());
        }

        public async Task<bool> Export<T>(string path, string title, string sheetName, List<T> list, List<string> excludedHeaders, Action<int> progress,
            Action<Exception> exception, CancellationToken cancelToken = default) {
            if (string.IsNullOrWhiteSpace(path) ||
                list.Any() != true) {
                return false;
            }
            await Task.Yield();
            try {
                //判断行/分文件
                var maxPageSize = 50 * 10000;
                var pageSize = list.Count / maxPageSize;
                pageSize += list.Count % maxPageSize > 0 ? 1 : 0;
                IWorkbook book = null;
                for (var pageIndex = 0; pageIndex < pageSize; pageIndex++) {
                    TitleStyle = null;
                    HeaderStyle = null;
                    ContentStyle = null;
                    //写Excel文件
                    //HSSFWorkbook book = new HSSFWorkbook();

                    if (path?.IndexOf(".xlsx") > 0) // 2007版本
                        book = new XSSFWorkbook();
                    else if (path?.IndexOf(".xls") > 0) // 2003版本
                        book = new HSSFWorkbook();

                    var sheet = book?.CreateSheet(sheetName);//页文件
                    if (sheet is not null) {
                        sheet.DisplayGridlines = false;
                        sheet.IsPrintGridlines = false;
                    }
                    //取出列名和列长度
                    var propertyInfos = typeof(T).GetProperties(
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    //判断列
                    var infos = propertyInfos.Where(w =>
                        w.GetCustomAttribute<DisplayNameAttribute>() != null &&
                        w.GetCustomAttribute<ExcelInfoAttribute>() != null)?.ToList();
                    if (infos?.Any() != true) {
                        return false;
                    }

                    /*if (excludedHeaders?.Any(a => a.Equals(infos[j].Name)) == true) {
                        continue;
                    }*/
                    //判断排除列
                    infos.RemoveAll(r => (bool)excludedHeaders?.Contains(r.Name));
                    //创建Excel 头，合并单元格10列(计算)
                    sheet?.AddMergedRegion(new CellRangeAddress(0, 0, 0, infos.Count - 1));
                    var rowHeader = sheet?.CreateRow(0);
                    //在行中：建立单元格，参数为列号，从0计
                    var cellHeader = rowHeader?.CreateCell(0);
                    //设置单元格内容
                    cellHeader?.SetCellValue(title);
                    if (cellHeader != null) cellHeader.CellStyle = CreateHeaderStyle(book);
                    if (rowHeader != null) {
                        rowHeader.Height = 650;
                        rowHeader.RowStyle = CreateHeaderStyle(book);
                    }

                    //设置列
                    var rowTitle = sheet?.CreateRow(1);
                    if (rowTitle != null) rowTitle.Height = 500;
                    for (var i = 0; i < infos.Count; i++) {
                        if (infos[i].GetCustomAttribute<ExcelInfoAttribute>()?.Width == 0) {
                            sheet?.AutoSizeColumn(i);
                        }
                        else {
                            sheet?.SetColumnWidth(i, infos[i].GetCustomAttribute<ExcelInfoAttribute>()?.Width ?? 0);
                        }
                        if (rowTitle == null) continue;
                        var cell = rowTitle.CreateCell(i);
                        cell.SetCellValue(infos[i].GetCustomAttribute<DisplayNameAttribute>()?.DisplayName);
                        cell.CellStyle = CreateTitleStyle(book);
                    }

                    //设置内容
                    for (var i = pageIndex * maxPageSize; i < (list.Count <= (pageIndex + 1) * maxPageSize ? list.Count : (pageIndex + 1) * maxPageSize); i++) {
                        //行
                        var type = list[i]?.GetType();
                        propertyInfos = type?.GetProperties();
                        var rowContent = sheet?.CreateRow((i - pageIndex * maxPageSize) + 2);
                        if (rowContent != null) {
                            rowContent.Height = 500;

                            for (var j = 0; j < infos.Count; j++) {
                                var firstOrDefault = propertyInfos?.FirstOrDefault(f => f.Name.Equals(infos[j].Name));
                                if (firstOrDefault == null) continue;
                                var value = firstOrDefault.GetValue(list[i]);
                                var cell = rowContent.CreateCell(j);
                                if (cell == null) continue;
                                if (value is Enum &&
                                    infos[j].GetCustomAttribute<ExcelInfoAttribute>()?.IsEnumToInt == true) {
                                    cell.SetCellValue((int)value);
                                }
                                else if (value is bool boolValue &&
                                         infos[j].GetCustomAttribute<ExcelInfoAttribute>()?.IsBooleanToInt == true) {
                                    cell.SetCellValue(boolValue ? 1 : 0);
                                }
                                else {
                                    cell.SetCellValue(value?.ToString());
                                }
                                cell.CellStyle = CreateContentStyle(book);
                                if (infos[j].GetCustomAttribute<ExcelInfoAttribute>()?.Width == 0) {
                                    sheet?.AutoSizeColumn(j);
                                }
                            }
                        }

                        //写出进度
                        await Task.Factory.StartNew(() => {
                            //进度+1(只在10%、20%、30%、50%、60%、80%、100%时写出)
                            var listCount = ((decimal)i + 1) / list.Count * 100;
                            if ((int)listCount == 100) {
                                listCount = 99;
                            }
                            progress.Invoke((int)listCount);
                        }, cancelToken);
                    }
                    // 写入到客户端操作
                    var startNew = await Task.Factory.StartNew(async () => {
                        await using var ms = new MemoryStream();
                        book?.Write(ms, false);
                        if (path != null) {
                            var fileInfo = new FileInfo(path);
                            var fileName = path;
                            if (pageIndex > 0) {
                                fileName = $"{fileInfo.FullName.Replace(fileInfo.Extension, $"(part{pageIndex + 1})")}{fileInfo.Extension}";
                            }
                            await using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                            var d = ms.ToArray();
                            await fs.WriteAsync(d, cancelToken);
                            await fs.FlushAsync(cancelToken);
                        }

                        if (pageIndex == pageSize - 1) {
                            progress.Invoke(100);
                        }
                    }, cancelToken);
                    Task.WaitAll(new[] { startNew }, cancelToken);
                }

                return true;
            }
            catch (Exception e) {
                exception.Invoke(e);
                return false;
            }
        }

        public async Task<KeyValuePair<bool, byte[]?>> Export<T>(string title, string sheetName, List<T> list, List<string> excludedHeaders, Action<int> progress,
          Action<Exception> exception, CancellationToken cancelToken = default) {
            if (
                list.Any() != true) {
                return new KeyValuePair<bool, byte[]?>(false, null);
            }
            await Task.Yield();
            try {
                IWorkbook book = null;
                TitleStyle = null;
                HeaderStyle = null;
                ContentStyle = null;
                //写Excel文件
                //HSSFWorkbook book = new HSSFWorkbook();
                book = new XSSFWorkbook();
                var sheet = book?.CreateSheet(sheetName);//页文件
                if (sheet is not null) {
                    sheet.DisplayGridlines = false;
                    sheet.IsPrintGridlines = false;
                }
                //取出列名和列长度
                var propertyInfos = typeof(T).GetProperties(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                //判断列
                var infos = propertyInfos.Where(w =>
                    w.GetCustomAttribute<DisplayNameAttribute>() != null &&
                    w.GetCustomAttribute<ExcelInfoAttribute>() != null)?.ToList();
                if (infos?.Any() != true) {
                    return new KeyValuePair<bool, byte[]?>(false, null);
                }

                /*if (excludedHeaders?.Any(a => a.Equals(infos[j].Name)) == true) {
                    continue;
                }*/
                //判断排除列
                infos.RemoveAll(r => (bool)excludedHeaders?.Contains(r.Name));
                //创建Excel 头，合并单元格10列(计算)
                sheet?.AddMergedRegion(new CellRangeAddress(0, 0, 0, infos.Count - 1));
                var rowHeader = sheet?.CreateRow(0);
                //在行中：建立单元格，参数为列号，从0计
                var cellHeader = rowHeader?.CreateCell(0);
                //设置单元格内容
                cellHeader?.SetCellValue(title);
                if (cellHeader != null) cellHeader.CellStyle = CreateHeaderStyle(book);
                if (rowHeader != null) {
                    rowHeader.Height = 650;
                    rowHeader.RowStyle = CreateHeaderStyle(book);
                }

                //设置列
                var rowTitle = sheet?.CreateRow(1);
                if (rowTitle != null) rowTitle.Height = 500;
                for (var i = 0; i < infos.Count; i++) {
                    if (infos[i].GetCustomAttribute<ExcelInfoAttribute>()?.Width == 0) {
                        sheet?.AutoSizeColumn(i);
                    }
                    else {
                        sheet?.SetColumnWidth(i, infos[i].GetCustomAttribute<ExcelInfoAttribute>()?.Width ?? 0);
                    }
                    if (rowTitle == null) continue;
                    var cell = rowTitle.CreateCell(i);
                    cell.SetCellValue(infos[i].GetCustomAttribute<DisplayNameAttribute>()?.DisplayName);
                    cell.CellStyle = CreateTitleStyle(book);
                }
                //设置内容
                for (var i = 0; i < list.Count; i++) {
                    //行
                    var type = list[i]?.GetType();
                    propertyInfos = type?.GetProperties();
                    var rowContent = sheet?.CreateRow(i + 2);
                    if (rowContent != null) {
                        rowContent.Height = 500;

                        for (var j = 0; j < infos.Count; j++) {
                            var firstOrDefault = propertyInfos?.FirstOrDefault(f => f.Name.Equals(infos[j].Name));
                            if (firstOrDefault == null) continue;
                            var value = firstOrDefault.GetValue(list[i]);
                            var cell = rowContent.CreateCell(j);
                            if (cell == null) continue;
                            if (value is Enum &&
                                infos[j].GetCustomAttribute<ExcelInfoAttribute>()?.IsEnumToInt == true) {
                                cell.SetCellValue((int)value);
                            }
                            else if (value is bool boolValue &&
                                     infos[j].GetCustomAttribute<ExcelInfoAttribute>()?.IsBooleanToInt == true) {
                                cell.SetCellValue(boolValue ? 1 : 0);
                            }
                            else {
                                cell.SetCellValue(value?.ToString());
                            }
                            cell.CellStyle = CreateContentStyle(book);
                            if (infos[j].GetCustomAttribute<ExcelInfoAttribute>()?.Width == 0) {
                                sheet?.AutoSizeColumn(j);
                            }
                        }
                    }

                    //写出进度
                    await Task.Factory.StartNew(() => {
                        //进度+1(只在10%、20%、30%、50%、60%、80%、100%时写出)
                        var listCount = ((decimal)i + 1) / list.Count * 100;
                        if ((int)listCount == 100) {
                            listCount = 99;
                        }
                        progress.Invoke((int)listCount);
                    }, cancelToken);
                }
                // 写出
                await using var ms = new MemoryStream();
                book?.Write(ms, false);

                progress.Invoke(100);
                return new KeyValuePair<bool, byte[]?>(true, ms.ToArray());
            }
            catch (Exception e) {
                exception.Invoke(e);
                return new KeyValuePair<bool, byte[]?>(false, null);
            }
        }

        public ICellStyle SetCellStyleColor(IWorkbook book, Color color) {
            var cellStyle = book.CreateCellStyle();
            cellStyle.FillForegroundColor = 0;
            cellStyle.FillPattern = FillPattern.SolidForeground;

            ((XSSFColor)cellStyle.FillForegroundColorColor).SetRgb(new[] { color.R, color.G, color.B });//设置单元格背景色
            return cellStyle;
        }

        public ICellStyle ClearCellStyleColor(IWorkbook book) {
            var cellStyle = book.CreateCellStyle();
            cellStyle.FillForegroundColor = 0;
            cellStyle.FillPattern = FillPattern.NoFill;
            return cellStyle;
        }

        public IComment SetComment(ISheet sheet, IComment comment, int column, int row, string text) {
            try {
                var book = sheet.Workbook;
                IRichTextString richTextString = null;
                if (comment is null) {
                    var draw = sheet.CreateDrawingPatriarch();
                    IClientAnchor clientAnchor = null;
                    if (book is HSSFWorkbook) {
                        clientAnchor = new HSSFClientAnchor(0, 0, 0, 0, column, row, column + 2, row + 5);
                        comment = draw.CreateCellComment(clientAnchor);
                        richTextString = new HSSFRichTextString($"{text}");
                    }
                    else if (book is XSSFWorkbook) {
                        clientAnchor = new XSSFClientAnchor(0, 0, 0, 0, column, row, column + 2, row + 5);
                        comment = draw.CreateCellComment(clientAnchor);
                        richTextString = new XSSFRichTextString($"{text}");
                    }
                }

                if (comment is not null) {
                    comment.String = richTextString;
                    comment.Visible = false;
                    comment.Author = "Hisoka";
                }

                return comment;
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            return null;
        }

        public ICellStyle CreateHeaderStyle(IWorkbook book) {
            if (HeaderStyle is not null) return HeaderStyle;
            HeaderStyle = book.CreateCellStyle();
            //设置单元格的样式：水平对齐居中
            HeaderStyle.Alignment = HorizontalAlignment.Center;
            HeaderStyle.VerticalAlignment = VerticalAlignment.Center;
            var font = book.CreateFont();
            //设置字体加粗样式
            font.IsBold = true;
            font.FontHeightInPoints = 20;
            font.FontName = "微软雅黑";
            font.Color = HSSFColor.Grey80Percent.Index;
            //使用SetFont方法将字体样式添加到单元格样式中
            HeaderStyle.SetFont(font);
            return HeaderStyle;
        }

        public ICellStyle CreateTitleStyle(IWorkbook book) {
            if (TitleStyle is not null) return TitleStyle;
            TitleStyle = book.CreateCellStyle();

            TitleStyle.Alignment = HorizontalAlignment.Center;
            TitleStyle.VerticalAlignment = VerticalAlignment.Center;
            //边框
            TitleStyle.BorderLeft = BorderStyle.Thin;
            TitleStyle.BorderRight = BorderStyle.Thin;
            TitleStyle.BorderTop = BorderStyle.Thin;
            TitleStyle.BorderBottom = BorderStyle.Thin;
            TitleStyle.LeftBorderColor = HSSFColor.Grey50Percent.Index;
            TitleStyle.RightBorderColor = HSSFColor.Grey50Percent.Index;
            TitleStyle.TopBorderColor = HSSFColor.Grey50Percent.Index;
            TitleStyle.BottomBorderColor = HSSFColor.Grey50Percent.Index;
            //背景色
            TitleStyle.FillForegroundColor = 0;
            TitleStyle.FillPattern = FillPattern.SolidForeground;
            ((XSSFColor)TitleStyle.FillForegroundColorColor).SetRgb(new byte[] { 101, 179, 255 });

            TitleStyle.FillBackgroundColor = HSSFColor.SkyBlue.Index;
            var fontLeft = book.CreateFont();
            fontLeft.FontHeightInPoints = 10;
            fontLeft.IsBold = true;
            fontLeft.FontName = "微软雅黑";
            fontLeft.Color = HSSFColor.Grey80Percent.Index;
            TitleStyle.ShrinkToFit = true;
            TitleStyle.SetFont(fontLeft);
            return TitleStyle;
        }

        /// <summary>
        /// 内容
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        public ICellStyle CreateContentStyle(IWorkbook book) {
            if (ContentStyle is not null) return ContentStyle;
            ContentStyle = book.CreateCellStyle();
            ContentStyle.Alignment = HorizontalAlignment.Center;
            ContentStyle.VerticalAlignment = VerticalAlignment.Center;
            //边框
            ContentStyle.BorderLeft = BorderStyle.Thin;
            ContentStyle.BorderRight = BorderStyle.Thin;
            ContentStyle.BorderTop = BorderStyle.Thin;
            ContentStyle.BorderBottom = BorderStyle.Thin;
            ContentStyle.LeftBorderColor = HSSFColor.Grey50Percent.Index;
            ContentStyle.RightBorderColor = HSSFColor.Grey50Percent.Index;
            ContentStyle.TopBorderColor = HSSFColor.Grey50Percent.Index;
            ContentStyle.BottomBorderColor = HSSFColor.Grey50Percent.Index;
            var fontLeft = book.CreateFont();
            fontLeft.FontHeightInPoints = 10;
            fontLeft.FontName = "微软雅黑";
            //cellStyle.ShrinkToFit = true;
            fontLeft.Color = HSSFColor.Grey80Percent.Index;
            ContentStyle.SetFont(fontLeft);
            return ContentStyle;
        }
    }
}