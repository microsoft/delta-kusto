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

namespace DeltaKustoAdxIntegrationTest
{
    public class FailIfDataLossTest : AdxIntegrationTestBase
    {
        public FailIfDataLossTest(AdxDbFixture adxDbFixture) : base(adxDbFixture)
        {
        }

        [Fact]
        public async Task TestFailIfDropsNoDrop()
        {
            var toFile = "FailIfDataLoss/target.kql";
            var targetDbName = await InitializeDbAsync();
            var overrides = ImmutableArray<(string path, string value)>
                .Empty
                .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                .Add(("jobs.main.target.adx.database", targetDbName));

            await PrepareDbAsync(toFile, targetDbName);
            await RunParametersAsync("FailIfDataLoss/no-fail.json", overrides);

            //  We just test that this doesn't fail
        }

        [Fact]
        public async Task TestFailIfDrops()
        {
            var toFile = "FailIfDataLoss/target.kql";
            var targetDbName = await InitializeDbAsync();
            var overrides = ImmutableArray<(string path, string value)>
                .Empty
                .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                .Add(("jobs.main.target.adx.database", targetDbName))
                .Append(("failIfDataLoss", "true"));

            await PrepareDbAsync(toFile, targetDbName);

            try
            {
                //  The "Main" will return non-zero which will throw an exception
                var parameters =
                    await RunParametersAsync("FailIfDataLoss/no-fail.json", overrides);

                Assert.True(parameters.FailIfDataLoss);
                Assert.False(true, "Should have thrown by now");
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}