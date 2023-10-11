using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Policies.StreamingIngestion
{
    public class StreamingIngestionPolicyTableTest : IntegrationTestBase
    {
        #region Inner types
        private record StreamingIngestionPolicy
        {
            public bool IsEnabled { get; init; } = false;

            public double? HintAllocatedRate { get; init; } = null;
        }
        #endregion

        [Fact]
        public async Task NoneToOne()
        {
            var paramPath = "Policies/StreamingIngestion/Table/NoneToOne/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterStreamingIngestionPolicyCommand)
                .Cast<AlterStreamingIngestionPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Table, policyCommand!.EntityType);
            Assert.Equal("my-table", policyCommand!.EntityName.Name);
            Assert.True(
                policyCommand!.DeserializePolicy<StreamingIngestionPolicy>().IsEnabled);
            Assert.Equal(
                2.1,
                policyCommand!.DeserializePolicy<StreamingIngestionPolicy>().HintAllocatedRate);
        }

        [Fact]
        public async Task OneToNone()
        {
            var paramPath = "Policies/StreamingIngestion/Table/OneToNone/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is DeleteStreamingIngestionPolicyCommand)
                .Cast<DeleteStreamingIngestionPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Table, policyCommand!.EntityType);
            Assert.Equal("my-table", policyCommand!.EntityName.Name);
            Assert.False(
                policyCommand!.DeserializePolicy<StreamingIngestionPolicy>().IsEnabled);
            Assert.Null(
                policyCommand!.DeserializePolicy<StreamingIngestionPolicy>().HintAllocatedRate);
        }

        [Fact]
        public async Task OneToOne()
        {
            var paramPath = "Policies/StreamingIngestion/Table/OneToOneNoChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(outputCommands);
        }

        [Fact]
        public async Task OneToOneWithChange()
        {
            var paramPath = "Policies/StreamingIngestion/Table/OneToOneWithChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterStreamingIngestionPolicyCommand)
                .Cast<AlterStreamingIngestionPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Table, policyCommand!.EntityType);
            Assert.Equal("my-table", policyCommand!.EntityName.Name);
             Assert.False(
                policyCommand!.DeserializePolicy<StreamingIngestionPolicy>().IsEnabled);
            Assert.Null(
                policyCommand!.DeserializePolicy<StreamingIngestionPolicy>().HintAllocatedRate);
        }
    }
}