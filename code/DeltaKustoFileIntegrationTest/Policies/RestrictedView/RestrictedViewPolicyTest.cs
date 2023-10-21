using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies.RestrictedView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Policies.RestrictedView
{
    public class RestrictedViewPolicyTest : IntegrationTestBase
    {
        [Fact]
        public async Task NoneToOne()
        {
            var paramPath = "Policies/RestrictedView/NoneToOne/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterRestrictedViewPolicyCommand)
                .Cast<AlterRestrictedViewPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal("my-table", policyCommand.TableName.Name);
            Assert.True(policyCommand.IsEnabled);
        }

        [Fact]
        public async Task OneToNone()
        {
            var paramPath = "Policies/RestrictedView/OneToNone/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is DeleteRestrictedViewPolicyCommand)
                .Cast<DeleteRestrictedViewPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal("my-table", policyCommand.TableName.Name);
        }

        [Fact]
        public async Task OneToOne()
        {
            var paramPath = "Policies/RestrictedView/OneToOneNoChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(outputCommands);
        }

        [Fact]
        public async Task OneToOneWithChange()
        {
            var paramPath = "Policies/RestrictedView/OneToOneWithChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterRestrictedViewPolicyCommand)
                .Cast<AlterRestrictedViewPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal("my-table", policyCommand.TableName.Name);
            Assert.False(policyCommand.IsEnabled);
        }
    }
}