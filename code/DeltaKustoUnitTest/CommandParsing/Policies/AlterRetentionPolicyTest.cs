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
                .ToScript();
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterRetentionPolicyCommand>(command);

            var realCommand = (AlterRetentionPolicyCommand)command;

            Assert.Equal(type, realCommand.EntityType);
            Assert.Equal(name, realCommand.EntityName.Name);
            Assert.Equal(softDelete, realCommand.SoftDelete);
            Assert.Equal(recoverability, realCommand.Recoverability);
        }
    }
}