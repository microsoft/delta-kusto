using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class AlterUpdatePolicyTest : ParsingTestBase
    {
        [Fact]
        public void NoObjects()
        {
            TestUpdatePolicy("B");
        }

        [Fact]
        public void NoObjectsFunkyTableName()
        {
            TestUpdatePolicy("B 1");
        }

        [Fact]
        public void SimplePolicy()
        {
            TestUpdatePolicy("B 1", new UpdatePolicy { Source = "A", Query = "A" });
        }

        [Fact]
        public void TwoSimplePolicies()
        {
            TestUpdatePolicy(
                "B",
                new UpdatePolicy { Source = "A", Query = "A" },
                new UpdatePolicy { Source = "C", Query = "C" });
        }

        private void TestUpdatePolicy(string tableName, params UpdatePolicy[] policies)
        {
            var table = new EntityName(tableName);
            var policiesText = JsonSerializer.Serialize(policies);
            var commandText = $".alter table {table.ToScript()} policy update @'{policiesText}'";
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterUpdatePolicyCommand>(command);

            var alterUpdatePolicyCommand = (AlterUpdatePolicyCommand)command;

            Assert.Equal(tableName, alterUpdatePolicyCommand.TableName.Name);
            Assert.Equal(policies, alterUpdatePolicyCommand.UpdatePolicies);
        }
    }
}