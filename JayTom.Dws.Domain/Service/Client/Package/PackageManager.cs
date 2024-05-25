using System;
using MediatR;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq.Expressions;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;

namespace JayTom.Dws.Domain.Service.Client.Package {

    /// <summary>
    /// 包裹管理器
    /// </summary>
    public class PackageManager : BaseMediator, IPackageManager {
        private readonly IConfigRepository _configRepository;
        private ConcurrentDictionary<DateTime, PackageInfoModel> _packageInfos = new();
        private SemaphoreSlim _packageSlim = new(1);
        private CreatePackageSettingsDto _createPackageSettingsDto = new();

        public PackageManager(IMediator mediator,
            IConfigRepository configRepository) : base(mediator) {
            _configRepository = configRepository;
        }

        public override async Task Handle(GenericMessage request, CancellationToken cancellationToken = default) {
            //程序启动

            if (request is {
                Type: GenericMessageType.System,
                Content: SystemMessageInfo info
            }) {
                switch (info.Type) {
                    //读组包设置
                    case SystemMessageType.Start:
                        _createPackageSettingsDto = await _configRepository.FirstOrDefaultEntity<CreatePackageSettingsDto>(
                                                        "CreatePackageSettings", cancellationToken) ??
                                                    new CreatePackageSettingsDto();
                        break;

                    case SystemMessageType.Stop: {
                            if (_createPackageSettingsDto.ClearPackageQueueOnStop) {
                                try {
                                    await _packageSlim.WaitAsync(cancellationToken);
                                    _packageInfos.Clear();
                                }
                                finally {
                                    _packageSlim.Release();
                                }
                            }

                            break;
                        }
                }
            }

            throw new NotImplementedException();

            //更新包裹各种信息
        }

        public event EventHandler<PackageInfoMessage>? PackageCreated;

        public event EventHandler<PackageInfoMessage>? PackageIntercepted;

        public event EventHandler<PackageInfoMessage>? PackageRemoved;

        public event EventHandler? PackagesCleared;

        public event EventHandler<PackageInfoMessage>? PackageUpdated;

        public event EventHandler<PackageInfoMessage>? PackageAppended;

        public Task<KeyValuePair<bool, PackageInfoModel?>> CreatePackage(PackageCreationMethodsEnum packageCreationMethod, long packageTimestamped, PackageInfoModel packageInfo) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, PackageInfoModel?>> RemovePackage(PackageInfoModel packageInfo) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, PackageInfoModel?>> RemovePackage(long packageTimestamped) {
            throw new NotImplementedException();
        }

        public Task<bool> ClearPackages() {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, PackageInfoModel?>> UpdatePackage(Expression<Func<PackageInfoModel, bool>> where, BasePackageForeignKeyInfoModel info) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, PackageInfoModel?>> AppendPackageInfo(Expression<Func<PackageInfoModel, bool>> where, BasePackageForeignKeyInfoModel info) {
            throw new NotImplementedException();
        }

        public Task<PackageInfoModel>? FindPackage(Expression<Func<PackageInfoModel, bool>> where, CancellationToken token) {
            throw new NotImplementedException();
        }

        protected virtual async void OnPackageCreated(PackageInfoMessage e) {
            await PublishMessage(new GenericMessage() {
                Type = GenericMessageType.Packaging,
                Content = e
            });
            PackageCreated?.Invoke(this, e);
        }

        protected virtual async void OnPackageIntercepted(PackageInfoMessage e) {
            await PublishMessage(new GenericMessage() {
                Type = GenericMessageType.Packaging,
                Content = e
            });
            PackageIntercepted?.Invoke(this, e);
        }

        protected virtual async void OnPackageRemoved(PackageInfoMessage e) {
            await PublishMessage(new GenericMessage() {
                Type = GenericMessageType.Packaging,
                Content = e
            });
            PackageRemoved?.Invoke(this, e);
        }

        protected virtual async void OnPackagesCleared() {
            await Task.Yield();
            PackagesCleared?.Invoke(this, EventArgs.Empty);
        }

        protected virtual async void OnPackageUpdated(PackageInfoMessage e) {
            await PublishMessage(new GenericMessage() {
                Type = GenericMessageType.Packaging,
                Content = e
            });
            PackageUpdated?.Invoke(this, e);
        }

        protected virtual async void OnPackageAppended(PackageInfoMessage e) {
            await PublishMessage(new GenericMessage() {
                Type = GenericMessageType.Packaging,
                Content = e
            });
            PackageAppended?.Invoke(this, e);
        }
    }
}