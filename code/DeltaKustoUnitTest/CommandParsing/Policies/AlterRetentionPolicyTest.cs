using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterRetentionPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestRetentionPolicy(EntityType.Table, "A", TimeSpan.FromDays(3), true);
        }

        [Fact]
        public void FunkyTable()
        {
            TestRetentionPolicy(EntityType.Table, "A- 1", TimeSpan.FromMinutes(90), false);
        }

        [Fact]
        public void DbComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mydb.mytable policy retention "
                + "@'{\"SoftDeletePeriod\":\"90.00:00:00\"}'");

            Assert.IsType<AlterRetentionPolicyCommand>(command);

            var realCommand = (AlterRetentionPolicyCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mycluster.['my db'].mytable policy retention "
                + "@'{\"SoftDeletePeriod\":\"90.00:00:00\"}'");

            Assert.IsType<AlterRetentionPolicyCommand>(command);

            var realCommand = (AlterRetentionPolicyCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestRetentionPolicy(EntityType.Database, "Db", TimeSpan.FromSeconds(40), true);
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestRetentionPolicy(EntityType.Database, "db.mine", TimeSpan.FromHours(90), false);
        }

        private void TestRetentionPolicy(
            EntityType type,
            string name,
            TimeSpan softDelete,
            bool recoverability)
        {
            var commandText = new AlterRetentionPolicyCommand(
                type,
                new EntityName(name),
                softDelete,
                recoverability)
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterRetentionPolicyCommand>(command);

            var realCommand = (AlterRetentionPolicyCommand)command;

            Assert.Equal(type, realCommand.EntityType);
            Assert.Equal(name, realCommand.EntityName.Name);
            Assert.Equal(softDelete, realCommand.SoftDeletePeriod);
            Assert.Equal(recoverability, realCommand.Recoverability);
        }
    }
}