using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class CreateTablesTest : ParsingTestBase
    {
        [Fact]
        public void CreateOne()
        {
            var tableNames = new[] { "my.table" };
            var columns = new[]
            {
                new[]
                {
                    (name: "TimeStamp", type: "datetime"),
                    (name: "BrowserVer", type: "string")
                }
            };

            ValidateTablesCommand(tableNames, columns, null);
        }

        [Fact]
        public void CreateTwo()
        {
            var tableNames = new[] { "t1", "t2" };
            var columns = new[]
            {
                new[]
                {
                    (name: "TimeStamp", type: "datetime"),
                    (name: "BrowserVer", type: "string")
                },
                new[]
                {
                    (name: "OsVer", type: "string"),
                    (name: "Country", type: "string")
                }
            };

            ValidateTablesCommand(tableNames, columns, null);
        }

        [Fact]
        public void CreateThreeWithFolder()
        {
            var tableNames = new[] { "t 1", "t 2", "tennis" };
            var columns = new[]
            {
                new[]
                {
                    (name: "TimeStamp", type: "datetime"),
                    (name: "BrowserVer", type: "string"),
                    (name: "OsVer", type: "string")
                },
                new[]
                {
                    (name: "Country", type: "int")
                },
                new[]
                {
                    (name: "weight", type: "real")
                }
            };

            ValidateTablesCommand(tableNames, columns, "my\\tables");
        }

        private void ValidateTablesCommand(
            string[] tableNames,
            (string name, string type)[][] columns,
            string? folder)
        {
            var tableParts = tableNames
                .Zip(columns, (t, cols) => $"['{t}'] ({string.Join(", ", cols.Select(c => $"{c.name}:{c.type}"))})");
            var withFolder = folder == null
                ? string.Empty
                : $" with (folder=\"{folder.Replace("\\", "\\\\")}\")";
            var command = ParseOneCommand(
                $".create tables {string.Join(", ", tableParts)}"
                + withFolder);

            Assert.IsType<CreateTablesCommand>(command);

            var createTablesCommand = (CreateTablesCommand)command;

            Assert.Equal(folder, createTablesCommand.Folder?.Text);
            for (int i = 0; i != createTablesCommand.Tables.Count; ++i)
            {
                var table = createTablesCommand.Tables[i];

                Assert.Equal(tableNames[i], table.TableName.Name);
                Assert.Equal(columns[i].Length, table.Columns.Count);
                for (int j = 0; j != columns[i].Length; ++j)
                {
                    Assert.Equal(columns[i][j].name, table.Columns[j].ColumnName.Name);
                    Assert.Equal(columns[i][j].type, table.Columns[j].PrimitiveType);
                }
            }
        }
    }
}