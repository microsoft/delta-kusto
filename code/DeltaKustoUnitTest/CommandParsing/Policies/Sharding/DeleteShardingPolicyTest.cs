using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.Sharding;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.Sharding
{
    public class DeleteShardingPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestShardingPolicy(EntityType.Table, "A");
        }

        [Fact]
        public void FunkyTable()
        {
            TestShardingPolicy(EntityType.Table, "A- 1");
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestShardingPolicy(EntityType.Database, "Db");
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestShardingPolicy(EntityType.Database, "db.mine");
        }

        private void TestShardingPolicy(EntityType type, string name)
        {
            var commandText = new DeleteShardingPolicyCommand(type, new EntityName(name))
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<DeleteShardingPolicyCommand>(command);
        }
    }
}