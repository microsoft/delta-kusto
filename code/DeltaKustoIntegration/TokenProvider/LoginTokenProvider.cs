using DeltaKustoLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.TokenProvider
{
    internal class LoginTokenProvider : ITokenProvider
    {
        private readonly ITracer _tracer;
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _secret;

        private ConcurrentDictionary<Uri, string> _tokenCache = new ConcurrentDictionary<Uri, string>();

        public LoginTokenProvider(
            ITracer tracer,
            string tenantId,
            string clientId,
            string secret)
        {
            _tracer = tracer;
            _tenantId = tenantId;
            _clientId = clientId;
            _secret = secret;
        }

        async Task<string> ITokenProvider.GetTokenAsync(
            Uri clusterUri,
            CancellationToken ct)
        {
            if (_tokenCache.ContainsKey(clusterUri))
            {
                _tracer.WriteLine(true, "Token was cached");

                return _tokenCache[clusterUri];
            }
            else
            {
                try
                {
                    return await RetrieveTokenAsync(clusterUri, ct);
                }
                catch (Exception ex)
                {
                    throw new DeltaException("Issue retrieving token", ex);
                }
            }
        }

        private async Task<string> RetrieveTokenAsync(Uri clusterUri, CancellationToken ct)
        {
            _tracer.WriteLine(true, "LoginTokenProvider.RetrieveTokenAsync start");

            //  Implementation of https://docs.microsoft.com/en-us/azure/data-explorer/kusto/api/rest/request#examples
            using (var client = new HttpClient())
            {
                var loginUrl = $"https://login.microsoftonline.com/{_tenantId}/oauth2/token";
                var response = await client.PostAsync(
                    loginUrl,
                    new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string?, string?>("client_id", _clientId),
                        new KeyValuePair<string?, string?>("client_secret", _secret),
                        new KeyValuePair<string?, string?>("resource", clusterUri.ToString()),
                        new KeyValuePair<string?, string?>("grant_type", "client_credentials")
                    }),
                    ct);

                _tracer.WriteLine(true, "LoginTokenProvider.RetrieveTokenAsync retrieve payload");
                
                var responseText =
                    await response.Content.ReadAsStringAsync(ct);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new DeltaException(
                        $"Authentication failed for cluster URI '{clusterUri}' "
                        + $"with status code '{response.StatusCode}' "
                        + $"and payload {responseText}");
                }

                var tokenMap = JsonSerializer.Deserialize<IDictionary<string, string>>(responseText);

                if (tokenMap == null || !tokenMap.ContainsKey("access_token"))
                {
                    throw new DeltaException(
                        $"Can't deserialize token in authentication response:"
                        + $"  '{responseText}'");
                }
                var accessToken = tokenMap["access_token"];

                _tokenCache[clusterUri] = accessToken;

                _tracer.WriteLine(true, "LoginTokenProvider.RetrieveTokenAsync end");

                return accessToken;
            }
        }
    }
}