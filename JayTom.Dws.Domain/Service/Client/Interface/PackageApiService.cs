using System;
using MediatR;
using System.Linq;
using System.Text;
using System.Reflection;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Domain.Attributes;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Domain.Service.Client.Interface {

    public class PackageApiService : BaseMediator, IPackageApiService {
        private readonly IConfigRepository _configRepository;
        private ApiSettingsDto _apiSettingsDto = new();
        public IPackageApi? PackageApi { get; private set; }

        public PackageApiService(IMediator mediator,
            IConfigRepository configRepository) : base(mediator) {
            _configRepository = configRepository;

            /*if (type.GetConstructor(Type.EmptyTypes) != null) {
                var instance = Activator.CreateInstance(type);
                instances.Add(instance);
            }*/
        }

        public override async Task Handle(GenericMessage request, CancellationToken cancellationToken = default) {
            if (request is {
                Type: GenericMessageType.System,
                Content: SystemMessageInfo info
            }) {
                switch (info.Type) {
                    //读组包设置
                    case SystemMessageType.Start:

                        //读选中接口
                        _apiSettingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>("ApiSettings", cancellationToken) ?? new ApiSettingsDto();

                        var withAttribute = FindClassesWithAttribute(Assembly.GetExecutingAssembly(), typeof(PackageApiAttribute));
                        var type = withAttribute?.FirstOrDefault(f =>
                            f.GetCustomAttribute<PackageApiAttribute>()
                                ?.Name.Equals(_apiSettingsDto.ApiName) == true);

                        if (type is not null) {
                        }

                        break;

                    case SystemMessageType.Stop: {
                            break;
                        }
                }
            }
        }

        public event EventHandler<UploadResponse>? UploadCompleted;

        public IPackageApi CreateInstance() {
            throw new NotImplementedException();
        }

        public Task<UploadResponse> RequestSortCallbackAsync(PackageInfoModel info, object? other = null, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<bool> EnqueueUploadAsync(PackageInfoModel info, object? other = null, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public IEnumerable<IPackageApi> ListAvailableEndpoints() {
            throw new NotImplementedException();
        }
    }
}