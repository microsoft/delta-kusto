using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class DeleteAutoDeletePolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestAutoDeletePolicy("A");
        }

        [Fact]
        public void FunkyTable()
        {
            TestAutoDeletePolicy("A- 1");
        }

        private void TestAutoDeletePolicy(string tableName)
        {
            var commandText = new DeleteAutoDeletePolicyCommand(new EntityName(tableName))
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<DeleteAutoDeletePolicyCommand>(command);
        }
    }
}