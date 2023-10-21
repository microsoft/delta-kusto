using DeltaKustoLib.CommandModel.Policies.RowLevelSecurity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Policies.RowLevelSecurity
{
    public class RowLevelSecurityPolicyTest : IntegrationTestBase
    {
        [Fact]
        public async Task NoneToOne()
        {
            var paramPath = "Policies/RowLevelSecurity/NoneToOne/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterRowLevelSecurityPolicyCommand)
                .Cast<AlterRowLevelSecurityPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal("my-table", policyCommand.TableName.Name);
            Assert.True(policyCommand.IsEnabled);
            Assert.Equal("['my-table']", policyCommand.Query.Text);
        }

        [Fact]
        public async Task OneToNone()
        {
            var paramPath = "Policies/RowLevelSecurity/OneToNone/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is DeleteRowLevelSecurityPolicyCommand)
                .Cast<DeleteRowLevelSecurityPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal("my-table", policyCommand.TableName.Name);
        }

        [Fact]
        public async Task OneToOne()
        {
            var paramPath = "Policies/RowLevelSecurity/OneToOneNoChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(outputCommands);
        }

        [Fact]
        public async Task OneToOneWithChange()
        {
            var paramPath = "Policies/RowLevelSecurity/OneToOneWithChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var policyCommand = outputCommands
                .Where(c => c is AlterRowLevelSecurityPolicyCommand)
                .Cast<AlterRowLevelSecurityPolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(policyCommand);
            Assert.Equal("my-table", policyCommand.TableName.Name);
            Assert.False(policyCommand.IsEnabled);
            Assert.Equal("['my-table']", policyCommand.Query.Text);
        }
    }
}