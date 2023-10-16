using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using Kusto.Language.Syntax;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterPartitioningPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestAlter("mytable");
        }

        [Fact]
        public void DbComposedTableName()
        {
            TestAlter("mydb.mytable");
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            TestAlter("mycluster.mydb.mytable");
        }

        private static void TestAlter(string tableExpression)
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
            var commands = CommandBase.FromScript(commandText, true);

            Assert.Single(commands);
            Assert.IsType<AlterPartitioningPolicyCommand>(commands.First());

            var realCommand = (AlterPartitioningPolicyCommand)commands.First();

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }
    }
}