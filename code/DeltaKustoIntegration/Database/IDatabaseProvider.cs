using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Database
{
    public interface IDatabaseProvider
    {
        Task<DatabaseModel> RetrieveDatabaseAsync(CancellationToken ct = default);
    }
}