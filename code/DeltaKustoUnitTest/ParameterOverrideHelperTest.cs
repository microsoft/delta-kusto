using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest
{
    public class ParameterOverrideHelperTest : TestBase
    {
        [Fact]
        public void TestSingleProperty()
        {
            var main = new MainParameterization();
            var before = main.SendErrorOptIn;
            var value = (!before).ToString().ToLower();

            ParameterOverrideHelper.InplaceOverride(main, $"sendErrorOptIn={value}");

            Assert.Equal(!before, main.SendErrorOptIn);
        }

        [Fact]
        public void TestSinglePropertyWithEqualInValue()
        {
            var main = new MainParameterization();
            var value = "bla==this#=";

            ParameterOverrideHelper.InplaceOverride(main, $"tokenProvider.login.secret={value}");

            Assert.Equal(value, main.TokenProvider!.Login!.Secret);
        }

        #region Expected errors
        [Fact]
        public void TestSinglePropertyWrongType()
        {
            var main = new MainParameterization();

            try
            {
                ParameterOverrideHelper.InplaceOverride(main, "sendTelemetryOptIn=42");

                Assert.Fail("Shouldn't reach this point");
            }
            catch (DeltaException)
            {
            }
        }

        [Fact]
        public void TestSinglePropertyWrongName()
        {
            var main = new MainParameterization();

            try
            {
                ParameterOverrideHelper.InplaceOverride(main, "myPropertyOrTheHighWay=42");

                Assert.Fail("Shouldn't reach this point");
            }
            catch (DeltaException)
            {
            }
        }
        #endregion

        #region Existing properties
        [Fact]
        public void TestExistingPropertyPath()
        {
            var main = new MainParameterization
            {
                TokenProvider = new TokenProviderParameterization
                {
                    Login = new ServicePrincipalLoginParameterization
                    {
                        TenantId = "42",
                        ClientId = "15",
                        Secret = "My secret"
                    }
                }
            };
            var before = main.TokenProvider!.Login!.TenantId;
            var newTenantId = before + before;

            ParameterOverrideHelper.InplaceOverride(
                main,
                $"tokenProvider.login.tenantId={newTenantId}");

            Assert.Equal(newTenantId, main.TokenProvider!.Login!.TenantId);
        }

        [Fact]
        public void TestExistingDictionarySubItem()
        {
            var main = new MainParameterization
            {
                TokenProvider = new TokenProviderParameterization
                {
                    Tokens = new Dictionary<string, TokenParameterization>()
                    {
                        {
                            "mine",
                            new TokenParameterization
                            {
                                Token="abc"
                            }
                        }
                    }
                }
            };
            var before = main.TokenProvider!.Tokens["mine"].Token;
            var newToken = before + before;

            ParameterOverrideHelper.InplaceOverride(
                main,
                $"tokenProvider.tokens.mine.token={newToken}");

            Assert.Equal(newToken, main.TokenProvider!.Tokens["mine"].Token);
        }

        [Fact]
        public void TestExistingArraySubItem()
        {
            var main = new MainParameterization
            {
                Jobs = new Dictionary<string, JobParameterization>()
                {
                    {
                        "myJob",
                        new JobParameterization
                        {
                            Current = new SourceParameterization
                            {
                                Scripts = new []
                                {
                                    new SourceFileParametrization
                                    {
                                        FilePath="to-be-overriden"
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var before = main.Jobs["myJob"].Current!.Scripts![0].FilePath;
            var newFilePath = "hello.txt";

            ParameterOverrideHelper.InplaceOverride(
                main,
                $"Jobs.myJob.current.scripts[0].filePath={newFilePath}");

            Assert.Equal(newFilePath, main.Jobs["myJob"].Current!.Scripts![0].FilePath);
        }
        #endregion

        #region Non-existing properties
        [Fact]
        public void TestNonExistingSimplePropertyPath()
        {
            var main = new MainParameterization
            {
                TokenProvider = new TokenProviderParameterization
                {
                    Login = new ServicePrincipalLoginParameterization()
                }
            };
            var newTenantId = "hello-world";

            ParameterOverrideHelper.InplaceOverride(
                main,
                $"tokenProvider.login.tenantId={newTenantId}");

            Assert.Equal(newTenantId, main.TokenProvider!.Login!.TenantId);
        }

        [Fact]
        public void TestSimplePropertyInNonExistingNodePath()
        {
            var main = new MainParameterization
            {
                TokenProvider = new TokenProviderParameterization
                {
                }
            };
            var newTenantId = "hello-world";

            ParameterOverrideHelper.InplaceOverride(
                main,
                $"tokenProvider.login.tenantId={newTenantId}");

            Assert.Equal(newTenantId, main.TokenProvider!.Login!.TenantId);
        }

        [Fact]
        public void TestNonExistingDictionarySubItem()
        {
            var main = new MainParameterization
            {
                TokenProvider = new TokenProviderParameterization
                {
                    Tokens = new Dictionary<string, TokenParameterization>()
                }
            };
            var newToken = "Hello world";

            ParameterOverrideHelper.InplaceOverride(
                main,
                $"tokenProvider.tokens.mine.token={newToken}");

            Assert.Equal(newToken, main.TokenProvider!.Tokens["mine"].Token);
        }

        [Fact]
        public void TestNonExistingEntireDictionarySubItem()
        {
            var main = new MainParameterization
            {
                TokenProvider = new TokenProviderParameterization()
            };
            var newToken = "Hello world";

            ParameterOverrideHelper.InplaceOverride(
                main,
                $"tokenProvider.tokens.mine.token={newToken}");

            Assert.Equal(newToken, main.TokenProvider!.Tokens!["mine"].Token);
        }

        [Fact]
        public void TestComplexPropertyInNonExistingNodePath()
        {
            var main = new MainParameterization
            {
                TokenProvider = new TokenProviderParameterization
                {
                }
            };
            var tenantId = "hello-world";
            var clientId = "xyz_fasf";
            var secret = "fijfdsjfdsajipof20348234";

            ParameterOverrideHelper.InplaceOverride(
                main,
                $"tokenProvider.login={{\"tenantId\":\"{tenantId}\",\"clientId\":\"{clientId}\", \"secret\":\"{secret}\"}}");

            Assert.NotNull(main.TokenProvider!.Login);
            Assert.Equal(tenantId, main.TokenProvider!.Login!.TenantId);
            Assert.Equal(clientId, main.TokenProvider!.Login!.ClientId);
            Assert.Equal(secret, main.TokenProvider!.Login!.Secret);
        }

        [Fact]
        public void TestNonExistingDictionaryEntry()
        {
            var main = new MainParameterization();
            var jobName = "myJob";
            var value = 42;

            ParameterOverrideHelper.InplaceOverride(main, $"jobs.{jobName}.Priority={value}");

            Assert.Contains(jobName, main.Jobs);
            Assert.Equal(value, main.Jobs[jobName].Priority);
        }

        [Fact]
        public void TestNonExistingSubDictionaryEntry()
        {
            var main = new MainParameterization();
            var jobName = "myJob";
            var value = "my-cluster";

            ParameterOverrideHelper.InplaceOverride(
                main,
                $"jobs.{jobName}.current.adx.clusterUri={value}");

            Assert.Contains(jobName, main.Jobs);
            Assert.NotNull(main.Jobs[jobName].Current);
            Assert.NotNull(main.Jobs[jobName].Current!.Adx);
            Assert.NotNull(main.Jobs[jobName].Current!.Adx!.ClusterUri);
            Assert.Equal(value, main.Jobs[jobName].Current!.Adx!.ClusterUri);
        }

        [Fact]
        public void TestNonExistingArrayEntry()
        {
            var main = new MainParameterization();
            var jobName = "myJob";
            var value = "my-path";

            ParameterOverrideHelper.InplaceOverride(
                main,
                $"jobs.{jobName}.current.scripts[0].filePath={value}");

            Assert.Contains(jobName, main.Jobs);
            Assert.NotNull(main.Jobs[jobName].Current);
            Assert.NotNull(main.Jobs[jobName].Current!.Scripts);
            Assert.Single(main.Jobs[jobName].Current!.Scripts!);
            Assert.NotNull(main.Jobs[jobName].Current!.Scripts![0]);
            Assert.Equal(value, main.Jobs[jobName].Current!.Scripts![0].FilePath);
        }
        #endregion
    }
}