using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Policies.IngestionBatching
{
    public class IngestionBatchingPolicyTableTest : IntegrationTestBase
    {
        #region Inner types
        private record IngestionBatchingPolicy
        {
            public string MaximumBatchingTimeSpan { get; init; } = string.Empty;

            public TimeSpan GetMaximumBatchingTimeSpan() =>
                TimeSpan.Parse(MaximumBatchingTimeSpan);
        }
        #endregion

        [Fact]
        public async Task NoneToOne()
        {
            var paramPath = "Policies/IngestionBatching/Table/NoneToOne/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterIngestionBatchingPolicyCommand)
                .Cast<AlterIngestionBatchingPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Table, policyCommand!.EntityType);
            Assert.Equal("my-table", policyCommand!.EntityName.Name);
            Assert.Equal(
                new TimeSpan(0, 7, 17),
                policyCommand!.DeserializePolicy<IngestionBatchingPolicy>().GetMaximumBatchingTimeSpan());
        }

        [Fact]
        public async Task OneToNone()
        {
            var paramPath = "Policies/IngestionBatching/Table/OneToNone/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is DeleteIngestionBatchingPolicyCommand)
                .Cast<DeleteIngestionBatchingPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Table, policyCommand!.EntityType);
            Assert.Equal("my-table", policyCommand!.EntityName.Name);
        }

        [Fact]
        public async Task OneToOne()
        {
            var paramPath = "Policies/IngestionBatching/Table/OneToOneNoChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(outputCommands);
        }

        [Fact]
        public async Task OneToOneWithChange()
        {
            var paramPath = "Policies/IngestionBatching/Table/OneToOneWithChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterIngestionBatchingPolicyCommand)
                .Cast<AlterIngestionBatchingPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Table, policyCommand!.EntityType);
            Assert.Equal("my-table", policyCommand!.EntityName.Name);
            Assert.Equal(
               new TimeSpan(0, 7, 18),
               policyCommand!.DeserializePolicy<IngestionBatchingPolicy>().GetMaximumBatchingTimeSpan());
        }
    }
}