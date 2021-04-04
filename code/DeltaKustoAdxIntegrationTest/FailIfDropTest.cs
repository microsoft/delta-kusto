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

namespace DeltaKustoAdxIntegrationTest.Functions
{
    public class FailIfDropTest : AdxIntegrationTestBase
    {
        [Fact]
        public async Task TestFailIfDropsNoDrop()
        {
            var toFile = "FailIfDrops/target.kql";
            var ct = CreateCancellationToken();

            await CleanDatabasesAsync(ct);
            await PrepareDbAsync(toFile, false, ct);

            await RunParametersAsync("FailIfDrops/no-fail.json", ct, TargetDbOverrides);

            //  We just test that this doesn't fail
        }

        [Fact]
        public async Task TestFailIfDrops()
        {
            var toFile = "FailIfDrops/target.kql";
            var ct = CreateCancellationToken();

            await CleanDatabasesAsync(ct);
            await PrepareDbAsync(toFile, false, ct);

            var overrides = TargetDbOverrides
                .Append(("failIfDrops", "true"));

            try
            {
                //  The "Main" will return non-zero which will throw an exception
                var parameters =
                    await RunParametersAsync("FailIfDrops/no-fail.json", ct, overrides);

                Assert.True(parameters.FailIfDrops);
                Assert.False(true, "Should have thrown by now");
            }
            catch (InvalidOperationException)
            {
            }
        }

        private CancellationToken CreateCancellationToken() =>
           new CancellationTokenSource(TimeSpan.FromSeconds(8)).Token;
    }
}