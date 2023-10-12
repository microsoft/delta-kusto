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
                ".create-or-alter table MyTable ingestion csv mapping "
                + $"'Mapping1' '{format}'");

            Assert.IsType<CreateMappingCommand>(command);

            var createMappingCommand = (CreateMappingCommand)command;

            Assert.Equal(new EntityName("MyTable"), createMappingCommand.TableName);
            Assert.Equal(new QuotedText("Mapping1"), createMappingCommand.MappingName);
            Assert.Equal("csv", createMappingCommand.MappingKind);
            Assert.Equal(new QuotedText(format), createMappingCommand.MappingAsJson);
        }

        [Fact]
        public void CreateWithMultilineStringLiteral()
        {
            var format = @"
```
[
    {
        'column' : 'rownumber',
        'DataType' : 'int',
        'Properties' :
        {
            'Ordinal' : '0'
        }
    }
]
```";
            var command = ParseOneCommand(
                ".create-or-alter table MyTable ingestion csv mapping "
                + $"'Mapping1' {format}");

            Assert.IsType<CreateMappingCommand>(command);

            var createMappingCommand = (CreateMappingCommand)command;

            Assert.Equal(new EntityName("MyTable"), createMappingCommand.TableName);
            Assert.Equal(new QuotedText("Mapping1"), createMappingCommand.MappingName);
            Assert.Equal("csv", createMappingCommand.MappingKind);
            Assert.Contains("rownumber", createMappingCommand.MappingAsJson.Text);
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

        [Fact]
        public void CreateWithMultilines()
        {
            var command = ParseOneCommand(
                ".create table MyTable ingestion csv mapping "
                + "'Map ping1' \n"
                + "'['\n"
                + "  '{'\n"
                + "    '\"column\" : \"rownumber\",'\n"
                + "    '\"DataType\":\"int\",'\n"
                + "    '\"Properties\":{\"Ordinal\":\"0\"}'\n"
                + "  '}'\n"
                + "']'\n"
                );

            Assert.IsType<CreateMappingCommand>(command);

            var createMappingCommand = (CreateMappingCommand)command;

            Assert.Equal(new EntityName("MyTable"), createMappingCommand.TableName);
            Assert.Equal(new QuotedText("Map ping1"), createMappingCommand.MappingName);
            Assert.Equal("csv", createMappingCommand.MappingKind);
            Assert.Contains("rownumber", createMappingCommand.MappingAsJson.Text);
        }

        [Fact]
        public void CreateWithRemoveOldestIfRequired()
        {
            foreach (var flag in new[] { true, false })
            {
                var command = ParseOneCommand(
                    ".create table MyTable ingestion csv mapping "
                    + "'Map ping1' \n"
                    + " '['  \n"
                    + "  '{'\n"
                    + "    '\"column\" : \"rownumber\",'\n"
                    + "    '\"DataType\":\"int\",'\n"
                    + "    '\"Properties\":{\"Ordinal\":\"0\"}'\n"
                    + "  '}'\n"
                    + $"']' with (removeOldestIfRequired={flag})");

                Assert.IsType<CreateMappingCommand>(command);

                var createMappingCommand = (CreateMappingCommand)command;

                Assert.Equal(new EntityName("MyTable"), createMappingCommand.TableName);
                Assert.Equal(new QuotedText("Map ping1"), createMappingCommand.MappingName);
                Assert.Equal("csv", createMappingCommand.MappingKind);
                Assert.Contains("rownumber", createMappingCommand.MappingAsJson.Text);
                Assert.Equal(flag, createMappingCommand.RemoveOldestIfRequired);
            }
        }

        [Fact]
        public void CreateWithRemoveOldestIfRequiredAndStringLiteral()
        {
            foreach (var flag in new[] { true, false })
            {
                var command = ParseOneCommand(
                    $@".create table MyTable ingestion csv mapping 
'Map ping1'
```
[
    {{
        ""column"" : ""rownumber"",
        ""DataType"":""int"",
        ""Properties"":{{""Ordinal"":""0""}}
    }}
]
```
with (removeOldestIfRequired={flag})
");

                Assert.IsType<CreateMappingCommand>(command);

                var createMappingCommand = (CreateMappingCommand)command;

                Assert.Equal(new EntityName("MyTable"), createMappingCommand.TableName);
                Assert.Equal(new QuotedText("Map ping1"), createMappingCommand.MappingName);
                Assert.Equal("csv", createMappingCommand.MappingKind);
                Assert.Contains("rownumber", createMappingCommand.MappingAsJson.Text);
                Assert.Equal(flag, createMappingCommand.RemoveOldestIfRequired);
            }
        }
    }
}