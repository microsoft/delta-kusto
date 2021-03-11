using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib.SchemaObjects;
using System;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Kusto
{
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

        Task<DatabaseSchema> IKustoManagementGateway.GetDatabaseSchemaAsync()
        {
            throw new NotImplementedException();
        }
    }
}