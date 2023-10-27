using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.Update;
using Kusto.Language.Syntax;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.Update
{
    public class DeleteUpdatePolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestUpdatePolicy("B");
        }

        [Fact]
        public void FunkyTable()
        {
            TestUpdatePolicy("B 1");
        }

        private void TestUpdatePolicy(string tableName)
        {
            var table = new EntityName(tableName);
            var commandText = $".delete table {table.ToScript()} policy update";
            var command = ParseOneCommand(commandText);

            Assert.IsType<DeleteUpdatePolicyCommand>(command);

            var realCommand = (DeleteUpdatePolicyCommand)command;

            Assert.Equal(tableName, realCommand.EntityName.Name);
        }
    }
}