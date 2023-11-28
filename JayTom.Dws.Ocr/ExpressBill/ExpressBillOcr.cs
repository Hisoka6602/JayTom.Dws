using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Ocr.ExpressBill {

    public class ExpressBillOcr : IOcr {
        private ExpressBillPool _expressBillPool = new(10);
        private SemaphoreSlim _semaphoreSlim = new(2);

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

        public async void SubmitImage(Bitmap imageBytes, string cameraSerialNumber) {
            try {
                await _semaphoreSlim.WaitAsync();
                NLog.LogManager.GetCurrentClassLogger().Error($"进入提交");
                using (var expressBill = _expressBillPool.GetObject()) {
                    if (expressBill is not null && expressBill.OcrStatus == OcrStatus.Initialized) {
                        //识别
                        var ocrResult = await expressBill.ParseOcrResult(imageBytes, cameraSerialNumber);
                        if (ocrResult is not null) {
                            NLog.LogManager.GetCurrentClassLogger().Error($"返回结果:{ocrResult.BarCode}");
                            OnOcrContentRecognized(ocrResult);
                        }
                        else {
                            NLog.LogManager.GetCurrentClassLogger().Error($"返回结果为空");
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
                NLog.LogManager.GetCurrentClassLogger().Error($"SubmitImage方法异常");
            }
            finally {
                _semaphoreSlim.Release();
            }
        }

        public Task<OcrResult?> ParseOcrResult(Bitmap imageBytes) {
            try {
                using (var expressBill = _expressBillPool.GetObject()) {
                    if (expressBill is not null && expressBill.OcrStatus is not OcrStatus.Uninitialized) {
                        //识别
                        return expressBill.ParseOcrResult(imageBytes);
                    }
                    else {
                        NLog.LogManager.GetCurrentClassLogger().Error($"expressBill对象已消耗完");
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

        public async Task<KeyValuePair<bool, string>> SetRecognitionTimeout(TimeSpan timeout) {
            return new KeyValuePair<bool, string>(false, "暂不支持设置超时");
        }

        public async Task<KeyValuePair<bool, string>> Initialize() {
            return new KeyValuePair<bool, string>(true, "已在对象池执行初始化,不需要调用");
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

        protected virtual async void OnOcrExceptionOccurred(OcrExceptionEventArgs e) {
            await Task.Yield();
            OcrExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnOcrInitializationExceptionOccurred(OcrInitializationExceptionEventArgs e) {
            await Task.Yield();
            OcrInitializationExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnOcrContentRecognized(OcrResult e) {
            await Task.Yield();
            OcrContentRecognized?.Invoke(this, e);
        }

        protected virtual async void OnAuthenticationExceptionOccurred(AuthenticationExceptionEventArgs e) {
            await Task.Yield();
            AuthenticationExceptionOccurred?.Invoke(this, e);
        }
    }
}