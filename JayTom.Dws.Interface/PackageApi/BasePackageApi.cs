using MediatR;
using System.Reflection;
using System.ComponentModel;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Service;
using JayTom.Dws.Domain.Attributes;
using JayTom.Dws.Domain.EventMediators;

namespace JayTom.Dws.Interface.PackageApi {

    [PackageApi(DisplayName = "基础接口", Name = "基础接口")]
    public abstract class BasePackageApi : BaseMediator, IPackageApi {
        private readonly IHttpClientFactory _httpClient;

        protected BasePackageApi(IMediator mediator, IHttpClientFactory httpClient) : base(mediator) {
            _httpClient = httpClient;
        }

        public Task<UploadResponse> RequestSortCallbackAsync(PackageInfoModel info, object? other = null, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<UploadResponse> SubmitSortReportAsync(PackageInfoModel info, object? other = null, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<bool> UploadImageAsync(PackageInfoModel info, object? other = null, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public bool SetInterfaceParams(BaseInterfaceParams @params, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public bool SetInterfaceParams(string paramsJson, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<UploadResponse> PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems,
            object? other = null, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        private async Task<T?> ExecuteAsync<T>(string methodName, object[] parameters) {
            var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (method == null) throw new InvalidOperationException($"Method {methodName} not found");

            var result = method.Invoke(this, parameters);

            if (result is Task<T> task) {
                return await task;
            }

            return (T)result;
        }
    }
}