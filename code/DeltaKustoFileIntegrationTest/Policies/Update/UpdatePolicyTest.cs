﻿using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.Update;
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
            Assert.Equal("my-table", createPolicyCommand!.EntityName.Name);

            Assert.Single(createPolicyCommand!.UpdatePolicies);
            Assert.Equal("A", createPolicyCommand!.UpdatePolicies[0].Source);
        }

        [Fact]
        public async Task OneToNone()
        {
            var paramPath = "Policies/Update/OneToNone/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var deletePolicyCommand = outputCommands
                .Where(c => c is DeleteUpdatePolicyCommand)
                .Cast<DeleteUpdatePolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(deletePolicyCommand);
            Assert.Equal("my-table", deletePolicyCommand!.EntityName.Name);
        }

        [Fact]
        public async Task OneToOne()
        {
            var paramPath = "Policies/Update/OneToOneNoChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(outputCommands);
        }

        [Fact]
        public async Task OneToOneWithChange()
        {
            var paramPath = "Policies/Update/OneToOneWithChange/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);

            var alterPolicyCommand = outputCommands
                .Where(c => c is AlterUpdatePolicyCommand)
                .Cast<AlterUpdatePolicyCommand>()
                .FirstOrDefault();

            Assert.NotNull(alterPolicyCommand);
            Assert.Equal("my-table", alterPolicyCommand!.EntityName.Name);

            Assert.Single(alterPolicyCommand!.UpdatePolicies);
            Assert.Equal("A", alterPolicyCommand!.UpdatePolicies[0].Source);
            Assert.EndsWith("t", alterPolicyCommand!.UpdatePolicies[0].Query!);
        }
    }
}