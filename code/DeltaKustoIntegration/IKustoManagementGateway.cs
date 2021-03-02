using DeltaKustoLib.SchemaObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoIntegration
{
    public interface IKustoManagementGateway
    {
        Task<DatabaseSchema> GetDatabaseSchemaAsync();
    }
}