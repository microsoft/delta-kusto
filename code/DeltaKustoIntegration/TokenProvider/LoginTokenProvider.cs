using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.TokenProvider
{
    internal class LoginTokenProvider : ITokenProvider
    {
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _secret;

        private IDictionary<string, string> _tokenCache = new Dictionary<string, string>();

        public LoginTokenProvider(string tenantId, string clientId, string secret)
        {
            _tenantId = tenantId;
            _clientId = clientId;
            _secret = secret;
        }

        async Task<string> ITokenProvider.GetTokenAsync(string clusterUri)
        {
            if (_tokenCache.ContainsKey(clusterUri))
            {
                return _tokenCache[clusterUri];
            }
            else
            {
                //  Implementation of https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow#first-case-access-token-request-with-a-shared-secret
                using (var client = new HttpClient())
                {
                    var loginUrl = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token";
                    var content = $"client_id={_clientId}\n&"
                        + $"scope={WebUtility.UrlEncode(clusterUri)}\n&"
                        + $"client_secret={WebUtility.UrlEncode(_secret)}"
                        + $"\n&grant_type=client_credentials";
                    var request = new HttpRequestMessage(HttpMethod.Post, loginUrl)
                    {
                        Content = new StringContent(
                            content,
                            null,
                            "application/x-www-form-urlencoded")
                    };
                    var response = await client.SendAsync(request);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new DeltaException(
                            $"Authentication failed for cluster URI '{clusterUri}':  {response.StatusCode}");
                    }

                    var responseText = response.Content.ReadAsStringAsync();

                    throw new NotImplementedException();
                }
            }
        }
    }
}