using System;
using System.Xml;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Reflection;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Collections.Generic;
using JayTom.Dws.Domain.Interface;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Domain.Interface.Attributes;

namespace JayTom.Dws.Interface.ApiImplementations.Post {

    [ApiClass("邮政处理中心Api", "PostApi", "PostApiParameters", "1.0", ExecutionType.UploadInformation | ExecutionType.SendSortingReport | ExecutionType.ScanPackage)]
    public class PostApi : IApiUploader<PostApi.ApiParameters> {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameters Parameters { get; private set; } = new();

        public bool SetParameters(object parameters) {
            if (parameters is not ApiParameters param) return false;
            lock (Parameters) {
                try {
                    IConfiguration configuration = new ConfigurationBuilder()
                        .SetBasePath($"{AppContext.BaseDirectory}ApiSettingJson")
                        .AddJsonFile("PostProcessingCenterSettings.json", optional: false, reloadOnChange: true)
                        .Build();
                    Parameters = new ApiParameters() {
                        Url = configuration["Url"] ?? string.Empty,
                        TimeOut = Convert.ToInt32(configuration["Timeout"]),
                        EmployeeNumber = configuration["EmployeeNumber"] ?? string.Empty,
                        DeviceId = configuration["DeviceId"] ?? string.Empty,
                        WorkshopCode = configuration["WorkshopCode"] ?? string.Empty,
                        LocalServiceUrl = configuration["LocalServiceUrl"] ?? string.Empty,
                    };
                }
                catch (Exception e) {
                    Parameters = new();
                    NLog.LogManager.GetCurrentClassLogger().Error($"读取接口配置错误:{e}");
                    return false;
                }
            }

            return true;
        }

