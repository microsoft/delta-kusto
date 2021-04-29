using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class CreateMappingTest : ParsingTestBase
    {
        [Fact]
        public void Create()
        {
            var format = "[{ \"column\" : \"rownumber\", \"DataType\":\"int\", "
                + "\"Properties\":{\"Ordinal\":\"0\"}}]";
            var command = ParseOneCommand(
                ".create table MyTable ingestion csv mapping "
                + $"'Mapping1' '{format}'");

            Assert.IsType<CreateMappingCommand>(command);

            var createMappingCommand = (CreateMappingCommand)command;

            Assert.Equal(new EntityName("MyTable"), createMappingCommand.TableName);
            Assert.Equal(new QuotedText("Mapping1"), createMappingCommand.MappingName);
            Assert.Equal("csv", createMappingCommand.MappingKind);
            Assert.Equal(new QuotedText(format), createMappingCommand.MappingAsJson);
        }

        [Fact]
        public void CreateOrAlter()
        {
            var format = "[{ \"column\" : \"rownumber\", \"DataType\":\"int\", "
                + "\"Properties\":{\"Ordinal\":\"0\"}}]";
            var command = ParseOneCommand(
                ".create-or-alter table MyTable ingestion csv mapping "
                + $"'Mapping1' '{format}'");

            Assert.IsType<CreateMappingCommand>(command);

            var createMappingCommand = (CreateMappingCommand)command;

            Assert.Equal(new EntityName("MyTable"), createMappingCommand.TableName);
            Assert.Equal(new QuotedText("Mapping1"), createMappingCommand.MappingName);
            Assert.Equal("csv", createMappingCommand.MappingKind);
            Assert.Equal(new QuotedText(format), createMappingCommand.MappingAsJson);
        }
    }
}