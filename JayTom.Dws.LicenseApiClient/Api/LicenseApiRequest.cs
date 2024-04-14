using System.Net;
using System.Text;
using System.Drawing;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using JayTom.Dws.LicenseApiClient.Data.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace JayTom.Dws.LicenseApiClient.Api {

    public class LicenseApiRequest : ILicenseApiRequest {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJSRuntime _jsRuntime;

        public static string Domain { get; private set; } = string.Empty;

        public LicenseApiRequest(IHttpClientFactory httpClientFactory,
            IJSRuntime jsRuntime) {
            _httpClientFactory = httpClientFactory;
            _jsRuntime = jsRuntime;
        }

        public async Task<bool> IsLoggedIn() {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "token");
            return !string.IsNullOrEmpty(invokeAsync);
        }

        public void SetBaseUrl(string url) {
            Domain = url;
        }

        public async Task<KeyValuePair<bool, object>> Register(string userCode, string userName, string passWord, string phone, string companyName, CancellationToken token) {
            try {
                //组包

                var requestJson = JsonConvert.SerializeObject(new {
                    userCode = userCode,
                    userName = userName,
                    passWord = passWord,
                    phone = phone,
                    companyName = companyName
                });

                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromSeconds(20);
                    HttpResponseMessage message;
                    await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync($"{Domain}{"/api/User/Register"}", content, token)
                                .ConfigureAwait(false);
                        }
                    }
                    string httpResult;
                    switch (message.StatusCode) {
                        case HttpStatusCode.OK: {
                                using (message) {
                                    httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                }
                                break;
                            }
                        case HttpStatusCode.NotFound:
                            return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                        default:
                            httpResult = $"{message}";
                            break;
                    }
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                    //解码
                    var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                    return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                }
            }
            catch (HttpRequestException) {
                return new KeyValuePair<bool, object>(false, "Http访问异常!");
            }
            catch (AggregateException) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
            catch (TaskCanceledException) {
                return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
            }
            catch (Exception) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> Login(string loginCode, string passWord, CancellationToken token = default) {
            try {
                //组包

                var requestJson = JsonConvert.SerializeObject(new {
                    loginCode = loginCode,
                    passWord = passWord,
                });

                using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                    httpClient.Timeout = TimeSpan.FromSeconds(20);
                    HttpResponseMessage message;
                    await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                        using (HttpContent content = new StreamContent(dataStream)) {
                            content.Headers.Add("Content-Type", "application/json");
                            message = await httpClient.PostAsync($"{Domain}{"/api/User/Login"}", content, token)
                                .ConfigureAwait(false);
                        }
                    }
                    string httpResult;
                    switch (message.StatusCode) {
                        case HttpStatusCode.OK: {
                                using (message) {
                                    httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                }
                                break;
                            }
                        case HttpStatusCode.NotFound:
                            return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                        default:
                            httpResult = $"{message}";
                            break;
                    }

                    //解码
                    var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                    if (result is not null) {
                        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", token, "token", result?.Data?.ToString() ?? string.Empty);
                    }
                    return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                }
            }
            catch (HttpRequestException) {
                return new KeyValuePair<bool, object>(false, "Http访问异常!");
            }
            catch (AggregateException) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
            catch (TaskCanceledException) {
                return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
            }
            catch (Exception) {
                return new KeyValuePair<bool, object>(false, "接口访问异常!");
            }
        }

        public async Task<KeyValuePair<bool, object>> UpdateProfile(string? userName, string? phone, string companyName, string companyAddress, string contactEmail,
            string description, string contractFilePath, string businessLicenseFilePath, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        companyName = companyName,
                        companyAddress = companyAddress,
                        contactEmail = contactEmail,
                        description = description,
                        contractFilePath = contractFilePath,
                        businessLicenseFilePath = businessLicenseFilePath,
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/User/UpdateProfile"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> ChangePassword(string oldPassWord, string newPassWord, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        oldPassWord = oldPassWord,
                        newPassWord = newPassWord,
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/User/ChangePassword"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        if (result is not null && result.Result) {
                            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                        }
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> Info(CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        message = await httpClient.GetAsync($"{Domain}{"/api/User/Info"}", token)
                            .ConfigureAwait(false);
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        if (result is not null) {
                            var userInfo = JsonConvert.DeserializeObject<UserInfo>(result?.Data?.ToString() ?? string.Empty);
                            if (userInfo is not null) {
                                return new KeyValuePair<bool, object>(true, userInfo);
                            }
                        }
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> FreezeUser(string userCode, bool isFreeze, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        userCode = userCode,
                        isFreeze = isFreeze,
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/User/FreezeUser"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> ChangeUserIcon(Image iconImage, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    var formData = new MultipartFormDataContent();
                    var imageToStreamContent = ImageToStreamContent(iconImage, "imageFile",
                        $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.png");
                    if (imageToStreamContent is not null) {
                        formData.Add(imageToStreamContent);
                    }
                    using var httpClient = _httpClientFactory.CreateClient("INSURANCE");
                    httpClient.Timeout = TimeSpan.FromSeconds(20);
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                    var message = await httpClient.PostAsync($"{Domain}{"/api/User/ChangeUserIcon"}", formData, token);
                    string httpResult;
                    switch (message.StatusCode) {
                        case HttpStatusCode.OK: {
                                using (message) {
                                    httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                }
                                break;
                            }
                        case HttpStatusCode.NotFound:
                            return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                        case HttpStatusCode.Unauthorized:
                            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                            return new KeyValuePair<bool, object>(false, $"用户未登录!");

                        default:
                            httpResult = $"{message}";
                            break;
                    }

                    //解码
                    var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                    return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> TenantInfos(CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        message = await httpClient.GetAsync($"{Domain}{"/api/User/TenantInfos"}", token)
                            .ConfigureAwait(false);
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        if (result is not null) {
                            var userInfos = JsonConvert.DeserializeObject<List<UserInfo>>(result?.Data?.ToString() ?? string.Empty);
                            if (userInfos?.Any() == true) {
                                return new KeyValuePair<bool, object>(true, userInfos);
                            }
                            else {
                                return new KeyValuePair<bool, object>(false, "未查询到相关信息");
                            }
                        }
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> CreateApplication(string applicationName, string description, List<FeatureItemModel>? featureInfos, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        applicationName = applicationName,
                        description = description,
                        featureInfos = featureInfos
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/App/CreateApplication"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> CreateApplicationTemplate(long licenseApplicationInfoId, string templateName, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        licenseApplicationInfoId = licenseApplicationInfoId,
                        templateName = templateName,
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/App/CreateApplicationTemplate"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> SetTemplatePermissions(long templateId, List<FeatureItemModel>? featureInfos, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        templateId = templateId,
                        featureInfos = featureInfos
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/App/SetTemplatePermissions"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> ApplicationData(CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        message = await httpClient.GetAsync($"{Domain}{"/api/App/ApplicationData"}", token)
                            .ConfigureAwait(false);
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        if (result is { Result: true, Data: not null }) {
                            var models = JsonConvert.DeserializeObject<List<ApplicationItemInfoModel>>(result.Data.ToString() ??
                                string.Empty);
                            if (models is not null) {
                                return new KeyValuePair<bool, object>(true, models);
                            }
                        }
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> TemplateData(CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        message = await httpClient.GetAsync($"{Domain}{"/api/App/TemplateData"}", token)
                            .ConfigureAwait(false);
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        if (result is { Result: true, Data: not null }) {
                            var models = JsonConvert.DeserializeObject<List<AppTemplateItemInfoModel>>(result.Data.ToString() ??
                                string.Empty);
                            if (models is not null) {
                                return new KeyValuePair<bool, object>(true, models);
                            }
                        }
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> DeleteApplication(long deleteApplicationId, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        deleteApplicationId = deleteApplicationId,
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/App/DeleteApplication"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> DeleteTemplate(long deleteTemplateId, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        deleteTemplateId = deleteTemplateId,
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/App/DeleteTemplate"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> CreateLicenseCode(long templateInfoId, int maxClientCount,
            DateTime expirationDate, string clientName,
            string? userCode,
            CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        templateInfoId = templateInfoId,
                        maxClientCount = maxClientCount,
                        expirationDate = expirationDate,
                        clientName = clientName,
                        userCode = userCode
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/License/CreateLicenseCode"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> UpdateLicenseCode(long templateInfoId, string userCode, string licenseCode, int maxClientCount,
            DateTime expirationDate, string clientName, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        templateInfoId = templateInfoId,
                        maxClientCount = maxClientCount,
                        expirationDate = expirationDate,
                        clientName = clientName,
                        licenseCode = licenseCode,
                        userCode = userCode
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/License/UpdateLicenseCode"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> LicenseCodeData(CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        message = await httpClient.GetAsync($"{Domain}{"/api/License/LicenseCodeData"}", token)
                            .ConfigureAwait(false);
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        if (result is not null) {
                            var licenseCodeItemInfoModels = JsonConvert.DeserializeObject<List<LicenseCodeItemInfoModel>>(result?.Data?.ToString() ?? string.Empty);
                            if (licenseCodeItemInfoModels?.Any() == true) {
                                return new KeyValuePair<bool, object>(true, licenseCodeItemInfoModels);
                            }
                            else {
                                return new KeyValuePair<bool, object>(false, "未查询到相关信息");
                            }
                        }
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> ExtendLicenseCodeValidity(string licenseCode, DateTime expirationDate, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        licenseCode = licenseCode,
                        expirationDate = expirationDate,
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/License/ExtendLicenseCodeValidity"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> FreezeLicenseCode(string licenseCode, bool isFreeze, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        licenseCode = licenseCode,
                        isFreeze = isFreeze,
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/License/FreezeLicenseCode"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> DownloadLicenseFile(string licenseCode, string machineCode, string remarks, CancellationToken token = default) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        licenseCode = licenseCode,
                        machineCode = machineCode,
                        remarks = remarks
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/License/DownloadLicenseFile"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> UnbindMachineCode(string licenseCode, string machineCode, CancellationToken token) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", token, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        licenseCode = licenseCode,
                        machineCode = machineCode,
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/License/UnbindMachineCode"}", content, token)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", token, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task<KeyValuePair<bool, object>> UpdateTenantLicenseMaxCount(string userCode, long licensePermissionTemplateInfoId, int maxLicenseCodeCount,
            CancellationToken cancellationToken) {
            var invokeAsync = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", cancellationToken, "token");
            if (!string.IsNullOrEmpty(invokeAsync)) {
                try {
                    //组包

                    var requestJson = JsonConvert.SerializeObject(new {
                        userCode = userCode,
                        licensePermissionTemplateInfoId = licensePermissionTemplateInfoId,
                        maxLicenseCodeCount = maxLicenseCodeCount,
                    });

                    using (var httpClient = _httpClientFactory.CreateClient("INSURANCE")) {
                        httpClient.Timeout = TimeSpan.FromSeconds(20);
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {invokeAsync}");
                        HttpResponseMessage message;
                        await using (Stream dataStream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson))) {
                            using (HttpContent content = new StreamContent(dataStream)) {
                                content.Headers.Add("Content-Type", "application/json");
                                message = await httpClient.PostAsync($"{Domain}{"/api/User/UpdateTenantLicenseMaxCount"}", content, cancellationToken)
                                    .ConfigureAwait(false);
                            }
                        }
                        string httpResult;
                        switch (message.StatusCode) {
                            case HttpStatusCode.OK: {
                                    using (message) {
                                        httpResult = await message.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                                    }
                                    break;
                                }
                            case HttpStatusCode.NotFound:
                                return new KeyValuePair<bool, object>(false, $"该地址不存在!");

                            case HttpStatusCode.Unauthorized:
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, "token");
                                return new KeyValuePair<bool, object>(false, $"用户未登录!");

                            default:
                                httpResult = $"{message}";
                                break;
                        }

                        //解码
                        var result = JsonConvert.DeserializeObject<ApiResult>(httpResult);
                        return new KeyValuePair<bool, object>(result?.Result ?? false, result ?? new ApiResult());
                    }
                }
                catch (HttpRequestException) {
                    return new KeyValuePair<bool, object>(false, "Http访问异常!");
                }
                catch (AggregateException) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
                catch (TaskCanceledException) {
                    return new KeyValuePair<bool, object>(false, "接口访问返回超时!");
                }
                catch (Exception) {
                    return new KeyValuePair<bool, object>(false, "接口访问异常!");
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "用户未登录");
            }
        }

        public async Task LogOut(CancellationToken cancellationToken) {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, "token");
        }

        public StreamContent? ImageToStreamContent(Image image, string paramName, string fileName) {
            try {
                using var memoryStream = new MemoryStream();
                image.Save(memoryStream, ImageFormat.Png);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var clonedStream = new MemoryStream();
                memoryStream.CopyTo(clonedStream);
                clonedStream.Seek(0, SeekOrigin.Begin);

                var streamContent = new StreamContent(clonedStream);
                streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
                    Name = paramName,
                    FileName = fileName
                };

                return streamContent;
            }
            catch (Exception e) {
                return null;
            }
            finally {
                image.Dispose();
            }
        }
    }
}