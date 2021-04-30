using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class DropMappingTest : ParsingTestBase
    {
        [Fact]
        public void Drop()
        {
            var command = ParseOneCommand(
                ".drop table MyTable ingestion csv mapping 'Mapping1'");

            Assert.IsType<DropMappingCommand>(command);

            var dropMappingCommand = (DropMappingCommand)command;

            Assert.Equal(new EntityName("MyTable"), dropMappingCommand.TableName);
            Assert.Equal(new QuotedText("Mapping1"), dropMappingCommand.MappingName);
            Assert.Equal("csv", dropMappingCommand.MappingKind);
        }

        [Fact]
        public void DropFunkyNames()
        {
            var command = ParseOneCommand(
                ".drop table MyTable ingestion json mapping 'Mapp.. ing1'");

            Assert.IsType<DropMappingCommand>(command);

            var dropMappingCommand = (DropMappingCommand)command;

            Assert.Equal(new EntityName("MyTable"), dropMappingCommand.TableName);
            Assert.Equal(new QuotedText("Mapp.. ing1"), dropMappingCommand.MappingName);
            Assert.Equal("json", dropMappingCommand.MappingKind);
        }
    }
}