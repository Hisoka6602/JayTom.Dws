using System;
using MediatR;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.EventMediators;

namespace JayTom.Dws.Domain.Service.Client.Interface {

    public class PackageApiService : BaseMediator, IPackageApiService {

        public PackageApiService(IMediator mediator) : base(mediator) {
        }

        public override Task Handle(GenericMessage request, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
    }
}