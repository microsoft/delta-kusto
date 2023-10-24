using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.IngestionBatching;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.IngestionBatching
{
    public class DeleteIngestionBatchPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestIngestionBatchingPolicy(EntityType.Table, "A");
        }

        [Fact]
        public void FunkyTable()
        {
            TestIngestionBatchingPolicy(EntityType.Table, "A- 1");
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestIngestionBatchingPolicy(EntityType.Database, "Db");
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestIngestionBatchingPolicy(EntityType.Database, "db.mine");
        }

        private void TestIngestionBatchingPolicy(EntityType type, string name)
        {
            var commandText = new DeleteIngestionBatchingPolicyCommand(type, new EntityName(name))
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<DeleteIngestionBatchingPolicyCommand>(command);
        }
    }
}