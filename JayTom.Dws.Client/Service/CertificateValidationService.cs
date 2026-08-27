using System;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using JayTom.Dws.Integrations;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace JayTom.Dws.Client.Service
{

    public class CertificateValidationService : ICertificateValidationService
    {
        private readonly INetworkTime _networkTime;

        public CertificateValidationService(INetworkTime networkTime)
        {
            _networkTime = networkTime;
        }

        public async Task<bool> ValidateTimeAsync(CancellationToken cancellationToken = default)
        {
            var dateTime = await _networkTime.GetLocalTimeAsync(cancellationToken);
            return new DateTime(2024, 11, 1).CompareTo(dateTime.LocalDateTime) >= 0;
        }

        public Task<bool> ValidateCertificateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> ValidateMachineCodeAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}
