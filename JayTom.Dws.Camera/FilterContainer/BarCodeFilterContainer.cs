using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Camera.FilterContainer {

    public class BarCodeFilterContainer {
        private static readonly ConcurrentDictionary<string, BarCodeFilterInfo> Container = new();
        /// <summary>限制全表过期清理的执行频率，避免每个条码都扫描整个去重容器。</summary>
        private static readonly long CleanupIntervalTicks = Math.Max(1L, Stopwatch.Frequency / 4);
        /// <summary>最近一次全表过期清理使用的单调时钟刻度。</summary>
        private static long _lastCleanupTimestamp;
        /// <summary>
        /// 限制单次正则匹配的最长执行时间，防止灾难性回溯阻塞采集线程。
        /// </summary>
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
        /// <summary>按表达式复用已编译正则，避免每个条码重复走全局正则缓存和解释路径。</summary>
        private static readonly ConcurrentDictionary<string, Regex> RegexCache =
            new(StringComparer.Ordinal);
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
                CleanupContainer(true, null);
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
                    if (!GetRegex(Pattern).IsMatch(data.BarCode)) {
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
            CleanupContainer(false, data.ScanTime);
            return insertedOrUpdated;
        }

        public ValidationResult ValidateData(BarCodeFilterInfo barCodeFilterInfo) {
            if (BarCodeFilterMode == BarCodeFilterMode.BasicFilter) {
                if (!string.IsNullOrEmpty(Pattern)) {
                    try {
                        if (!GetRegex(Pattern).IsMatch(barCodeFilterInfo.BarCode)) {
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
                if (CustomRegularExpressionItems.Count > 0) {
                    try {
                        var matches = false;
                        for (var index = 0; index < CustomRegularExpressionItems.Count; index++) {
                            if (GetRegex(CustomRegularExpressionItems[index])
                                .IsMatch(barCodeFilterInfo.BarCode)) {
                                matches = true;
                                break;
                            }
                        }
                        if (!matches) {
                            return new ValidationResult {
                                IsValidationPassed = false,
                                FilteredCategory = FilteredCategory.RuleFiltered,
                                BarCode = barCodeFilterInfo.BarCode
                            };
                        }
                    }
                    catch (Exception) {
                        return new ValidationResult {
                            IsValidationPassed = false,
                            FilteredCategory = FilteredCategory.RuleFiltered,
                            BarCode = barCodeFilterInfo.BarCode
                        };
                    }
                }
            }

            barCodeFilterInfo.ExpirationTime ??= ExpirationTime;
            var tryAdd = TryAddAfterRemovingExpired(barCodeFilterInfo);
            CleanupContainer(false, barCodeFilterInfo.ScanTime);
            return new ValidationResult {
                IsValidationPassed = tryAdd,
                FilteredCategory = tryAdd ? FilteredCategory.None : FilteredCategory.TimeFiltered,
                BarCode = barCodeFilterInfo.BarCode
            };
        }

        /// <summary>原子检查当前条码的去重窗口，并在旧记录过期后写入新记录。</summary>
        private bool TryAddAfterRemovingExpired(BarCodeFilterInfo candidate) {
            while (true) {
                if (!Container.TryGetValue(candidate.BarCode, out var existing)) {
                    return Container.TryAdd(candidate.BarCode, candidate);
                }

                if (MaxSize > 0 || !IsExpired(existing, candidate.ScanTime)) {
                    return false;
                }

                if (Container.TryRemove(
                        new KeyValuePair<string, BarCodeFilterInfo>(candidate.BarCode, existing))) {
                    continue;
                }
            }
        }

        /// <summary>判断去重记录在指定时间是否已经过期。</summary>
        private static bool IsExpired(BarCodeFilterInfo value, DateTime now) {
            var expiration = value.ExpirationTime ?? TimeSpan.Zero;
            return now - value.ScanTime > expiration;
        }

        /// <summary>按容量或低频机会策略清理去重容器，避免常态热路径执行全表扫描。</summary>
        private void CleanupContainer(bool force, DateTime? referenceTime) {
            if (MaxSize > 0) {
                while (Container.Count > _maxSize) {
                    KeyValuePair<string, BarCodeFilterInfo>? oldest = null;
                    foreach (var pair in Container) {
                        if (oldest is null || pair.Value.ScanTime < oldest.Value.Value.ScanTime) {
                            oldest = pair;
                        }
                    }

                    if (oldest is null || !Container.TryRemove(oldest.Value)) {
                        break;
                    }
                }
                return;
            }

            var timestamp = Stopwatch.GetTimestamp();
            var previous = Volatile.Read(ref _lastCleanupTimestamp);
            if (!force && timestamp - previous < CleanupIntervalTicks) {
                return;
            }
            if (!force && Interlocked.CompareExchange(
                    ref _lastCleanupTimestamp,
                    timestamp,
                    previous) != previous) {
                return;
            }
            if (force) {
                Volatile.Write(ref _lastCleanupTimestamp, timestamp);
            }

            var now = referenceTime ?? DateTime.Now;
            foreach (var pair in Container) {
                if (IsExpired(pair.Value, now)) {
                    Container.TryRemove(pair);
                }
            }
        }

        public BarCodeFilterInfo? Get(string barCode) {
            while (Container.TryGetValue(barCode, out var value)) {
                if (MaxSize > 0 || !IsExpired(value, DateTime.Now)) {
                    return value;
                }
                if (!Container.TryRemove(new KeyValuePair<string, BarCodeFilterInfo>(barCode, value))) {
                    continue;
                }
            }
            CleanupContainer(false, DateTime.Now);
            return null;
        }

        /// <summary>
        /// 替换
        /// </summary>
        /// <param name="barCode"></param>
        /// <returns></returns>
        public string RegexReplace(string barCode) {
            if (IsUseCustomRegexReplacement && CustomRegexReplacementItems.Count > 0) {
                var replacedBarcode = barCode;
                try {
                    for (var index = 0; index < CustomRegexReplacementItems.Count; index++) {
                        var item = CustomRegexReplacementItems[index];
                        replacedBarcode = GetRegex(item.RegexPattern)
                            .Replace(replacedBarcode, item.ReplaceContent);
                    }
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

        /// <summary>获取线程安全的已编译正则快照。</summary>
        private static Regex GetRegex(string pattern) {
            return RegexCache.GetOrAdd(
                pattern,
                static value => new Regex(
                    value,
                    RegexOptions.CultureInvariant | RegexOptions.Compiled,
                    RegexTimeout));
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
