using System;
using System.Xml;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace JayTom.Dws.Interface.Post {
    public class PostApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameters? Parameters { get; private set; }
        private static long _num = 1;
        public object SettingLock { get; private set; } = new();

        public PostApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
            lock (SettingLock) {
                try {
                    if (Parameters is null) {
                        IConfiguration configuration = new ConfigurationBuilder()
                            .SetBasePath($"{AppContext.BaseDirectory}ApiSettingJson")
                            .AddJsonFile("PostProcessingCenterSettings.json", optional: false, reloadOnChange: true)
                            .Build();
                        Parameters = new ApiParameters() {
                            Url = configuration["Url"] ?? string.Empty,
                            Timeout = Convert.ToInt32(configuration["Timeout"]),
                            EmployeeNumber = configuration["EmployeeNumber"] ?? string.Empty,
                            DeviceId = configuration["DeviceId"] ?? string.Empty,
                            WorkshopCode = configuration["WorkshopCode"] ?? string.Empty,
                            LocalServiceUrl = configuration["LocalServiceUrl"] ?? string.Empty,
                        };
                    }
                }
                catch (Exception e) {
                    Parameters = new();
                    NLog.LogManager.GetCurrentClassLogger().Error($"读取接口配置错误:{e}");
                }
                _httpClientFactory = httpClientFactory;
            }
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default) {
            //请求格口
            SubmitScanInfo(barcode, token);
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
                data =
                    @$"<soapenv:Envelope xmlns:web=""http://serverNs.webservice.pcs.jdpt.chinapost.cn/"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header />
    <soapenv:Body>
        <web:getGKCX>
            <arg0>#HEAD::{DateTime.Now:yyyyMM}{Parameters?.WorkshopCode}FJ{_num.ToString().PadLeft(9, '0')}::{barcode}::{Parameters?.DeviceId}::{Parameters?.WorkshopCode}:: :: ::||#END</arg0>
        </web:getGKCX>
    </soapenv:Body>
</soapenv:Envelope>";
                /*
                var envelope = new SoapEnvelope {
                    Body = new SoapBody {
                        GetGKCX = new GetGKCXRequest {
                            Arg0 = $"#HEAD::{DateTime.Now:yyyyMM}{Parameters.WorkshopCode}FJ{_num.ToString().PadLeft(9, '0')}::{barcode}::{Parameters.DeviceId}::{Parameters.WorkshopCode}:: :: ::||#END"
                        }
                    }
                };

                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                namespaces.Add("web", "http://serverNs.webservice.pcs.jdpt.chinapost.cn/");

                var serializer = new XmlSerializer(typeof(SoapEnvelope));
                var settings = new XmlWriterSettings {
                    OmitXmlDeclaration = true,
                    Indent = true,
                    IndentChars = "    ",
                    Async = true
                };
                await using (var stringWriter = new StringWriter())
                await using (var xmlWriter = XmlWriter.Create(stringWriter, settings)) {
                    serializer.Serialize(xmlWriter, envelope, namespaces);
                    data = stringWriter.ToString();
                }*/

                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters?.Timeout ?? 1000);
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
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public async Task<UploadResponse> UploadData(string barcode, double weight, DateTime scanTime, double length = default, double width = default,
            double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
            //请求格口
            SubmitScanInfo(barcode, token);
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

                data = $@"
<soapenv:Envelope xmlns:web=""http://serverNs.webservice.pcs.jdpt.chinapost.cn/"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header />
    <soapenv:Body>
        <web:getGKCX>
            <arg0>#HEAD::{DateTime.Now:yyyyMM}{Parameters?.WorkshopCode}FJ{_num.ToString().PadLeft(9, '0')}::{barcode}::{Parameters?.DeviceId}::{Parameters?.WorkshopCode}:: :: ::||#END</arg0>
        </web:getGKCX>
    </soapenv:Body>
</soapenv:Envelope>";

                /*
                var envelope = new SoapEnvelope {
                    Body = new SoapBody {
                        GetGKCX = new GetGKCXRequest {
                            Arg0 = $"#HEAD::{DateTime.Now:yyyyMM}{Parameters.WorkshopCode}FJ{_num.ToString().PadLeft(9, '0')}::{barcode}::{Parameters.DeviceId}::{Parameters.WorkshopCode}:: :: ::||#END"
                        }
                    }
                };

                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                namespaces.Add("web", "http://serverNs.webservice.pcs.jdpt.chinapost.cn/");
                var serializer = new XmlSerializer(typeof(SoapEnvelope));
                var settings = new XmlWriterSettings {
                    OmitXmlDeclaration = true,
                    Indent = true,
                    IndentChars = "    ",
                    Async = true
                };
                await using (var stringWriter = new StringWriter())
                await using (var xmlWriter = XmlWriter.Create(stringWriter, settings)) {
                    serializer.Serialize(xmlWriter, envelope, namespaces);
                    data = stringWriter.ToString();
                }*/
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters?.Timeout ?? 1000);
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
                    ResponseTime = DateTime.Now
                };
            }
            return response;
        }

        public Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters) {
            //先默认
            return Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));
        }

        public async void UploadInBackground(string barcode, double weight, DateTime scanTime, double length = default,
            double width = default, double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
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
                    data = $@"
<soapenv:Envelope xmlns:web=""http://serverNs.webservice.pcs.jdpt.chinapost.cn/"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header />
    <soapenv:Body>
        <web:getYJLG>
            <arg0>#HEAD::{Parameters?.DeviceId}::{barcode}::{"0"}::{"0"}::{Parameters?.EmployeeNumber}::{"0"}::{DateTime.Now:yyyyMMddHHmmss}::{routingDirection}::{lcgk}::{mailType}::{chuteCode}::{Parameters?.WorkshopCode}::{"1"}::{"0"}::{"0"}::{"0"}::{"0"}::{"0"}::{"0"}::{"0"}::{"0"}::{playId}::||#END</arg0>
        </web:getYJLG>
    </soapenv:Body>
</soapenv:Envelope>";
                    using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                    httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters?.Timeout ?? 100);
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
                        ResponseTime = DateTime.Now
                    };
                    NLog.LogManager.GetCurrentClassLogger().Error($"落格返回：{JsonConvert.SerializeObject(response)}");
                }
            }
        }

        public void PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
        }

        /// <summary>
        /// 提交扫描信息
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="token"></param>
        public async void SubmitScanInfo(string barcode, CancellationToken token = default) {
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
                data = $@"
<soapenv:Envelope xmlns:web=""http://serverNs.webservice.pcs.jdpt.chinapost.cn/"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header />
    <soapenv:Body>
        <web:getYJSM>
            <arg0>#HEAD::{Parameters?.DeviceId}::{barcode}::{Parameters?.EmployeeNumber}::{DateTime.Now:yyyyMMddHHmmss}::2::0::0::{Parameters?.WorkshopCode}::0::0::0::0::0::0::0::||#END</arg0>
        </web:getYJSM>
    </soapenv:Body>
</soapenv:Envelope>";
                using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters?.Timeout ?? 1000);
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
                    ResponseTime = DateTime.Now
                };
                NLog.LogManager.GetCurrentClassLogger().Error(JsonConvert.SerializeObject(response));
            }
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
                    var data = $@"
