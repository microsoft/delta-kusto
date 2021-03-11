using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib.SchemaObjects;
using System;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Kusto
{
    /// <summary>
    /// Basically wraps REST APIs <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/api/rest/"/>.
    /// </summary>
    internal class KustoManagementGateway : IKustoManagementGateway
    {
        private readonly string _clusterUri;
        private readonly string _database;
        private readonly ITokenProvider _tokenProvider;

        public KustoManagementGateway(string clusterUri, string database, ITokenProvider tokenProvider)
        {
            _clusterUri = clusterUri;
            _database = database;
            _tokenProvider = tokenProvider;
        }

        async Task<DatabaseSchema> IKustoManagementGateway.GetDatabaseSchemaAsync()
        {
            var token = await _tokenProvider.GetTokenAsync(_clusterUri);

            //httpClient.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Bearer", access_token);
            throw new NotImplementedException();
        }
    }
}