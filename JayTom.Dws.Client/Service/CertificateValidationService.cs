using System;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using JayTom.Dws.Interface;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace JayTom.Dws.Client.Service {

    public class CertificateValidationService : ICertificateValidationService {
        private readonly INetworkTime _networkTime;

        public CertificateValidationService(INetworkTime networkTime) {
            _networkTime = networkTime;
        }

        public async Task<bool> ValidateTime() {
            var dateTime = await _networkTime.GetTime();
            return Convert.ToDateTime("2024-11-01 00:00:00").CompareTo(dateTime) >= 0;
        }

        public Task<bool> ValidateCertificate() {
            return Task.FromResult(false);
        }

        public Task<bool> ValidateMachineCode() {
            return Task.FromResult(false);
        }
    }
}