<soapenv:Envelope xmlns:web=""http://serverNs.webservice.pcs.jdpt.chinapost.cn/"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header />
    <soapenv:Body>
        <web:getGkzt>
            <arg0>{exit}</arg0>
        </web:getGkzt>
    </soapenv:Body>
</soapenv:Envelope>";

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

        public class ApiParameters {
            /// <summary>
            /// URL
            /// </summary>
            public string Url { get; set; } = "http://10.4.188.85/pcs-tc-nc-job/WyService/services/CommWY?wsdl";

            /// <summary>
            /// 超时时间 (Timeout in milliseconds)
            /// </summary>
            public int Timeout { get; set; } = 1000;

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

        [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public class SoapEnvelope {
            [XmlElement(ElementName = "Header", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
            public SoapHeader Header { get; set; }

            [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
            public SoapBody Body { get; set; }

            public SoapEnvelope() {
                Header = new SoapHeader(); // Ensure Header is not null
                Body = new SoapBody();
            }
        }

        public class SoapHeader {
            // 可以根据需要添加头部元素
        }

        public class SoapBody {
            [XmlElement(ElementName = "getGKCX", Namespace = "http://serverNs.webservice.pcs.jdpt.chinapost.cn/")]
            public GetGKCXRequest GetGkcx { get; set; }

            public SoapBody() {
                GetGkcx = new GetGKCXRequest();
            }
        }

        public class GetGKCXRequest {
            [XmlElement(ElementName = "arg0", Namespace = "")]
            public string Arg0 { get; set; }
        }
    }
}