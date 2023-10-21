using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.RowLevelSecurity;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.RowLevelSecurity
{
    public class DeleteRowLevelSecurityPolicyTest : ParsingTestBase
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
            var commandText = new DeleteRowLevelSecurityPolicyCommand(new EntityName(tableName))
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<DeleteRowLevelSecurityPolicyCommand>(command);
        }
    }
}