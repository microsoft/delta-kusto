using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class AlterColumnTest : ParsingTestBase
    {
        [Fact]
        public void AlterColumn()
        {
            var command = ParseOneCommand(".alter column t.c type=string");

            Assert.IsType<AlterColumnTypeCommand>(command);

            var alterColumnTypeCommand = (AlterColumnTypeCommand)command;

            Assert.Equal(new EntityName("t"), alterColumnTypeCommand.TableName);
            Assert.Equal(new EntityName("c"), alterColumnTypeCommand.ColumnName);
            Assert.Equal("string", alterColumnTypeCommand.Type);
        }

        [Fact]
        public void AlterColumnFunkyNames()
        {
            var command = ParseOneCommand(".alter column t_able.['c '] type=string");

            Assert.IsType<AlterColumnTypeCommand>(command);

            var alterColumnTypeCommand = (AlterColumnTypeCommand)command;

            Assert.Equal(new EntityName("t_able"), alterColumnTypeCommand.TableName);
            Assert.Equal(new EntityName("c "), alterColumnTypeCommand.ColumnName);
            Assert.Equal("string", alterColumnTypeCommand.Type);
        }
    }
}