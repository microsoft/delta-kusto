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
            var targetDbs = await GetDbsAsync(1);
            var targetDb = targetDbs.First();

            try
            {
                var toFile = "FailIfDataLoss/target.kql";
                var overrides = ImmutableArray<(string path, string value)>
                    .Empty
                    .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                    .Add(("jobs.main.target.adx.database", targetDb));

                await PrepareDbAsync(toFile, targetDb);
                await RunParametersAsync("FailIfDataLoss/no-fail.json", overrides);

                //  We just test that this doesn't fail
            }
            finally
            {
                ReleaseDbs(targetDbs);
            }
        }

        [Fact]
        public async Task TestFailIfDrops()
        {
            var targetDbs = await GetDbsAsync(1);
            var targetDb = targetDbs.First();

            try
            {
                var toFile = "FailIfDataLoss/target.kql";
                var overrides = ImmutableArray<(string path, string value)>
                    .Empty
                    .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                    .Add(("jobs.main.target.adx.database", targetDb))
                    .Append(("failIfDataLoss", "true"));

                await PrepareDbAsync(toFile, targetDb);

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
            finally
            {
                ReleaseDbs(targetDbs);
            }
        }
    }
}