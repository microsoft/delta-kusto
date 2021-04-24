using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class DropFunctionsTest : ParsingTestBase
    {
        [Fact]
        public void DropFunctions()
        {
            var command = ParseOneCommand(".drop functions (f1, f2, f3)");

            Assert.IsType<DropFunctionsCommand>(command);

            var dropFunctionsCommand = (DropFunctionsCommand)command;

            Assert.True(dropFunctionsCommand.FunctionNames.ToHashSet().SetEquals(new[]{
                new EntityName("f1"),
                new EntityName("f2"),
                new EntityName("f3")
            }));
        }

        [Fact]
        public void DropFunctionsFunkyName()
        {
            var command = ParseOneCommand(".drop functions (['f .1'], ['f.2'], f3)");

            Assert.IsType<DropFunctionsCommand>(command);

            var dropFunctionsCommand = (DropFunctionsCommand)command;

            Assert.True(dropFunctionsCommand.FunctionNames.ToHashSet().SetEquals(new[]{
                new EntityName("f .1"),
                new EntityName("f.2"),
                new EntityName("f3")
            }));
        }
    }
}