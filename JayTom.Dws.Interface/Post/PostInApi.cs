using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TouchSocket.Core;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace JayTom.Dws.Interface.Post {

    /// <summary>
    /// 揽投机构
    /// </summary>
    public class PostInApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameters? Parameters { get; private set; }
        private static long _num = 1;
        public object SettingLock { get; private set; } = new();

        public PostInApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
            lock (SettingLock) {
                try {
                    if (Parameters is null) {
                        IConfiguration configuration = new ConfigurationBuilder()
                            .SetBasePath($"{AppContext.BaseDirectory}ApiSettingJson")
                            .AddJsonFile("PostDeliveryAgencySettings.json", optional: false, reloadOnChange: true)
                            .Build();
                        Parameters = new ApiParameters() {
                            Url = configuration["Url"] ?? string.Empty,
                            Timeout = Convert.ToInt32(configuration["Timeout"]),
                            WorkshopCode = configuration["WorkshopCode"] ?? string.Empty,
                            DeviceId = configuration["DeviceId"] ?? string.Empty,
                            CompanyName = configuration["CompanyName"] ?? string.Empty,
                            DeviceBarcode = configuration["DeviceBarcode"] ?? string.Empty,
                            OrganizationNumber = configuration["OrganizationNumber"] ?? string.Empty,
                            EmployeeNumber = configuration["EmployeeNumber"] ?? string.Empty,
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
        <web:getLTGKCX>
            <arg0>#HEAD::{DateTime.Now:yyyyMM}{Parameters?.WorkshopCode}FJ{_num.ToString().PadLeft(9, '0')}::{Parameters?.DeviceId}::{barcode}::0:: :: :: ::{DateTime.Now:yyyy-MM-dd HH:mm:ss}::{Parameters?.EmployeeNumber}::{Parameters?.OrganizationNumber}::{Parameters?.CompanyName}::{Parameters?.DeviceBarcode}::||#END</arg0>
        </web:getLTGKCX>
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

                        if (parts.Length > 7 && parts[7].Length >= 4) {
                            //格口
                            resultContent += $"格口:[{parts[7][..4]}]";
                            isSuccess = true;
                            SubmitScanInfo(barcode, token);
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
                    RequestContent = data,
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
        <web:getLTGKCX>
            <arg0>#HEAD::{DateTime.Now:yyyyMM}{Parameters?.WorkshopCode}FJ{_num.ToString().PadLeft(9, '0')}::{Parameters?.DeviceId}::{barcode}::0:: :: :: ::{DateTime.Now:yyyy-MM-dd HH:mm:ss}::{Parameters?.EmployeeNumber}::{Parameters?.OrganizationNumber}::{Parameters?.CompanyName}::{Parameters?.DeviceBarcode}::||#END</arg0>
        </web:getLTGKCX>
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
                httpClient.Timeout = TimeSpan.FromMilliseconds(Parameters.Timeout);
                HttpResponseMessage message;
                await using (Stream dataStream =
                             new MemoryStream(Encoding.UTF8.GetBytes(data))) {
                    using HttpContent content = new StreamContent(dataStream);
                    content.Headers.Add("Content-Type", "text/xml");
                    message = await httpClient.PostAsync(Parameters.Url, content, token)
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

                        if (parts.Length > 7 && parts[7].Length >= 4) {
                            //格口
                            resultContent += $"格口:[{parts[7][..4]}]";
                            isSuccess = true;
                            SubmitScanInfo(barcode, token);
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
                    RequestContent = data,
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
            //提交落格信息
            if (other is UploadResponse { IsSuccess: true } uploadResponse) {
                var chuteCode = "0";
                var routingDirection = "0";
                var mailType = "0";
                var sortingSchemeCode = "0";
                if (!string.IsNullOrWhiteSpace(uploadResponse.ResponseContent)) {
                    var pattern = @"#HEAD::(.*?)::\|\|#END";
                    var match = Regex.Match(uploadResponse.ResponseContent, pattern);
                    if (match.Success) {
                        // Extract the content and split by '::'
                        var content = match.Groups[1].Value;
                        var parts = content.Split(new string[] { "::" }, StringSplitOptions.None);

                        if (parts.Length > 7 && parts[7].Length >= 4) {
                            //格口
                            int.TryParse($"{parts[7][..4]}", out var exit);
                            chuteCode = exit.ToString();
                        }
                        //路向-4
                        if (parts.Length > 4) {
                            routingDirection = parts[4];
                        }
                        if (parts.Length > 2) {
                            mailType = parts[2];
                        }

                        if (parts.Length > 1) {
                            sortingSchemeCode = parts[1];
                        }
                        //邮件种类-1
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
            <arg0>#HEAD::{Parameters?.DeviceId}::{barcode}::{0}::{0}::{Parameters?.EmployeeNumber}::{0}::{DateTime.Now:yyyyMMddHHmmss}::{routingDirection}::{mailType}::{chuteCode}::{1}::{0}::{0}::{0}::{0}::{0}::{0}::{0}::{sortingSchemeCode}||#END</arg0>
        </web:getYJLG>
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
            <arg0>#HEAD::{Parameters?.DeviceId}::{barcode}::{Parameters?.EmployeeNumber}::{DateTime.Now:yyyyMMddHHmmss}::2::001::0000::{"0000"}::0::0::0::0::0::0::0||#END</arg0>
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
        /// 顶扫稽核
        /// </summary>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> SweepTopReceiveByCsb() {
            var deliverunSweepTopReceiveQuery = new {
                opOrgCode = "机构代码",
                waybillNo = "邮件条码",
                userCode = "操作人代码",
                userName = "操作人名称",
                deviceName = "设备名称",
                machineBarcode = "设备Id"
            };

            var messageHeader = new Dictionary<string, object>()
              {
                { "sysCod", "系统标识" },
                { "sign", "签名" },
                { "serialNo", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
                { "sendDate", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}" }
            };
            return new KeyValuePair<bool, string>(false, string.Empty);
        }

        public class ApiParameters {

            /// <summary>
            /// URL
            /// </summary>
            public string Url { get; set; } = "http://10.4.201.115/pcs-ci-job/WyService/services/CommWY?wsdl";

            /// <summary>
            /// 超时时间 (Timeout in milliseconds)
            /// </summary>
            public int Timeout { get; set; } = 1000;

            /// <summary>
            /// 车间代码 (Workshop code)
            /// </summary>
            public string WorkshopCode { get; set; } = "WS20140010";

            /// <summary>
            /// 设备ID (Device ID)
            /// </summary>
            public string DeviceId { get; set; } = "20140010";

            /// <summary>
            /// 公司名称 (Company name)
            /// </summary>
            public string CompanyName { get; set; } = "广东泽业科技有限公司";

            /// <summary>
            /// 设备条码 (Device barcode)
            /// </summary>
            public string DeviceBarcode { get; set; } = "141562320001131";

            /// <summary>
            /// 机构号 (Organization number)
            /// </summary>
            public string OrganizationNumber { get; set; } = "20140011";

            /// <summary>
            /// 员工号 (Employee number)
            /// </summary>
            public string EmployeeNumber { get; set; } = "00818684";
        }
    }
}