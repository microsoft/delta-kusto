using DeltaKustoIntegration.Parameterization;
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
            var before = main.SendTelemetryOptIn;
            var value = (!before).ToString().ToLower();

            ParameterOverrideHelper.InplaceOverride(
                main,
                $"[{{\"path\" : \"sendTelemetryOptIn\", \"value\":{value} }}]");

            Assert.Equal(!before, main.SendTelemetryOptIn);
        }
    }
}