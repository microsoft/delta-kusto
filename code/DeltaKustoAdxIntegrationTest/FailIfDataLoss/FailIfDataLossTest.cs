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

namespace DeltaKustoAdxIntegrationTest.FailIfDataLoss
{
    public class FailIfDataLossTest : AdxIntegrationTestBase
    {
        [Fact]
        public async Task TestFailIfDropsNoDrop()
        {
            using (var targetDb = await InitializeDbAsync())
            {
                var toFile = "FailIfDataLoss/target.kql";
                var overrides = ImmutableArray<(string path, string value)>
                    .Empty
                    .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                    .Add(("jobs.main.target.adx.database", targetDb.Name));

                await PrepareDbAsync(toFile, targetDb.Name);
                await RunParametersAsync("FailIfDataLoss/no-fail.json", overrides);

                //  We just test that this doesn't fail
            }
        }

        [Fact]
        public async Task TestFailIfDrops()
        {
            using (var targetDb = await InitializeDbAsync())
            {
                var toFile = "FailIfDataLoss/target.kql";
                var overrides = ImmutableArray<(string path, string value)>
                    .Empty
                    .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                    .Add(("jobs.main.target.adx.database", targetDb.Name))
                    .Append(("failIfDataLoss", "true"));

                await PrepareDbAsync(toFile, targetDb.Name);

                try
                {
                    //  The "Main" will return non-zero which will throw an exception
                    var parameters =
                        await RunParametersAsync("FailIfDataLoss/no-fail.json", overrides);

                    Assert.True(parameters.FailIfDataLoss);
                    Assert.Fail("Should have thrown by now");
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}