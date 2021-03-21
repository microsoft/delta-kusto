using DeltaKustoIntegration.Database;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest.Functions
{
    public class FailIfDropTest : AdxIntegrationTestBase
    {
        [Fact]
        public async Task TestFailIfDropsNoDrop()
        {
            var toFile = "FailIfDrops/target.kql";

            await CleanDatabasesAsync();
            await PrepareDbAsync(toFile, false);

            await RunParametersAsync("FailIfDrops/no-fail.json", TargetDbOverrides);

            //  We just test that this doesn't fail
        }

        [Fact]
        public async Task TestFailIfDrops()
        {
            var toFile = "FailIfDrops/target.kql";

            await CleanDatabasesAsync();
            await PrepareDbAsync(toFile, false);

            var overrides = TargetDbOverrides
                .Append(("failIfDrops", true));

            try
            {
                //  The "Main" will return non-zero which will throw an exception
                var parameters = await RunParametersAsync("FailIfDrops/no-fail.json", overrides);

                Assert.True(parameters.FailIfDrops);
                Assert.False(true, "Should have thrown by now");
            }
            catch (InvalidOperationException)
            {

            }
        }
    }
}