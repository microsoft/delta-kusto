using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class CreateTableTest : ParsingTestBase
    {
        [Fact]
        public void Create()
        {
            var tableName = "demo_make_series1";
            var columns = new[]
            {
                (name: "TimeStamp", type: "datetime"),
                (name: "BrowserVer", type: "string"),
                (name: "OsVer", type: "string"),
                (name: "Country", type: "string")
            };
            var command = ParseOneCommand(
                $".create table {tableName} "
                + $"({string.Join(", ", columns.Select(c => $"{c.name}:{c.type}"))})");

            ValidateTableCommand(command, tableName, columns, null, null);
        }

        [Fact]
        public void CreateMerge()
        {
            var tableName = "demo_make_series1";
            var columns = new[]
            {
                (name: "TimeStamp", type: "datetime"),
                (name: "BrowserVer", type: "string"),
                (name: "OsVer", type: "string"),
                (name: "Country", type: "string")
            };
            var command = ParseOneCommand(
                $".create-merge table {tableName} "
                + $"({string.Join(", ", columns.Select(c => $"{c.name}:{c.type}"))})");

            ValidateTableCommand(command, tableName, columns, null, null);
        }

        [Fact]
        public void AlterMerge()
        {
            var tableName = "demo_make_series1";
            var columns = new[]
            {
                (name: "TimeStamp", type: "datetime"),
                (name: "BrowserVer", type: "string"),
                (name: "OsVer", type: "string"),
                (name: "Country", type: "string")
            };
            var command = ParseOneCommand(
                $".alter-merge table {tableName} "
                + $"({string.Join(", ", columns.Select(c => $"{c.name}:{c.type}"))})");

            ValidateTableCommand(command, tableName, columns, null, null);
        }

        [Fact]
        public void WithLetsAndProperties()
        {
            var folder = "Demo";
            var docString = "Function calling other function";
            var tableName = "demo_make_series1";
            var columns = new[]
            {
                (name: "TimeStamp", type: "datetime"),
                (name: "BrowserVer", type: "string"),
                (name: "OsVer", type: "string"),
                (name: "Country", type: "string")
            };
            var command = ParseOneCommand(
                $".create-merge table {tableName} "
                + $"({string.Join(", ", columns.Select(c => $"{c.name}:{c.type}"))}) "
                + $"with (folder=\"{folder}\", docstring=\"{docString}\")");

            ValidateTableCommand(command, tableName, columns, folder, docString);
        }

        private static void ValidateTableCommand(
            CommandBase command,
            string tableName,
            (string name, string type)[] columns,
            string? folder,
            string? docString)
        {
            Assert.IsType<CreateTableCommand>(command);

            var createTableCommand = (CreateTableCommand)command;

            Assert.Equal(tableName, createTableCommand.TableName.Name);
            Assert.Equal(columns.Length, createTableCommand.Columns.Count);
            for (int i = 0; i != columns.Length; ++i)
            {
                Assert.Equal(columns[i].name, createTableCommand.Columns[i].ColumnName.Name);
                Assert.Equal(columns[i].type, createTableCommand.Columns[i].PrimitiveType);
            }
            Assert.Equal(folder ?? string.Empty, createTableCommand.Folder?.Text);
            Assert.Equal(docString ?? string.Empty, createTableCommand.DocString?.Text);
        }
    }
}