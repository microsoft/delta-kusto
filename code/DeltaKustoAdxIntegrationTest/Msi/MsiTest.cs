using DeltaKustoIntegration.Database;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest.Msi
{
    public class MsiTest : AdxIntegrationTestBase
    {
        [Fact]
        public async Task TestSystemManagedMsi()
        {
            try
            {
                var targetDbName = await InitializeDbAsync();
                var overrides = ImmutableArray<(string path, string value)>
                    .Empty
                    .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                    .Add(("jobs.main.target.adx.database", targetDbName));

                await RunParametersAsync("Msi/SystemAssigned/sys-assigned.yaml", overrides);
            }
            //  This should be thrown when we're in-proc
            catch (DeltaException)
            {
            }
            //  This should be thrown when we're out-of-proc
            catch (InvalidOperationException)
            {
            }
        }
    }
}