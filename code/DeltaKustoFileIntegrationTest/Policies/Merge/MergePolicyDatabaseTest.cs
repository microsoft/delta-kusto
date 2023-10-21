using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.Merge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Policies.Merge
{
    public class MergePolicyDatabaseTest : IntegrationTestBase
    {
        #region Inner types
        private record MergePolicy
        {
            public int MaxRangeInHours { get; init; }
        }
        #endregion

        [Fact]
        public async Task NoneToOne()
        {
            var paramPath = "Policies/Merge/Database/NoneToOne/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterMergePolicyCommand)
                .Cast<AlterMergePolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Database, policyCommand!.EntityType);
            Assert.Equal("mydb", policyCommand!.EntityName.Name);
            Assert.Equal(
                24,
                policyCommand!.DeserializePolicy<MergePolicy>().MaxRangeInHours);
        }

        [Fact]
        public async Task OneToNone()
        {
            var paramPath = "Policies/Merge/Database/OneToNone/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is DeleteMergePolicyCommand)
                .Cast<DeleteMergePolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Database, policyCommand!.EntityType);
            Assert.Equal("mydb", policyCommand!.EntityName.Name);
        }

        [Fact]
        public async Task OneToOne()
        {
            var paramPath = "Policies/Merge/Database/OneToOneNoChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(outputCommands);
        }

        [Fact]
        public async Task OneToOneWithChange()
        {
            var paramPath = "Policies/Merge/Database/OneToOneWithChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterMergePolicyCommand)
                .Cast<AlterMergePolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal(EntityType.Database, policyCommand!.EntityType);
            Assert.Equal("mydb", policyCommand!.EntityName.Name);
            Assert.Equal(
               25,
               policyCommand!.DeserializePolicy<MergePolicy>().MaxRangeInHours);
        }
    }
}