        public async Task<UploadResponse> UploadInformation([NotNull] string barcode, [NotNull] double weight, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            var data = string.Empty;

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                //---组数据

                data = ConvertXmlElementData("getGKCX",
                    $"#HEAD::{DateTime.Now:yyyyMM}{Parameters?.WorkshopCode}FJ{_num.ToString().PadLeft(9, '0')}::{barcode}::{Parameters?.DeviceId}::{Parameters?.WorkshopCode}:: :: ::||#END");
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters?.TimeOut ?? 1000);
                HttpResponseMessage message;
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(data))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "text/xml");
                    message = await httpClient.PostAsync(Parameters?.Url, content, token)
                        .ConfigureAwait(false);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
                if (!string.IsNullOrWhiteSpace(resultContent)) {
                    var pattern = @"#HEAD::(.*?)::\|\|#END";
                    var match = Regex.Match(resultContent, pattern);
                    if (match.Success) {
                        // Extract the content and split by '::'
                        var content = match.Groups[1].Value;
                        var parts = content.Split(new string[] { "::" }, StringSplitOptions.None);

                        if (parts.Length > 7 && parts[6].Length >= 8) {
                            var exit = $"格口:[{parts[6][..4]}]";
                            //判断备用格口
                            if (await IsExitLocked(parts[6][..4], token)) {
                                exit = $"格口:[{parts[6][4..8]}]";
                            }
                            resultContent += exit;
                            isSuccess = true;
                        }
                    }
                }
                //判断是否成功条件
            }
            catch (HttpRequestException e) {
                isSuccess = false;
                resultContent += exceptionMsg = e.Message;
            }
            catch (AggregateException) {
                isSuccess = false;
                resultContent += exceptionMsg = "接口访问异常!";
            }
            catch (JsonException) {
                isSuccess = false;
                resultContent += exceptionMsg = "报文解析异常!";
            }
            catch (TaskCanceledException) {
                isSuccess = false;
                resultContent += exceptionMsg = "接口访问返回超时!";
            }
            catch (Exception e) {
                isSuccess = false;
                resultContent += exceptionMsg = e.Message;
            }
            finally {
                _num++;
                stopwatch.Stop();
                response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = JsonConvert.SerializeObject(data),
                    RequestTime = requestTime,
                    RequestUrl = Parameters?.Url ?? string.Empty,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now,
                    ExecutionType = ExecutionType.UploadInformation
                };
            }
            return response;
        }

        public async void ScanPackage([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            //提交扫描信息
            if (barcode.Equals("NoRead", StringComparison.CurrentCultureIgnoreCase)) {
                return;
            }
            UploadResponse response;
            var resultContent = string.Empty;
            var exceptionMsg = string.Empty;
            var isSuccess = false;
            var requestTime = DateTime.Now;
            var data = string.Empty;

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                NLog.LogManager.GetCurrentClassLogger().Error(JsonConvert.SerializeObject("开始提交扫描信息"));
                data = ConvertXmlElementData("getYJSM",
                    $"#HEAD::{Parameters?.DeviceId}::{barcode}::{Parameters?.EmployeeNumber}::{DateTime.Now:yyyyMMddHHmmss}::2::0::0::{Parameters?.WorkshopCode}::0::0::0::0::0::0::0::||#END");

                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters?.TimeOut ?? 1000);
                HttpResponseMessage message;
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(data))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "text/xml");
                    message = await httpClient.PostAsync(Parameters?.Url, content, token)
                        .ConfigureAwait(false);
                }

                resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                resultContent = Regex.Unescape(resultContent);
            }
            catch (HttpRequestException e) {
                isSuccess = false;
                resultContent += exceptionMsg = e.Message;
            }
            catch (AggregateException) {
                isSuccess = false;
                resultContent += exceptionMsg = "接口访问异常!";
            }
            catch (JsonException) {
                isSuccess = false;
                resultContent += exceptionMsg = "报文解析异常!";
            }
            catch (TaskCanceledException) {
                isSuccess = false;
                resultContent += exceptionMsg = "接口访问返回超时!";
            }
            catch (Exception e) {
                isSuccess = false;
                resultContent += exceptionMsg = e.Message;
            }
            finally {
                stopwatch.Stop();
                response = new UploadResponse() {
                    ExceptionMsg = exceptionMsg,
                    ApiParameters = JsonConvert.SerializeObject(this),
                    IsSuccess = isSuccess,
                    Duration = stopwatch.Elapsed.TotalSeconds,
                    RequestContent = data,
                    RequestTime = requestTime,
                    RequestUrl = Parameters?.Url ?? string.Empty,
                    ResponseContent = resultContent,
                    ResponseTime = DateTime.Now,
                };
                NLog.LogManager.GetCurrentClassLogger().Error(JsonConvert.SerializeObject(response));
            }
        }

        public async Task<UploadResponse> SendSortingReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            UploadResponse response = new();
            if (other is UploadResponse { IsSuccess: true } uploadResponse) {
                var chuteCode = "0";//6
                var routingDirection = "0";//1
                var mailType = "0";//5
                var playId = "0";//2
                var lcgk = "0";
                if (!string.IsNullOrWhiteSpace(uploadResponse.ResponseContent)) {
                    var pattern = @"#HEAD::(.*?)::\|\|#END";
                    var match = Regex.Match(uploadResponse.ResponseContent, pattern);
                    if (match.Success) {
                        // Extract the content and split by '::'
                        var content = match.Groups[1].Value;
                        var parts = content.Split(new string[] { "::" }, StringSplitOptions.None);

                        if (parts.Length > 6 && parts[6].Length >= 4) {
                            //格口
                            int.TryParse($"{parts[6][..4]}", out var exit);
                            //判断备用格口
                            if (await IsExitLocked(parts[6][..4], token)) {
                                int.TryParse($"{parts[6][4..8]}", out exit);
                            }
                            chuteCode = exit.ToString();
                            //这里换成实际落格
                        }
                        //路向-4
                        if (parts.Length > 1) {
                            routingDirection = parts[1];
                        }
                        if (parts.Length > 5) {
                            mailType = parts[5];
                        }
                        if (parts.Length > 2) {
                            playId = parts[2];
                        }
                        if (parts.Length > 3) {
                            lcgk = parts[3];
                        }
                    }
                }

                var resultContent = string.Empty;
                var exceptionMsg = string.Empty;
                var isSuccess = false;
                var requestTime = DateTime.Now;
                var data = string.Empty;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                try {
                    NLog.LogManager.GetCurrentClassLogger().Error(JsonConvert.SerializeObject("开始提交扫描信息"));
                    data = ConvertXmlElementData("getYJLG",
                        $"#HEAD::{Parameters?.DeviceId}::{barcode}::{"0"}::{"0"}::{Parameters?.EmployeeNumber}::{"0"}::{DateTime.Now:yyyyMMddHHmmss}::{routingDirection}::{lcgk}::{mailType}::{chuteCode}::{Parameters?.WorkshopCode}::{"1"}::{"0"}::{"0"}::{"0"}::{"0"}::{"0"}::{"0"}::{"0"}::{"0"}::{playId}::||#END");
                    using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters?.TimeOut ?? 100);
                    HttpResponseMessage message;
                    await using (Stream dataStream =
                                 new MemoryStream(Encoding.UTF8.GetBytes(data))) {
                        using HttpContent content = new StreamContent(dataStream);
                        content.Headers.Add("Content-Type", "text/xml");
                        message = await httpClient.PostAsync(Parameters?.Url, content, token)
                            .ConfigureAwait(false);
                    }

                    resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                }
                catch (HttpRequestException e) {
                    isSuccess = false;
                    resultContent += exceptionMsg = e.Message;
                }
                catch (AggregateException) {
                    isSuccess = false;
                    resultContent += exceptionMsg = "接口访问异常!";
                }
                catch (JsonException) {
                    isSuccess = false;
                    resultContent += exceptionMsg = "报文解析异常!";
                }
                catch (TaskCanceledException) {
                    isSuccess = false;
                    resultContent += exceptionMsg = "接口访问返回超时!";
                }
                catch (Exception e) {
                    isSuccess = false;
                    resultContent += exceptionMsg = e.Message;
                }
                finally {
                    stopwatch.Stop();
                    response = new UploadResponse() {
                        ExceptionMsg = exceptionMsg,
                        ApiParameters = JsonConvert.SerializeObject(this),
                        IsSuccess = isSuccess,
                        Duration = stopwatch.Elapsed.TotalSeconds,
                        RequestContent = data,
                        RequestTime = requestTime,
                        RequestUrl = Parameters?.Url ?? string.Empty,
                        ResponseContent = resultContent,
                        ResponseTime = DateTime.Now,
                        ExecutionType = ExecutionType.SendSortingReport
                    };
                    NLog.LogManager.GetCurrentClassLogger().Error($"落格返回：{JsonConvert.SerializeObject(response)}");
                }
            }

            return response;
        }

        public Task<UploadResponse> SendPickupReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default,
            double width = default, double height = default, double volume = default, long packageId = default,
            UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendPickupReport
            });
        }

        public Task<UploadResponse> SendConsolidationReport(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendConsolidationReport
            });
        }

        public Task<UploadResponse> SendImage(string barcode, List<UploadImageInfo> uploadImagesInfos, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendImage
            });
        }

        public Task<UploadResponse> SendLockCommand(string lockIdentifier, object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendLockCommand
            });
        }

        public Task<UploadResponse> SendUnlockCommand(string lockIdentifier, object? other = null, CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendUnlockCommand
            });
        }

        public Task<UploadResponse> SendDeviceReport(string deviceIdentifier, string deviceStatus, object? other = null,
            CancellationToken token = default) {
            return Task.FromResult(new UploadResponse() {
                ExecutionType = ExecutionType.SendDeviceReport
            });
        }

        private static long _num = 1;
        public object SettingLock { get; private set; } = new();

        public PostApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// 判断锁格
        /// </summary>
        /// <param name="exit"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> IsExitLocked(string exit, CancellationToken token = default) {
            try {
                if (!string.IsNullOrEmpty(Parameters?.LocalServiceUrl)) {
                    var data = ConvertXmlElementData("getGkzt",
                         $"{exit}");
                    using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                    httpClient.Timeout = TimeSpan.FromMilliseconds(1000);
                    HttpResponseMessage message;
                    await using (Stream dataStream =
                                 new MemoryStream(Encoding.UTF8.GetBytes(data))) {
                        using HttpContent content = new StreamContent(dataStream);
                        content.Headers.Add("Content-Type", "text/xml");
                        message = await httpClient.PostAsync(Parameters?.LocalServiceUrl, content, token)
                            .ConfigureAwait(false);
                    }

                    var resultContent = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    resultContent = Regex.Unescape(resultContent);
                    return resultContent.Contains("已锁格");
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            return false;
        }

        public class ApiParameters : BaseApiParameters {
            /*/// <summary>
            /// URL
            /// </summary>
            public string Url { get; set; } = "http://10.4.188.85/pcs-tc-nc-job/WyService/services/CommWY?wsdl";*/

            /// <summary>
            /// 车间代码 (Workshop code)
            /// </summary>
            public string WorkshopCode { get; set; } = "WS43400001";

            /// <summary>
            /// 设备ID (Device ID)
            /// </summary>
            public string DeviceId { get; set; } = "43400002";

            /// <summary>
            /// 员工号 (Employee number)
            /// </summary>
            public string EmployeeNumber { get; set; } = "03178298";

            /// <summary>
            /// 本地服务Url
            /// </summary>
            public string LocalServiceUrl { get; set; } = string.Empty;
        }

        private string ConvertXmlElementData(string methodName, string innerText) {
            var envelope = new SoapEnvelope {
                Header = new SoapHeader(),
                Body = new SoapBody {
                    MethodElement = CreateMethodElement(methodName, innerText)
                }
            };

            var xmlSerializer = new XmlSerializer(typeof(SoapEnvelope));
            var settings = new XmlWriterSettings {
                OmitXmlDeclaration = true,
                Indent = true,
                Encoding = Encoding.UTF8
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
            namespaces.Add("web", "http://serverNs.webservice.pcs.jdpt.chinapost.cn/");
            xmlSerializer.Serialize(xmlWriter, envelope, namespaces);

            var xmlString = stringWriter.ToString();

            // Remove the namespace declaration from the <web:getGKCX> element
            var startIdx = xmlString.IndexOf($"<web:{methodName} ", StringComparison.Ordinal);
            if (startIdx >= 0) {
                var endIdx = xmlString.IndexOf('>', startIdx);
                if (endIdx >= 0) {
                    xmlString = xmlString.Remove(startIdx, endIdx - startIdx + 1)
                        .Insert(startIdx, $"<web:{methodName}>");
                }
            }

            return xmlString;
        }

        private static XmlElement CreateMethodElement(string methodName, string innerText) {
            var xmlDoc = new XmlDocument();
            var methodElement = xmlDoc.CreateElement("web", methodName, "http://serverNs.webservice.pcs.jdpt.chinapost.cn/");
            var arg0Element = xmlDoc.CreateElement("arg0");
            arg0Element.InnerText = innerText;
            methodElement.AppendChild(arg0Element);
            return methodElement;
        }

        [XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public class SoapEnvelope {

            [XmlElement("Header", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
            public SoapHeader Header { get; set; } = new();

            [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
            public SoapBody Body { get; set; } = new();
        }

        public class SoapHeader {
            // Header content if any, currently empty
        }

        public class SoapBody {

            [XmlAnyElement]
            public XmlElement? MethodElement { get; set; }
        }
    }
}