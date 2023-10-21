using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.Retention;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Policies.Retention
{
    public class RetentionPolicyDatabaseTest : IntegrationTestBase
    {
        #region Inner types
        private record RetentionPolicy
        {
            public string SoftDeletePeriod { get; init; } = string.Empty;

            public TimeSpan GetSoftDeletePeriod() => TimeSpan.Parse(SoftDeletePeriod);
        }
        #endregion

        [Fact]
        public async Task NoneToOne()
        {
            var paramPath = "Policies/Retention/Database/NoneToOne/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterRetentionPolicyCommand)
                .Cast<AlterRetentionPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Database, policyCommand!.EntityType);
            Assert.Equal("mydb", policyCommand!.EntityName.Name);
            Assert.Equal(
                TimeSpan.FromHours(12),
                policyCommand!.DeserializePolicy<RetentionPolicy>().GetSoftDeletePeriod());
        }

        [Fact]
        public async Task OneToNone()
        {
            var paramPath = "Policies/Retention/Database/OneToNone/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is DeleteRetentionPolicyCommand)
                .Cast<DeleteRetentionPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Database, policyCommand!.EntityType);
            Assert.Equal("mydb", policyCommand!.EntityName.Name);
        }

        [Fact]
        public async Task OneToOne()
        {
            var paramPath = "Policies/Retention/Database/OneToOneNoChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(outputCommands);
        }

        [Fact]
        public async Task OneToOneWithChange()
        {
            var paramPath = "Policies/Retention/Database/OneToOneWithChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterRetentionPolicyCommand)
                .Cast<AlterRetentionPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Database, policyCommand!.EntityType);
            Assert.Equal("mydb", policyCommand!.EntityName.Name);
            Assert.Equal(
                TimeSpan.FromDays(10),
                policyCommand!.DeserializePolicy<RetentionPolicy>().GetSoftDeletePeriod());
        }
    }
}