using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class DropTableTest : ParsingTestBase
    {
        [Fact]
        public void DropTable()
        {
            var command = ParseOneCommand(".drop table t1");

            Assert.IsType<DropTableCommand>(command);

            var dropTableCommand = (DropTableCommand)command;

            Assert.Equal("t1", dropTableCommand.TableName.Name);
        }

        [Fact]
        public void DropTableFunkyName()
        {
            var command = ParseOneCommand(".drop table ['t .1']");

            Assert.IsType<DropTableCommand>(command);

            var dropTableCommand = (DropTableCommand)command;

            Assert.Equal("t .1", dropTableCommand.TableName.Name);
        }
    }
}