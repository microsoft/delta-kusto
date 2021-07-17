using DeltaKustoIntegration.Database;
using DeltaKustoIntegration.TokenProvider;
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
    public class GivenTokenProviderTest : AdxIntegrationTestBase
    {
        public GivenTokenProviderTest(AdxDbFixture adxDbFixture)
            //  We do not want the login provider as we are testing the given token provider
            : base(adxDbFixture, false)
        {
        }

        [Fact]
        public async Task TestGivenToken()
        {
            var clusterUri = TargetDbOverrides
                .Where(p => p.path == "jobs.main.target.adx.clusterUri")
                .Select(p => p.value)
                .First();
            var loginTokenProvider = new LoginTokenProvider(
                new ConsoleTracer(false),
                HttpClientFactory,
                TenantId,
                ServicePrincipalId,
                ServicePrincipalSecret) as ITokenProvider;
            var token = await loginTokenProvider.GetTokenAsync(clusterUri);
            var overrides = TargetDbOverrides
                .Append(("tokenProvider.tokens.myToken.clusterUri", clusterUri))
                .Append(("tokenProvider.tokens.myToken.token", token));

            await CleanDatabasesAsync();
            await PrepareDbAsync("GivenTokenProvider/target.kql", false);

            await RunParametersAsync("GivenTokenProvider/given-token.yaml", overrides);

            //  We just test that this doesn't fail
        }
    }
}