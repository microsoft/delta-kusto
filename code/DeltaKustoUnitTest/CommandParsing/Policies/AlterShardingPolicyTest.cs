using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterShardingPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestShardingPolicy(EntityType.Table, "A", 1000000, 1000, 100);
        }

        [Fact]
        public void FunkyTable()
        {
            TestShardingPolicy(EntityType.Table, "A- 1", 2000000, 2000, 150);
        }

        [Fact]
        public void DbComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mydb.mytable policy sharding "
                + "@'{\"MaxRowCount\":200000}'");

            Assert.IsType<AlterShardingPolicyCommand>(command);

            var realCommand = (AlterShardingPolicyCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mycluster.['my db'].mytable policy sharding "
                + "@'{\"MaxRowCount\":300000}'");

            Assert.IsType<AlterShardingPolicyCommand>(command);

            var realCommand = (AlterShardingPolicyCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestShardingPolicy(EntityType.Database, "Db", 1500000, 2000, 150);
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestShardingPolicy(EntityType.Database, "db.mine", 2000000, 3500, 240);
        }

        private void TestShardingPolicy(
            EntityType type,
            string name,
            int maxRowCount,
            int maxExtentSizeInMb,
            int maxOriginalSizeInMb)
        {
            var command = new AlterShardingPolicyCommand(
                type,
                new EntityName(name),
                maxRowCount,
                maxExtentSizeInMb,
                maxOriginalSizeInMb);
            var commandText = command.ToScript(null);
            var parsedCommand = ParseOneCommand(commandText);

            Assert.IsType<AlterShardingPolicyCommand>(parsedCommand);
        }
    }
}