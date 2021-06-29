using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Policies.Retention
{
    public class RetentionPolicyTableTest : IntegrationTestBase
    {
        [Fact]
        public async Task NoneToOne()
        {
            var paramPath = "Policies/Retention/Table/NoneToOne/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterRetentionPolicyCommand)
                .Cast<AlterRetentionPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Table, policyCommand!.EntityType);
            Assert.Equal("my-table", policyCommand!.EntityName.Name);
            Assert.Equal(TimeSpan.FromHours(12), policyCommand!.SoftDelete);
        }

        [Fact]
        public async Task OneToNone()
        {
            var paramPath = "Policies/Retention/Table/OneToNone/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is DeleteRetentionPolicyCommand)
                .Cast<DeleteRetentionPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Table, policyCommand!.EntityType);
            Assert.Equal("my-table", policyCommand!.EntityName.Name);
        }

        [Fact]
        public async Task OneToOne()
        {
            var paramPath = "Policies/Retention/Table/OneToOneNoChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(outputCommands);
        }

        [Fact]
        public async Task OneToOneWithChange()
        {
            var paramPath = "Policies/Retention/Table/OneToOneWithChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterRetentionPolicyCommand)
                .Cast<AlterRetentionPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Table, policyCommand!.EntityType);
            Assert.Equal("my-table", policyCommand!.EntityName.Name);
            Assert.Equal(TimeSpan.FromDays(10), policyCommand!.SoftDelete);
        }
    }
}