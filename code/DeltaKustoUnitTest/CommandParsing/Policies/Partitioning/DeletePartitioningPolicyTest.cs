using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.Partitioning;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.Partitioning
{
    public class DeletePartitioningPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestDeletePartitioning("A");
        }

        [Fact]
        public void FunkyTable()
        {
            TestDeletePartitioning("A- 1");
        }

        private void TestDeletePartitioning(string name)
        {
            var commandText = new DeletePartitioningPolicyCommand(
                new EntityName(name))
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<DeletePartitioningPolicyCommand>(command);
        }
    }
}