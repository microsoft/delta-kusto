using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class DropFunctionTest : ParsingTestBase
    {
        [Fact]
        public void Drop()
        {
            var command = ParseOneCommand(".drop function MyFunction");

            Assert.IsType<DropFunctionCommand>(command);

            var dropFunctionCommand = (DropFunctionCommand)command;

            Assert.Equal("MyFunction", dropFunctionCommand.FunctionName.Name);
        }
    }
}