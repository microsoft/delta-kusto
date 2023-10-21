using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.Update;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.Update
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

        [Fact]
        public void PolicyWithFunkyTableName()
        {
            TestUpdatePolicy(
                "B 1",
                new UpdatePolicy
                {
                    IsEnabled = true,
                    IsTransactional = true,
                    Source = "A-1",
                    Query = "A-1"
                });
        }

        [Fact]
        public void PolicyWithFunkyQuery()
        {
            TestUpdatePolicy(
                "B 1",
                new UpdatePolicy
                {
                    IsEnabled = true,
                    IsTransactional = true,
                    Source = "A-1",
                    Query = "['A-1'] | where ['c.42'] == \\\"ABC\\\""
                });
        }

        [Fact]
        public void DbComposedTableName()
        {
            var command = ParseOneCommand(".alter table mydb.mytable policy update '[]'");

            Assert.IsType<AlterUpdatePolicyCommand>(command);

            var realCommand = (AlterUpdatePolicyCommand)command;

            Assert.Equal("mytable", realCommand.TableName.Name);
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table ['my cluster'].['my db'].['my table'] policy update '[]'");

            Assert.IsType<AlterUpdatePolicyCommand>(command);

            var realCommand = (AlterUpdatePolicyCommand)command;

            Assert.Equal("my table", realCommand.TableName.Name);
        }

        private void TestUpdatePolicy(string tableName, params UpdatePolicy[] policies)
        {
            var table = new EntityName(tableName);
            var policiesText = JsonSerializer.Serialize(policies);
            var commandText = $".alter table {table.ToScript()} policy update @'{policiesText}'";
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterUpdatePolicyCommand>(command);
        }
    }
}