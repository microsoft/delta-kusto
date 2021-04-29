using DeltaKustoLib;
using DeltaKustoLib.KustoModel;
using DeltaKustoLib.SchemaObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Database
{
    public class JsonDatabaseProvider : IDatabaseProvider
    {
        private readonly ITracer _tracer;
        private readonly IFileGateway _fileGateway;
        private readonly string _jsonFilePath;

        public JsonDatabaseProvider(
            ITracer tracer,
            IFileGateway fileGateway,
            string jsonFilePath)
        {
            _tracer = tracer;
            _fileGateway = fileGateway;
            _jsonFilePath = jsonFilePath;
        }

        async Task<DatabaseModel> IDatabaseProvider.RetrieveDatabaseAsync(
            CancellationToken ct)
        {
            _tracer.WriteLine(true, "Retrieve scripts DB start");

            var schemaText = await _fileGateway.GetFileContentAsync(_jsonFilePath, ct);
            var rootSchema = RootSchema.FromJson(schemaText);

            if (rootSchema.Databases.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Schema doesn't contain a single database:  '{schemaText}'");
            }

            var dbSchema = rootSchema.Databases.First().Value;
            var model = DatabaseModel.FromDatabaseSchema(dbSchema);

            return model;
        }
    }
}