using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Policies.AutoDelete
{
    public class AutoDeletePolicyTest : IntegrationTestBase
    {
        #region Inner types
        private record AutoDeletePolicy
        {
            public string ExpiryDate { get; init; } = string.Empty;

            public DateTime GetExpiryDate() => DateTime.Parse(ExpiryDate);
        }
        #endregion

        [Fact]
        public async Task NoneToOne()
        {
            var paramPath = "Policies/AutoDelete/NoneToOne/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterAutoDeletePolicyCommand)
                .Cast<AlterAutoDeletePolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal("my-table", policyCommand!.TableName.Name);
            Assert.Equal(
                new DateTime(2030, 1, 1),
                policyCommand!.DeserializePolicy<AutoDeletePolicy>().GetExpiryDate());
        }

        [Fact]
        public async Task OneToNone()
        {
            var paramPath = "Policies/AutoDelete/OneToNone/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is DeleteAutoDeletePolicyCommand)
                .Cast<DeleteAutoDeletePolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal("my-table", policyCommand!.TableName.Name);
        }

        [Fact]
        public async Task OneToOne()
        {
            var paramPath = "Policies/AutoDelete/OneToOneNoChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(outputCommands);
        }

        [Fact]
        public async Task OneToOneWithChange()
        {
            var paramPath = "Policies/AutoDelete/OneToOneWithChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterAutoDeletePolicyCommand)
                .Cast<AlterAutoDeletePolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal("my-table", policyCommand!.TableName.Name);
            Assert.Equal(
               new DateTime(2030, 1, 1),
               policyCommand!.DeserializePolicy<AutoDeletePolicy>().GetExpiryDate());
        }
    }
}