using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
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
            var commandText = @"
.alter table mydb.mytable policy partitioning ```
{
  'PartitionKeys': [
    {
                'ColumnName': 'my_string_column',
      'Kind': 'Hash',
      'Properties': {
                    'Function': 'XxHash64',
        'MaxPartitionCount': 128,
        'PartitionAssignmentMode': 'Uniform'
      }
            }
  ]
}```
";
            var commands = CommandBase.FromScript(commandText, true);

            Assert.Empty(commands);
        }

        [Fact]
        public void DbComposedTableName()
        {
            var commandText = @"
.alter table mydb.mytable policy partitioning ```
{
  'PartitionKeys': [
    {
                'ColumnName': 'my_string_column',
      'Kind': 'Hash',
      'Properties': {
                    'Function': 'XxHash64',
        'MaxPartitionCount': 128,
        'PartitionAssignmentMode': 'Uniform'
      }
            }
  ]
}```
";
            var commands = CommandBase.FromScript(commandText, true);

            Assert.Empty(commands);
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            var commandText = @"
.alter table mycluster.mydb.mytable policy partitioning ```
{
  'PartitionKeys': [
    {
                'ColumnName': 'my_string_column',
      'Kind': 'Hash',
      'Properties': {
                    'Function': 'XxHash64',
        'MaxPartitionCount': 128,
        'PartitionAssignmentMode': 'Uniform'
      }
            }
  ]
}```
";
            var commands = CommandBase.FromScript(commandText, true);

            Assert.Empty(commands);
        }
    }
}