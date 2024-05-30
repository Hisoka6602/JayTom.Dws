using System;
using MediatR;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Domain.Attributes;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using Microsoft.Extensions.DependencyInjection;

namespace JayTom.Dws.Domain.Service.Client.Interface {

    public class PackageApiService : BaseMediator, IPackageApiService {
        private readonly IConfigRepository _configRepository;
        private readonly IServiceProvider _serviceProvider;

        private ApiSettingsDto _apiSettingsDto = new();
        public IPackageApi? PackageApi { get; private set; }

        public PackageApiService(IMediator mediator,
            IConfigRepository configRepository, IServiceProvider serviceProvider) : base(mediator) {
            _configRepository = configRepository;
            _serviceProvider = serviceProvider;
        }

        public override async Task Handle(GenericMessage request, CancellationToken cancellationToken = default) {
            if (request is {
                Type: GenericMessageType.System,
                Content: SystemMessageInfo info
            }) {
                switch (info.Type) {
                    //读组包设置
                    case SystemMessageType.Start:
                        _apiSettingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>("ApiSettings", cancellationToken) ?? new ApiSettingsDto();
                        PackageApi = CreateInstance();
                        //设置接口参数
                        // PackageApi.SetInterfaceParams()
                        break;

                    case SystemMessageType.Stop: {
                            break;
                        }
                }
            }
            else if (request is {
                Type: GenericMessageType.Setting,
                Content: SettingMessageInfo { SettingsName: "ApiSettings" }
            }) {
                _apiSettingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>("ApiSettings", cancellationToken) ?? new ApiSettingsDto();
                PackageApi = CreateInstance();
                //设置接口参数
                // PackageApi.SetInterfaceParams()
            }

            //落格事件
            //集包事件
        }

        public event EventHandler<UploadResponse>? UploadCompleted;

        public IPackageApi? CreateInstance() {
            //读选中接口
            try {
                var withAttribute = FindClassesWithAttribute(Assembly.GetExecutingAssembly(), typeof(PackageApiAttribute));
                var type = withAttribute?.FirstOrDefault(f =>
                    f.GetCustomAttribute<PackageApiAttribute>()
                        ?.Name.Equals(_apiSettingsDto.ApiName) == true);

                if (type is not null) {
                    var constructor = type.GetConstructors().FirstOrDefault();
                    if (constructor != null) {
                        var parameters = constructor.GetParameters();
                        var args = parameters.Select(p => _serviceProvider.GetService(p.ParameterType)).ToArray();
                        var instance = Activator.CreateInstance(type, args);
                        return instance as IPackageApi;
                    }
                    else {
                        NLog.LogManager.GetCurrentClassLogger().Error($"No constructor found for type {type.FullName}");
                    }
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return null;
        }

        public Task<bool> EnqueueUploadAsync(ApiRequestType type, PackageInfoModel info, object? other = null, int delay = 0, CancellationToken token = default) {
            //添加到队列,添加各种计时器
            throw new NotImplementedException();
        }

        public async Task<UploadResponse?> ImmediateUploadAsync(ApiRequestType type, PackageInfoModel info, object? other = null,
            CancellationToken token = default) {
            if (PackageApi is not null) {
                var response = await PackageApi.RequestSortCallbackAsync(info, other, token);
                await base.PublishMessage(new GenericMessage() {
                    Type = GenericMessageType.Api,
                    Content = new ApiMessageInfo {
                        ApiRequestType = ApiRequestType.ExitRequest,
                        MethodName = "RequestSortCallbackAsync",
                        UploadResponse = response
                    }
                }, token);
                return response;
            }
            else {
                NLog.LogManager.GetCurrentClassLogger().Error($"接口未实例化");
            }

            return null;
        }

        public IEnumerable<Type>? ListAvailableEndpoints() {
            return FindClassesWithAttribute(Assembly.GetExecutingAssembly(), typeof(PackageApiAttribute));
        }
    }
}