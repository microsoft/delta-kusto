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

        [Fact]
        public void CreateOrAlterFunctionWithLets()
        {
            var name = "MyFunction4";
            var body = "let limitVar = 100; let result = MyFunction(limitVar); result";
            var command = ParseOneCommand(
                $".create-or-alter function {name}() {{ {body} }}");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal(name, createFunctionCommand.FunctionName);
            Assert.Equal(body, createFunctionCommand.FunctionBody);
        }

        [Fact]
        public void CreateOrAlterFunctionWithLetsAndProperties()
        {
            var name = "MyFunction4";
            var body = "let limitVar = 100; let result = MyFunction(limitVar); result";
            var folder = "Demo";
            var docString = "Function calling other function";
            var skipValidation = true;
            var command = ParseOneCommand(
                $".create-or-alter function with (folder = \"{folder}\", docstring = \"{docString}\", skipvalidation = \"{skipValidation}\") {name} () {{ {body} }}");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal(name, createFunctionCommand.FunctionName);
            Assert.Equal(body, createFunctionCommand.FunctionBody);
            Assert.Equal(folder, createFunctionCommand.Folder);
            Assert.Equal(docString, createFunctionCommand.DocString);
            Assert.Equal(skipValidation, createFunctionCommand.SkipValidation);
        }
    }
}