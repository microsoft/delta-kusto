using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest
{
    public class ParameterOverrideHelperTest
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

        #region Expected errors
        [Fact]
        public void TestSinglePropertyWrongType()
        {
            var main = new MainParameterization();

            try
            {
                ParameterOverrideHelper.InplaceOverride(main, "sendTelemetryOptIn=42");

                Assert.True(false, "Shouldn't reach this point");
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

                Assert.True(false, "Shouldn't reach this point");
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
        #endregion
    }
}