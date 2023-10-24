using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.Merge;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.Merge
{
    public class AlterMergePolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestMergePolicy(EntityType.Table, "A", 1000000, 10, TimeSpan.FromDays(1));
        }

        [Fact]
        public void FunkyTable()
        {
            TestMergePolicy(EntityType.Table, "A- 1", 2000000, 20, TimeSpan.FromDays(2));
        }

        [Fact]
        public void DbComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mydb.mytable policy merge "
                + "@'{\"RowCountUpperBoundForMerge\":\"200000\"}'");

            Assert.IsType<AlterMergePolicyCommand>(command);

            var realCommand = (AlterMergePolicyCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mycluster.['my db'].mytable policy merge "
                + "@'{\"RowCountUpperBoundForMerge\":\"300000\"}'");

            Assert.IsType<AlterMergePolicyCommand>(command);

            var realCommand = (AlterMergePolicyCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestMergePolicy(EntityType.Database, "Db", 3000000, 30, TimeSpan.FromDays(3));
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestMergePolicy(EntityType.Database, "db.mine", 4000000, 40, TimeSpan.FromDays(4));
        }

        private void TestMergePolicy(
            EntityType type,
            string name,
            int rowCountUpperBoundForMerge,
            int maxExtentsToMerge,
            TimeSpan loopPeriod)
        {
            var command = new AlterMergePolicyCommand(
                type,
                new EntityName(name),
                rowCountUpperBoundForMerge,
                maxExtentsToMerge,
                loopPeriod);
            var commandText = command.ToScript(null);
            var parsedCommand = ParseOneCommand(commandText);

            Assert.IsType<AlterMergePolicyCommand>(parsedCommand);
        }
    }
}