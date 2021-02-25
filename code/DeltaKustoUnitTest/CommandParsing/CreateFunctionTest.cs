using DeltaKustoLib.CommandModel;
using System;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class CreateFunctionTest : ParsingTestBase
    {
        [Fact]
        public void CreateTrivialScalarFunction()
        {
            var command = ParseOneCommand(".create function MyFunction() { 42 }");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal("MyFunction", createFunctionCommand.FunctionName);
            Assert.Equal("42", createFunctionCommand.FunctionBody);
        }

        [Fact]
        public void CreateOrAlterTrivialScalarFunction()
        {
            var command = ParseOneCommand(".create-or-alter function YourFunction() { 72 }");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal("YourFunction", createFunctionCommand.FunctionName);
            Assert.Equal("72", createFunctionCommand.FunctionBody);
        }
    }
}