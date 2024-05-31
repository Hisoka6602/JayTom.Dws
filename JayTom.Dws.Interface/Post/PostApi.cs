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

namespace JayTom.Dws.Interface.Post {

    public class PostApi : IDataUploader {
        private readonly IHttpClientFactory _httpClientFactory;
        public ApiParameters Parameters { get; private set; } = new();
        private static long _num = 0;

        public PostApi(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
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
                data =
                    @$"<soapenv:Envelope xmlns:web=""http://serverNs.webservice.pcs.jdpt.chinapost.cn/"" xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header />
    <soapenv:Body>
        <web:getGKCX>
            <arg0>#HEAD::{DateTime.Now:yyyyMM}{Parameters.WorkshopCode}FJ{_num.ToString().PadLeft(9, '0')}::{barcode}::{Parameters.DeviceId}::{Parameters.WorkshopCode}:: :: ::||#END</arg0>
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

                        if (parts.Length > 7 && parts[6].Length >= 4) {
                            //格口
                            resultContent += $"格口:[{parts[6][..4]}]";
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
                    RequestUrl = Parameters.Url,
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
        <web:getGKCX>
            <arg0>#HEAD::{DateTime.Now:yyyyMM}{Parameters.WorkshopCode}FJ{_num.ToString().PadLeft(9, '0')}::{barcode}::{Parameters.DeviceId}::{Parameters.WorkshopCode}:: :: ::||#END</arg0>
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

                        if (parts.Length > 7 && parts[6].Length >= 4) {
                            //格口
                            resultContent += $"格口:[{parts[6][..4]}]";
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
                    RequestUrl = Parameters.Url,
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

        public void UploadInBackground(string barcode, double weight, DateTime scanTime, double length = default,
            double width = default, double height = default, double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default) {
        }

        public void PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
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
            public GetGKCXRequest GetGKCX { get; set; }

            public SoapBody() {
                GetGKCX = new GetGKCXRequest();
            }
        }

        public class GetGKCXRequest {

            [XmlElement(ElementName = "arg0", Namespace = "")]
            public string Arg0 { get; set; }
        }
    }
}