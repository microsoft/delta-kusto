using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.IngestionTime;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.IngestionTime
{
    public class DeleteIngestionTimePolicyTest : ParsingTestBase
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

        private void TestIngestionTimePolicy(string tableName)
        {
            var commandText = $@"
.delete table {tableName} policy ingestiontime";
            var command = ParseOneCommand(commandText);

            Assert.IsType<DeleteIngestionTimePolicyCommand>(command);

            var realCommand = (DeleteIngestionTimePolicyCommand)command;

            Assert.Equal(GetActualTableName(tableName), realCommand.TableName.Name);
        }
    }
}