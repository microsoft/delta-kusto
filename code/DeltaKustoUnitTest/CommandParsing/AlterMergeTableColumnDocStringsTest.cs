using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class AlterMergeTableColumnDocStringsTest : ParsingTestBase
    {
        [Fact]
        public void OneColumn()
        {
            var tableName = "t1";
            var columns = new[]
            {
                (name: "Timestamp", docString: "Time of \\nday")
            };
            var command = ParseOneCommand(
                $".alter-merge table {tableName} column-docstrings "
                + $"({string.Join(", ", columns.Select(c => $"{c.name}:\"{c.docString}\""))})");

            ValidateColumnCommand(command, tableName, columns);
        }

        [Fact]
        public void TwoColumns()
        {
            var tableName = "t2";
            var columns = new[]
            {
                (name: "TimeStamp", docString: "Time of day"),
                (name: "ac", docString: "acceleration")
            };
            var command = ParseOneCommand(
                $".alter-merge table {tableName} column-docstrings "
                + $"({string.Join(", ", columns.Select(c => $"{c.name}:\"{c.docString}\""))})");

            ValidateColumnCommand(command, tableName, columns);
        }

        [Fact]
        public void FunkyTableName()
        {
            var tableName = "t 1-";
            var columns = new[]
            {
                (name: "Timestamp", docString: "Time of \\nday")
            };
            var command = ParseOneCommand(
                $".alter-merge table [\"{tableName}\"] column-docstrings "
                + $"({string.Join(", ", columns.Select(c => $"{c.name}:\"{c.docString}\""))})");

            ValidateColumnCommand(command, tableName, columns);
        }

        [Fact]
        public void FunkyColumnName()
        {
            var tableName = "t1";
            var columns = new[]
            {
                (name: "Time.stamp", docString: "Time of \\nday")
            };
            var command = ParseOneCommand(
                $".alter-merge table {tableName} column-docstrings "
                + $"({string.Join(", ", columns.Select(c => $"['{c.name}']:\"{c.docString}\""))})");

            ValidateColumnCommand(command, tableName, columns);
        }

        private static void ValidateColumnCommand(
            CommandBase command,
            string tableName,
            (string name, string docString)[] columns)
        {
            Assert.IsType<AlterMergeTableColumnDocStringsCommand>(command);

            var alterColumnCommand = (AlterMergeTableColumnDocStringsCommand)command;

            Assert.Equal(tableName, alterColumnCommand.TableName);
            Assert.Equal(columns.Length, alterColumnCommand.Columns.Count);
            for (int i = 0; i != columns.Length; ++i)
            {
                Assert.Equal(columns[i].name, alterColumnCommand.Columns[i].ColumnName);
                Assert.Equal(columns[i].docString, alterColumnCommand.Columns[i].DocString);
            }
        }
    }
}