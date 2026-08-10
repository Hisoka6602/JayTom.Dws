using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Camera.FilterContainer {

    public class BarCodeFilterContainer {
        private static readonly ConcurrentDictionary<string, BarCodeFilterInfo> Container = new();
        /// <summary>
        /// 限制单次正则匹配的最长执行时间，防止灾难性回溯阻塞采集线程。
        /// </summary>
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
        private int _maxSize;

        /// <summary>
        /// 容器大小
        /// </summary>
        public int MaxSize {
            get => _maxSize;
            set {
                if (value < 0) {
                    throw new ArgumentException("容器大小必须大于等于零");
                }
                _maxSize = value;
                CleanupContainer();
            }
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间
        /// </summary>
        public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 过滤输出内容(为空则不输出)
        /// </summary>
        public string FilterOutContent { get; set; } = string.Empty;

        /// <summary>
        /// 过滤方式
        /// </summary>
        public BarCodeFilterMode BarCodeFilterMode { get; set; } = BarCodeFilterMode.None;

        /// <summary>
        /// 自定义正则表达式
        /// </summary>
        public List<string> CustomRegularExpressionItems { get; set; } = new();

        /// <summary>
        /// 是否使用正则替换
        /// </summary>
        public bool IsUseCustomRegexReplacement { get; set; }

        /// <summary>
        /// 是否使用过滤条码码种类
        /// </summary>
        public bool IsUseFilteredBarcodeTypes { get; set; }

        /// <summary>
        /// 正则替换项
        /// </summary>
        public List<CustomRegexReplacementItemInfo> CustomRegexReplacementItems { get; set; } = new();

        public bool InsertOrUpdate(BarCodeFilterInfo data) {
            if (!string.IsNullOrEmpty(Pattern)) {
                try {
                    if (!Regex.IsMatch(data.BarCode, Pattern, RegexOptions.CultureInvariant, RegexTimeout)) {
                        return false;
                    }
                }
                catch {
                    return false;
                }
            }
            var insertedOrUpdated = false;
            data.ExpirationTime ??= ExpirationTime;
            Container.AddOrUpdate(data.BarCode, key => {
                insertedOrUpdated = true;
                return data;
            }, (key, oldValue) => {
                insertedOrUpdated = false;
                return data;
            });
            CleanupContainer();
            return insertedOrUpdated;
        }

        public ValidationResult ValidateData(BarCodeFilterInfo barCodeFilterInfo) {
            CleanupContainer();
            if (BarCodeFilterMode == BarCodeFilterMode.BasicFilter) {
                if (!string.IsNullOrEmpty(Pattern)) {
                    try {
                        if (!Regex.IsMatch(barCodeFilterInfo.BarCode, Pattern,
                                RegexOptions.CultureInvariant, RegexTimeout)) {
                            return new ValidationResult {
                                IsValidationPassed = false,
                                FilteredCategory = FilteredCategory.RuleFiltered,
                                BarCode = barCodeFilterInfo.BarCode
                            };
                        }
                    }
                    catch {
                        return new ValidationResult {
                            IsValidationPassed = false,
                            FilteredCategory = FilteredCategory.RuleFiltered,
                            BarCode = barCodeFilterInfo.BarCode
                        };
                    }
                }
            }
            else if (BarCodeFilterMode == BarCodeFilterMode.CustomRegexFilter) {
                if (CustomRegularExpressionItems.Any()) {
                    try {
                        var any = CustomRegularExpressionItems.Any(a =>
                            Regex.IsMatch(barCodeFilterInfo.BarCode, a,
                                RegexOptions.CultureInvariant, RegexTimeout));
                        if (!any) {
                            return new ValidationResult {
                                IsValidationPassed = false,
                                FilteredCategory = FilteredCategory.RuleFiltered,
                                BarCode = barCodeFilterInfo.BarCode
                            };
                        }
                    }
                    catch (Exception e) {
                        return new ValidationResult {
                            IsValidationPassed = false,
                            FilteredCategory = FilteredCategory.RuleFiltered,
                            BarCode = barCodeFilterInfo.BarCode
                        };
                    }
                }
            }

            //后面加的
            var codeFilterInfo = Get(barCodeFilterInfo.BarCode);
            if (codeFilterInfo != null) {
                return new ValidationResult {
                    IsValidationPassed = false,
                    FilteredCategory = FilteredCategory.TimeFiltered,
                    BarCode = barCodeFilterInfo.BarCode
                };
            }
            //----------
            barCodeFilterInfo.ExpirationTime ??= ExpirationTime;
            var tryAdd = Container.TryAdd(barCodeFilterInfo.BarCode, barCodeFilterInfo);
            return new ValidationResult {
                IsValidationPassed = tryAdd,
                FilteredCategory = tryAdd ? FilteredCategory.None : FilteredCategory.TimeFiltered,
                BarCode = barCodeFilterInfo.BarCode
            };
        }

        private void CleanupContainer() {
            if (MaxSize > 0) {
                if (Container.Count <= _maxSize) {
                    return;
                }
                var oldestEntries = Container.OrderBy(kvp => kvp.Value.ScanTime)
                    .Take(Container.Count - _maxSize);
                foreach (var entry in oldestEntries) {
                    Container.TryRemove(entry.Key, out _);
                }
            }
            else {
                //删除过期的
                var pairs = Container.Where(w =>
                    DateTime.Now.Subtract(w.Value.ScanTime).TotalMilliseconds > w.Value?.ExpirationTime.Value.TotalMilliseconds);
                foreach (var pair in pairs) {
                    Container.TryRemove(pair.Key, out _);
                }
            }
        }

        public BarCodeFilterInfo? Get(string barCode) {
            CleanupContainer();
            Container.TryGetValue(barCode, out var value);
            return value;
        }

        /// <summary>
        /// 替换
        /// </summary>
        /// <param name="barCode"></param>
        /// <returns></returns>
        public string RegexReplace(string barCode) {
            if (IsUseCustomRegexReplacement && CustomRegexReplacementItems.Any()) {
                var replacedBarcode = barCode;
                try {
                    replacedBarcode = CustomRegexReplacementItems.Aggregate(replacedBarcode,
                        (current, item) => Regex.Replace(current, item.RegexPattern, item.ReplaceContent,
                            RegexOptions.CultureInvariant, RegexTimeout));
                    return replacedBarcode;
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
            return barCode;
        }

        /// <summary>
        /// 重置过滤器
        /// </summary>
        public static void ResetFilter() {
            Container.Clear();
        }
    }

    public class ValidationResult {
        public bool IsValidationPassed { get; set; }
        public FilteredCategory FilteredCategory { get; set; }
        public string BarCode { get; set; } = string.Empty;
    }

    public enum FilteredCategory {
        None,

        /// <summary>
        /// 被规则过滤
        /// </summary>
        RuleFiltered,

        /// <summary>
        /// 被时间过滤
        /// </summary>
        TimeFiltered
    }

    public class BarCodeFilterInfo {

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public TimeSpan? ExpirationTime { get; set; }
    }
}
