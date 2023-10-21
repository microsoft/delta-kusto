using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.Partitioning;
using Kusto.Language.Syntax;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.Partitioning
{
    public class AlterPartitioningPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestAlterPartitioning("mytable");
        }

        [Fact]
        public void DbComposedTableName()
        {
            TestAlterPartitioning("mydb.mytable");
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            TestAlterPartitioning("mycluster.mydb.mytable");
        }

        private void TestAlterPartitioning(string tableExpression)
        {
            var commandText = $@"
.alter table {tableExpression} policy partitioning ```
{{
  ""PartitionKeys"": [
    {{
      ""ColumnName"": ""my_string_column"",
      ""Kind"": ""Hash"",
      ""Properties"": {{
        ""Function"": ""XxHash64"",
        ""MaxPartitionCount"": 128,
        ""PartitionAssignmentMode"": ""Uniform""
      }}
    }}
  ]
}}
";
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterPartitioningPolicyCommand>(command);

            var realCommand = (AlterPartitioningPolicyCommand)command;

            Assert.Equal("mytable", realCommand.TableName.Name);
        }
    }
}