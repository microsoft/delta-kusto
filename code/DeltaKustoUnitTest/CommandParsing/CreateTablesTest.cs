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

            ValidateTablesCommand(tableNames, columns, null, null);
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

            ValidateTablesCommand(tableNames, columns, null, null);
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

            ValidateTablesCommand(tableNames, columns, "my\\tables", null);
        }

        [Fact]
        public void CreateThreeWithFolderAndDocString()
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

            ValidateTablesCommand(tableNames, columns, "my\\tables", "A wonderful story");
        }

        private void ValidateTablesCommand(
            string[] tableNames,
            (string name, string type)[][] columns,
            string? folder,
            string? docString)
        {
            var tableParts = tableNames
                .Zip(columns, (t, cols) => $"['{t}'] ({string.Join(", ", cols.Select(c => $"{c.name}:{c.type}"))})");
            var properties = new[]
            {
                folder != null ? $"folder={new QuotedText(folder)}" : null,
                docString != null ? $"docstring={new QuotedText(docString)}" : null
            };
            var nonEmptyProperties = properties.Where(p => p != null);
            var withProperties = !nonEmptyProperties.Any()
                ? string.Empty
                : $" with ({string.Join(", ", nonEmptyProperties)})";
            var commandTexts = new[] { ".create tables", ".create-merge tables" };

            foreach (var commandText in commandTexts)
            {
                var command = ParseOneCommand(
                    $"//body\n   \t{commandText} {string.Join(", ", tableParts)}"
                    + withProperties);

                Assert.IsType<CreateTablesCommand>(command);

                var createTablesCommand = (CreateTablesCommand)command;

                Assert.Equal(folder, createTablesCommand.Folder?.Text);
                Assert.Equal(docString, createTablesCommand.DocString?.Text);
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
}