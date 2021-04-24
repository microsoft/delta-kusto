using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class DropFunctionTest : ParsingTestBase
    {
        [Fact]
        public void DropFunction()
        {
            var command = ParseOneCommand(".drop function MyFunction");

            Assert.IsType<DropFunctionCommand>(command);

            var dropFunctionCommand = (DropFunctionCommand)command;

            Assert.Equal("MyFunction", dropFunctionCommand.FunctionName.Name);
        }

        [Fact]
        public void DropFunctionFunkyName()
        {
            var command = ParseOneCommand(".drop function ['m.1']");

            Assert.IsType<DropFunctionCommand>(command);

            var dropFunctionCommand = (DropFunctionCommand)command;

            Assert.Equal("m.1", dropFunctionCommand.FunctionName.Name);
        }
    }
}