namespace DeltaKustoIntegration.TokenProvider
{
    internal class LoginTokenProvider : ITokenProvider
    {
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _secret;

        public LoginTokenProvider(string tenantId, string clientId, string secret)
        {
            _tenantId = tenantId;
            _clientId = clientId;
            _secret = secret;
        }
    }
}