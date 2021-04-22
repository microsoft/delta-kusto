using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class DropTableColumnsTest : ParsingTestBase
    {
        [Fact]
        public void DropTableColumns()
        {
            var command = ParseOneCommand(".drop table myt columns (c1, c2, c3)");

            Assert.IsType<DropTableColumnsCommand>(command);

            var dropTableColumnsCommand = (DropTableColumnsCommand)command;

            Assert.Equal(new EntityName("myt"), dropTableColumnsCommand.TableName);
            Assert.True(dropTableColumnsCommand.ColumnNames.ToHashSet().SetEquals(new[]{
                new EntityName("c1"),
                new EntityName("c2"),
                new EntityName("c3")
            }));
        }

        [Fact]
        public void DropTableColumnsFunkyNames()
        {
            var command = ParseOneCommand(".drop table ['my t'] columns (['c. 1'], c_2, ['c 3'])");

            Assert.IsType<DropTableColumnsCommand>(command);

            var dropTableColumnsCommand = (DropTableColumnsCommand)command;

            Assert.Equal(new EntityName("my t"), dropTableColumnsCommand.TableName);
            Assert.True(dropTableColumnsCommand.ColumnNames.ToHashSet().SetEquals(new[]{
                new EntityName("c. 1"),
                new EntityName("c_2"),
                new EntityName("c 3")
            }));
        }
    }
}