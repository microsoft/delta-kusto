using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class CreateFunctionTest : ParsingTestBase
    {
        [Fact]
        public void Create()
        {
            var command = ParseOneCommand(".create function MyFunction() { 42 }");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal("MyFunction", createFunctionCommand.FunctionName);
            Assert.Equal("42", createFunctionCommand.Body);
        }

        [Fact]
        public void CreateOrAlter()
        {
            var command = ParseOneCommand(".create-or-alter function YourFunction() { 72 }");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal("YourFunction", createFunctionCommand.FunctionName);
            Assert.Equal("72", createFunctionCommand.Body);
        }

        [Fact]
        public void WithLets()
        {
            var name = "MyFunction4";
            var body = "let limitVar = 100; let result = MyFunction(limitVar); result";
            var command = ParseOneCommand(
                $".create-or-alter function {name}() {{ {body} }}");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal(name, createFunctionCommand.FunctionName);
            Assert.Equal(body, createFunctionCommand.Body);
        }

        [Fact]
        public void WithLetsAndProperties()
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
            Assert.Equal(body, createFunctionCommand.Body);
            Assert.Equal(folder, createFunctionCommand.Folder);
            Assert.Equal(docString, createFunctionCommand.DocString);
            Assert.Equal(skipValidation, createFunctionCommand.SkipValidation);
        }

        [Fact]
        public void WithLetsAndPropertiesAndParameters()
        {
            var name = "StormsReportedByStateAndSource";
            var body = "StormEvents | where State == state | where Source == source";
            var parameters = new[]
            {
                new { Name = "state", Type = "string"},
                new { Name = "source", Type = "string"}
            };
            var parameterText = string.Join(", ", parameters.Select(p => $"{p.Name}:{p.Type}"));
            var folder = "StormEventsFunctions";
            var docString = "";
            var skipValidation = true;
            var command = ParseOneCommand(
                ".create-or-alter function with "
                + $"(folder = \"{folder}\", docstring = \"{docString}\", skipvalidation = \"{skipValidation}\") "
                + $"{name} ({parameterText}) {{ {body} }}");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal(name, createFunctionCommand.FunctionName);
            Assert.Equal(body, createFunctionCommand.Body);
            Assert.Equal(folder, createFunctionCommand.Folder);
            Assert.Equal(docString, createFunctionCommand.DocString);
            Assert.Equal(skipValidation, createFunctionCommand.SkipValidation);
        }

        [Fact]
        public void AllParameterTypes()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void IfNotExists()
        {
            throw new NotImplementedException();
        }
    }
}