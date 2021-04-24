using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class DropTablesTest : ParsingTestBase
    {
        [Fact]
        public void DropTables()
        {
            var command = ParseOneCommand(".drop tables (t1, t2, t3)");

            Assert.IsType<DropTablesCommand>(command);

            var dropTablesCommand = (DropTablesCommand)command;

            Assert.True(dropTablesCommand.TableNames.ToHashSet().SetEquals(new[]{
                new EntityName("t1"),
                new EntityName("t2"),
                new EntityName("t3")
            }));
        }

        [Fact]
        public void DropTablesFunkyName()
        {
            var command = ParseOneCommand(".drop tables (['t .1'], ['t.2'], t3)");

            Assert.IsType<DropTablesCommand>(command);

            var dropTablesCommand = (DropTablesCommand)command;

            Assert.True(dropTablesCommand.TableNames.ToHashSet().SetEquals(new[]{
                new EntityName("t .1"),
                new EntityName("t.2"),
                new EntityName("t3")
            }));
        }
    }
}