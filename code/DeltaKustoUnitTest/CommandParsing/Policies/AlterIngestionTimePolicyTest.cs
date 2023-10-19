using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterIngestionTimePolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestIngestionTimePolicy("A");
        }

        [Fact]
        public void FunkyTable()
        {
            TestIngestionTimePolicy("['A- 1']");
        }

        [Fact]
        public void DbComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mydb.mytable policy auto_delete"
                + "@'{\"ExpiryDate\":\"2030-02-01\"}'");

            Assert.IsType<AlterAutoDeletePolicyCommand>(command);

            var realCommand = (AlterAutoDeletePolicyCommand)command;

            Assert.Equal("mytable", realCommand.TableName.Name);
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table mycluster.['my db'].mytable policy auto_delete "
                + "@'{\"ExpiryDate\":\"2031-02-01\"}'");

            Assert.IsType<AlterAutoDeletePolicyCommand>(command);

            var realCommand = (AlterAutoDeletePolicyCommand)command;

            Assert.Equal("mytable", realCommand.TableName.Name);
        }

        private void TestIngestionTimePolicy(string tableName)
        {
            TestIngestionTimePolicy(tableName, true);
            TestIngestionTimePolicy(tableName, false);
        }

        private void TestIngestionTimePolicy(string tableName, bool isEnabled)
        {
            var commandText = $@"
.alter table {tableName} policy ingestiontime {isEnabled.ToString().ToLower()}";
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterIngestionTimePolicyCommand>(command);

            var realCommand = (AlterIngestionTimePolicyCommand)command;

            Assert.Equal(tableName, realCommand.TableName.Name);
            Assert.Equal(isEnabled, realCommand.IsEnabled);
        }
    }
}