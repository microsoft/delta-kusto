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
            var targetDbName = await AdxDbFixture.InitializeDbAsync();
            var loginTokenProvider = new LoginTokenProvider(
                new ConsoleTracer(false),
                HttpClientFactory,
                TenantId,
                ServicePrincipalId,
                ServicePrincipalSecret) as ITokenProvider;
            var token = await loginTokenProvider.GetTokenAsync(ClusterUri.ToString());
            var overrides = ImmutableArray<(string path, string value)>
                .Empty
                .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                .Add(("jobs.main.target.adx.database", targetDbName))
                .Add(("tokenProvider.tokens.myToken.clusterUri", ClusterUri.ToString()))
                .Add(("tokenProvider.tokens.myToken.token", token));

            await PrepareDbAsync("GivenTokenProvider/target.kql", targetDbName);
            await RunParametersAsync("GivenTokenProvider/given-token.yaml", overrides);

            //  We just test that this doesn't fail
        }
    }
}