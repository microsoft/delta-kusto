using DeltaKustoIntegration.Database;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest.NoAuth
{
    public class NoAuthTest : AdxIntegrationTestBase
    {
        public NoAuthTest()
            //  We do not want the login provider as we are testing the given token provider
            : base(false)
        {
        }

        [Fact]
        public async Task NoAuthExpectedEx()
        {
            try
            {
                using (var targetDb = await InitializeDbAsync())
                {
                    var overrides = ImmutableArray<(string path, string value)>
                        .Empty
                        .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                        .Add(("jobs.main.target.adx.database", targetDb.Name));

                    await PrepareDbAsync("NoAuth/target.kql", targetDb.Name);
                    await RunParametersAsync("NoAuth/no-auth.yaml", overrides);

                    //  We just test that this doesn't fail
                }
                Assert.Fail("This test is failing");
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}