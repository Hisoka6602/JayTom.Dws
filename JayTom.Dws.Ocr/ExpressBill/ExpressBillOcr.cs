using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Ocr.Yolo;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Ocr.ExpressBill {

    public class ExpressBillOcr : IOcr {
        private readonly ExpressBillPool _expressBillPool = new(10);
        private readonly SemaphoreSlim _semaphoreSlim = new(2);
        private readonly object _confirmationLock = new();
        private static string _onnxModel = string.Empty;

        private static float _confidenceThreshold = 0.5F;
        private static float _rectangleScale = 1;
        private static bool _isSecondConfirmationEnabled;
        private string _lastBarCode = string.Empty;

        public ExpressBillOcr() {
            //释放文件
            CopyFiles($".\\ExpressBill\\Lib\\Dll", AppDomain.CurrentDomain.BaseDirectory);
        }

        public void Dispose() {
        }

        public event EventHandler<OcrExceptionEventArgs>? OcrExceptionOccurred;

        public event EventHandler<OcrInitializationExceptionEventArgs>? OcrInitializationExceptionOccurred;

        public event EventHandler<OcrResult>? OcrContentRecognized;

        public event EventHandler<AuthenticationExceptionEventArgs>? AuthenticationExceptionOccurred;

        public OcrStatus OcrStatus { get; private set; } = OcrStatus.Initialized;

        public async Task SubmitImage(Bitmap imageBytes, string cameraSerialNumber) {
            var lockTaken = false;
            try {
                await _semaphoreSlim.WaitAsync();
                lockTaken = true;
                using (var expressBill = _expressBillPool.GetObject()) {
                    if (expressBill.OcrStatus == OcrStatus.Initialized) {
                        //识别
                        if (string.IsNullOrEmpty(expressBill.OnnxModel)) {
                            expressBill.OnnxModel = _onnxModel;
                        }

                        var ocrResult = await expressBill.ParseOcrResult(imageBytes, cameraSerialNumber);
                        if (ocrResult is not null) {
                            OnOcrContentRecognized(ocrResult);
                        }
                    }
                    else if (expressBill?.OcrStatus is OcrStatus.Uninitialized) {
                        //鉴权异常
                        OnAuthenticationExceptionOccurred(new AuthenticationExceptionEventArgs() {
                            Exception = new Exception("Ocr鉴权异常"),
                            ExceptionTime = DateTime.Now
                        });
                    }
                    else {
                        NLog.LogManager.GetCurrentClassLogger().Error($"expressBill对象已消耗完");
                    }
                }
            }
            catch (Exception e) {
                OnOcrExceptionOccurred(new OcrExceptionEventArgs() {
                    Exception = e,
                    ExceptionTime = DateTime.Now
                });
            }
            finally {
                if (lockTaken) {
                    _semaphoreSlim.Release();
                }
            }
        }

        public OcrResult? ParseOcrTemporarilyResult(Bitmap imageBytes, string cropImageModelPath, float confidenceThreshold,
            float rectangleScale) {
            try {
                using (var expressBill = _expressBillPool.GetObject()) {
                    if (expressBill.OcrStatus is not OcrStatus.Uninitialized) {
                        return expressBill.ParseOcrResult(imageBytes,
                            new YoloParser(cropImageModelPath), confidenceThreshold, rectangleScale);
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"ParseOcrResult方法异常:{e}");
            }

            return null;
        }

        public async Task<OcrResult?> ParseOcrResultAsync(Bitmap imageBytes) {
            try {
                using (var expressBill = _expressBillPool.GetObject()) {
                    if (expressBill.OcrStatus is not OcrStatus.Uninitialized) {
                        //识别
                        if (string.IsNullOrEmpty(expressBill.OnnxModel)) {
                            expressBill.OnnxModel = _onnxModel;
                        }

                        var ocrResultAsync = await expressBill.ParseOcrResultAsync(imageBytes);
                        if (_isSecondConfirmationEnabled) {
                            lock (_confirmationLock) {
                                if (ocrResultAsync?.BarCode.Equals(_lastBarCode) != true) {
                                    _lastBarCode = ocrResultAsync?.BarCode ?? string.Empty;
                                    return null;
                                }
                            }
                        }
                        return ocrResultAsync;
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"ParseOcrResult方法异常:{e}");
            }

            return null;
        }

        public OcrResult? ParseOcrResult(Bitmap imageBytes) {
            try {
                using (var expressBill = _expressBillPool.GetObject()) {
                    if (expressBill.OcrStatus is not OcrStatus.Uninitialized) {
                        if (string.IsNullOrEmpty(expressBill.OnnxModel)) {
                            expressBill.OnnxModel = _onnxModel;
                        }

                        var ocrResult = expressBill.ParseOcrResult(imageBytes, _confidenceThreshold, _rectangleScale);
                        if (_isSecondConfirmationEnabled) {
                            lock (_confirmationLock) {
                                if (ocrResult?.BarCode.Equals(_lastBarCode) != true) {
                                    _lastBarCode = ocrResult?.BarCode ?? string.Empty;
                                    return null;
                                }
                            }
                        }

                        return ocrResult;
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"ParseOcrResult方法异常:{e}");
            }

            return null;
        }

        public async Task<KeyValuePair<bool, string>> SetOcrParameters(Dictionary<string, object> parameters) {
            await Task.Yield();
            if (!Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}ExpressBill\\Lib\\resource")) {
                return new KeyValuePair<bool, string>(false, "路径不存在");
            }
            List<string> parameterNames = new()
            {
                "three_segment_code",//三段码开关控制参数
                "recipient_name",//收件人姓名开关控制参数
                "recipient_phone",//收件人手机号开关控制参数
                "recipient_addr",//收件人地址开关控制参数
                "sender_name",//寄件人姓名开关控制参数
                "sender_phone",//寄件人手机号开关控制参数
                "sender_addr",//寄件人地址开关控制参数
                "virtual_number",//虚拟面单开关控制参数
                "log_level",//选填，日志级别(TRACE, DEBUG, INFO, WARN, ERR, CRITICAL, OFF)
                "log_path ",//选填，日志输出路径
                "console_log",//选填，是否输出控制台（Android下为logcat）日志
            };
            try {
                var list = parameters?.Where(w => !parameterNames.Contains(w.Key))?
                    .Select(s => s.Key)?.ToList();
                if (list?.Any() == true) {
                    return new KeyValuePair<bool, string>(false, $"参数不存在:{string.Join(",", list)}");
                }

                var lines = (await File.ReadAllLinesAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExpressBill", "Lib", "resource", "configure.ini")))
                    // 过滤注释行和不符合预期格式的行
                    .ToList();
                lines = lines.Select(line => {
                    foreach (var parameter in (parameters ?? new Dictionary<string, object>()).Where(parameter => line.StartsWith(parameter.Key))) {
                        // 修改 log_level 的值
                        line = $"{parameter.Key}{(line.Contains("=") ? "=" : ":")}{parameter.Value?.ToString()?.ToLower()}";
                    }

                    return line;
                }).ToList();
                await File.WriteAllLinesAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExpressBill", "Lib", "resource", "configure.ini"), lines);
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public Task<KeyValuePair<bool, string>> SetOnnxModelPath(string onnxModelPath) {
            if (File.Exists(onnxModelPath)) {
                if (new FileInfo(onnxModelPath).Extension.Contains("onnx")) {
                    _onnxModel = onnxModelPath;
                    return Task.FromResult(new KeyValuePair<bool, string>(true, "设置成功"));
                }
            }
            return Task.FromResult(new KeyValuePair<bool, string>(false, "找不到文件或文件不匹配"));
        }

        public Task<KeyValuePair<bool, string>> SetConfidenceThreshold(float confidenceThreshold) {
            _confidenceThreshold = confidenceThreshold;
            return Task.FromResult(new KeyValuePair<bool, string>(true, "设置成功"));
        }

        public Task<KeyValuePair<bool, string>> SetRectangleScale(float rectangleScale) {
            _rectangleScale = rectangleScale;
            return Task.FromResult(new KeyValuePair<bool, string>(true, "设置成功"));
        }

        public Task<KeyValuePair<bool, string>> SetRecognitionTimeout(TimeSpan timeout) {
            return Task.FromResult(new KeyValuePair<bool, string>(false, "暂不支持设置超时"));
        }

        public Task<KeyValuePair<bool, string>> SetIsSecondConfirmationEnabled(bool isUse) {
            _isSecondConfirmationEnabled = isUse;
            return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
        }

        public Task<KeyValuePair<bool, string>> Initialize() {
            return Task.FromResult(new KeyValuePair<bool, string>(true, "已在对象池执行初始化,不需要调用"));
        }

        private void CopyFiles(string sourceDirectory, string targetDirectory) {
            try {
                // 获取源目录和目标目录中的所有文件
                var sourceFiles = Directory.GetFiles(sourceDirectory);
                var targetFiles = Directory.GetFiles(targetDirectory);

                // 使用 LINQ 过滤出尚未复制的文件并进行复制
                var list = sourceFiles?.Select(s => new FileInfo(s).Name)?.ToList()
                    ?.Except(targetFiles?.Select(s1 => new FileInfo(s1).Name)?.ToList() ?? new List<string>())
                    ?.ToList() ?? new List<string>();

                // 复制文件
                foreach (var file in list) {
                    File.Copy($"{sourceDirectory}\\{file}" ?? string.Empty, Path.Combine(targetDirectory, Path.GetFileName(file) ?? string.Empty));
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
        }

        protected virtual void OnOcrExceptionOccurred(OcrExceptionEventArgs e) {
            OcrExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnOcrInitializationExceptionOccurred(OcrInitializationExceptionEventArgs e) {
            OcrInitializationExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnOcrContentRecognized(OcrResult e) {
            OcrContentRecognized?.Invoke(this, e);
        }

        protected virtual void OnAuthenticationExceptionOccurred(AuthenticationExceptionEventArgs e) {
            AuthenticationExceptionOccurred?.Invoke(this, e);
        }
    }
}
