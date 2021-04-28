﻿using System.Net.Http;
using System.Threading.Tasks;
using VaultSharp.Core;
using VaultSharp.V1.Commons;

namespace VaultSharp.V1.SecretsEngines.PKI
{
    internal class PKISecretsEngineProvider : IPKISecretsEngine
    {
        private readonly Polymath _polymath;

        private string MountPoint
        {
            get 
            {
                _polymath.VaultClientSettings.SecretEngineMountPoints.TryGetValue(nameof(SecretsEngineDefaultPaths.PKI), out var mountPoint);
                return mountPoint ?? SecretsEngineDefaultPaths.PKI;
            }
        }

        public PKISecretsEngineProvider(Polymath polymath)
        {
            _polymath = polymath;
        }

        public async Task<Secret<CertificateCredentials>> GetCredentialsAsync(string pkiRoleName, CertificateCredentialsRequestOptions certificateCredentialRequestOptions, string pkiBackendMountPoint = null, string wrapTimeToLive = null)
        {
            Checker.NotNull(pkiRoleName, "pkiRoleName");
            Checker.NotNull(certificateCredentialRequestOptions, "certificateCredentialRequestOptions");

            var result = await _polymath.MakeVaultApiRequest<Secret<CertificateCredentials>>(pkiBackendMountPoint ?? MountPoint, "/issue/" + pkiRoleName, HttpMethod.Post, certificateCredentialRequestOptions, wrapTimeToLive: wrapTimeToLive).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
            result.Data.CertificateFormat = certificateCredentialRequestOptions.CertificateFormat;

            return result;
        }

        public async Task<Secret<RevokeCertificateResponse>> RevokeCertificateAsync(string serialNumber, string pkiBackendMountPoint = null)
        {
            Checker.NotNull(serialNumber, "serialNumber");

            return await _polymath.MakeVaultApiRequest<Secret<RevokeCertificateResponse>>(pkiBackendMountPoint ?? MountPoint, "/revoke", HttpMethod.Post, new { serial_number = serialNumber }).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task TidyAsync(CertificateTidyRequest certificateTidyRequest = null, string pkiBackendMountPoint = null)
        {
            var newRequest = certificateTidyRequest ?? new CertificateTidyRequest();

            await _polymath.MakeVaultApiRequest(pkiBackendMountPoint ?? MountPoint, "/tidy", HttpMethod.Post, newRequest).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task<RawCertificateData> ReadCACertificateAsync(CertificateFormat certificateFormat = CertificateFormat.der, string pkiBackendMountPoint = null)
        {
            var format = certificateFormat == CertificateFormat.pem ? "/" + CertificateFormat.pem : string.Empty;
            var outputFormat = certificateFormat == CertificateFormat.pem
                ? CertificateFormat.pem
                : CertificateFormat.der;

            var result = await _polymath.MakeVaultApiRequest<string>(pkiBackendMountPoint ?? MountPoint, "/ca" + format, HttpMethod.Get, rawResponse: true).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
            
            return new RawCertificateData
            {
                CertificateContent = result,
                EncodedCertificateFormat = outputFormat
            };
        }
    }
}