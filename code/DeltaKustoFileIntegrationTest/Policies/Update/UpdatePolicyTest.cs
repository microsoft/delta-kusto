using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Policies.Update
{
    public class UpdatePolicyTest : IntegrationTestBase
    {
        [Fact]
        public async Task NoneToOne()
        {
            var paramPath = "Policies/Update/NoneToOne/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var createPolicyCommand = outputCommands
                .Where(c => c is AlterUpdatePolicyCommand)
                .Cast<AlterUpdatePolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(createPolicyCommand);
            Assert.Equal("my-table", createPolicyCommand!.TableName.Name);

            Assert.Single(createPolicyCommand!.UpdatePolicies);
            Assert.Equal("A", createPolicyCommand!.UpdatePolicies[0].Source);
        }
    }
}