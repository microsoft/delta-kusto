using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.AutoDelete;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.AutoDelete
{
    public class AlterAutoDeletePolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestAutoDeletePolicy("A", new DateTime(2030, 1, 1), true);
        }

        [Fact]
        public void FunkyTable()
        {
            TestAutoDeletePolicy("A- 1", new DateTime(2030, 1, 1), false);
        }

        [Fact]
        public void DbComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mydb.mytable policy auto_delete"
                + "@'{\"ExpiryDate\":\"2030-02-01\"}'");

            Assert.IsType<AlterAutoDeletePolicyCommand>(command);

            var realCommand = (AlterAutoDeletePolicyCommand)command;

            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mycluster.['my db'].mytable policy auto_delete "
                + "@'{\"ExpiryDate\":\"2031-02-01\"}'");

            Assert.IsType<AlterAutoDeletePolicyCommand>(command);

            var realCommand = (AlterAutoDeletePolicyCommand)command;

            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        private void TestAutoDeletePolicy(
            string tableName,
            DateTime expiryDate,
            bool deleteIfNotEmpty)
        {
            var commandText = new AlterAutoDeletePolicyCommand(
                new EntityName(tableName),
                expiryDate,
                deleteIfNotEmpty)
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterAutoDeletePolicyCommand>(command);
        }
    }
}