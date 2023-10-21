using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.RestrictedView;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.RestrictedView
{
    public class DeleteRestrictedViewPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestRestrictedViewPolicy("A");
        }

        [Fact]
        public void FunkyTable()
        {
            TestRestrictedViewPolicy("A- 1");
        }

        private void TestRestrictedViewPolicy(string tableName)
        {
            var commandText = new DeleteRestrictedViewPolicyCommand(new EntityName(tableName))
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<DeleteRestrictedViewPolicyCommand>(command);

            var realCommand = (DeleteRestrictedViewPolicyCommand)command;

            Assert.Equal(tableName, realCommand.TableName.Name);
        }
    }
}