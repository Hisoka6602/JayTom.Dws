using System.Net;
using System.Text;
using JayTom.Dws.Abstractions.Results;
using Newtonsoft.Json;

namespace JayTom.Dws.Integrations.License;

/// <summary>通过远程授权服务创建、激活并下载客户端授权文件。</summary>
public sealed class DefaultClientLicenseApi : IClientLicenseApi {
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>获取授权服务根地址。</summary>
    public static string Domain { get; private set; } = "http://api.wxck.top";

    /// <summary>创建客户端授权接口。</summary>
    public DefaultClientLicenseApi(IHttpClientFactory httpClientFactory) {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public Task<OperationResult<ApiResult>> CreateAuthorization(
        string licenseCode,
        string machineCode,
        string remarks,
        CancellationToken token = default) =>
        PostAuthorizationAsync(
            "/api/License/CreateAuthorization",
            licenseCode,
            machineCode,
            remarks,
            "license_authorization_rejected",
            token);

    /// <inheritdoc />
    public Task<OperationResult<ApiResult>> ActivateAuthorization(
        string licenseCode,
        string machineCode,
        string remarks,
        CancellationToken token = default) =>
        PostAuthorizationAsync(
            "/api/License/ActivateAuthorization",
            licenseCode,
            machineCode,
            remarks,
            "license_activation_rejected",
            token);

    /// <inheritdoc />
    public async Task<OperationResult<string>> DownloadFileAsync(
        string fileUrl,
        string savePath,
        CancellationToken token = default) {
        string? temporaryPath = null;
        try {
            if (string.IsNullOrWhiteSpace(fileUrl) || string.IsNullOrWhiteSpace(savePath)) {
                return OperationResult<string>.Failure(
                    "license_download_invalid_path",
                    "下载地址或保存路径为空");
            }

            using var httpClient = _httpClientFactory.CreateClient(ApiHttpClientNames.ExternalApi);
            using var response = await httpClient.GetAsync(
                fileUrl,
                HttpCompletionOption.ResponseHeadersRead,
                token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var directory = Path.GetDirectoryName(Path.GetFullPath(savePath));
            if (string.IsNullOrWhiteSpace(directory)) {
                return OperationResult<string>.Failure(
                    "license_download_invalid_directory",
                    "无法确定授权文件目录");
            }

            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(
                directory,
                $".{Path.GetFileName(savePath)}.{Guid.NewGuid():N}.tmp");
            await using (var contentStream = await response.Content
                             .ReadAsStreamAsync(token)
                             .ConfigureAwait(false)) {
                await using var fileStream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous | FileOptions.WriteThrough);
                await contentStream.CopyToAsync(fileStream, token).ConfigureAwait(false);
                await fileStream.FlushAsync(token).ConfigureAwait(false);
            }

            File.Move(temporaryPath, savePath, true);
            temporaryPath = null;
            return OperationResult<string>.Success(savePath);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) {
            throw;
        }
        catch (Exception exception) {
            return OperationResult<string>.Failure(
                "license_download_failed",
                exception.Message);
        }
        finally {
            DeleteTemporaryFile(temporaryPath);
        }
    }

    private async Task<OperationResult<ApiResult>> PostAuthorizationAsync(
        string endpoint,
        string licenseCode,
        string machineCode,
        string remarks,
        string rejectedErrorCode,
        CancellationToken token) {
        try {
            var requestJson = JsonConvert.SerializeObject(new {
                licenseCode,
                machineCode,
                remarks
            });
            using var httpClient = _httpClientFactory.CreateClient(ApiHttpClientNames.ExternalApi);
            httpClient.Timeout = TimeSpan.FromSeconds(20);
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            using var response = await httpClient.PostAsync(
                $"{Domain}{endpoint}",
                content,
                token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound) {
                return OperationResult<ApiResult>.Failure(
                    "license_endpoint_not_found",
                    "授权服务地址不存在");
            }

            var responseContent = await response.Content
                .ReadAsStringAsync(token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) {
                return OperationResult<ApiResult>.Failure(
                    "license_http_status_error",
                    $"授权服务返回 HTTP {(int)response.StatusCode}: {responseContent}");
            }

            var result = JsonConvert.DeserializeObject<ApiResult>(responseContent);
            if (result is null) {
                return OperationResult<ApiResult>.Failure(
                    "license_response_invalid",
                    "授权服务返回了无法解析的响应");
            }

            return result.IsSuccess
                ? OperationResult<ApiResult>.Success(result)
                : OperationResult<ApiResult>.Failure(
                    rejectedErrorCode,
                    string.IsNullOrWhiteSpace(result.Message) ? "授权操作被拒绝" : result.Message,
                    result);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) {
            throw;
        }
        catch (TaskCanceledException) {
            return OperationResult<ApiResult>.Failure(
                "license_timeout",
                "授权服务响应超时");
        }
        catch (HttpRequestException exception) {
            return OperationResult<ApiResult>.Failure(
                "license_http_error",
                exception.Message);
        }
        catch (Exception exception) {
            return OperationResult<ApiResult>.Failure(
                "license_unknown_error",
                exception.Message);
        }
    }

    private static void DeleteTemporaryFile(string? temporaryPath) {
        if (temporaryPath is null) {
            return;
        }

        try {
            File.Delete(temporaryPath);
        }
        catch (IOException) {
            // 临时文件由后续清理任务处理。
        }
    }
}
