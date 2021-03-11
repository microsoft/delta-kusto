using DeltaKustoLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.TokenProvider
{
    internal class LoginTokenProvider : ITokenProvider
    {
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _secret;

        private ConcurrentDictionary<Uri, string> _tokenCache = new ConcurrentDictionary<Uri, string>();

        public LoginTokenProvider(string tenantId, string clientId, string secret)
        {
            _tenantId = tenantId;
            _clientId = clientId;
            _secret = secret;
        }

        async Task<string> ITokenProvider.GetTokenAsync(Uri clusterUri)
        {
            if (_tokenCache.ContainsKey(clusterUri))
            {
                return _tokenCache[clusterUri];
            }
            else
            {
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
                        }));
                    var responseText = await response.Content.ReadAsStringAsync();

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
                        throw new DeltaException($"Can deserialize token in authentication response:"
                            + $"  '{responseText}'");
                    }
                    var accessToken = tokenMap["access_token"];

                    _tokenCache[clusterUri] = accessToken;

                    return accessToken;
                }
            }
        }
    }
}