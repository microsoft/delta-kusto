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
                new TypedParameter("state", "string"),
                new TypedParameter("source", "string")
            };
            var parameterText = string
                .Join(", ", parameters.Select(p => $"{p.ParameterName}:{p.Type}"));
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
            Assert.True(createFunctionCommand
                .Parameters
                .Zip(parameters, (p1, p2) => p1.Equals(p2))
                .All(p => p));
            Assert.Equal(body, createFunctionCommand.Body);
            Assert.Equal(folder, createFunctionCommand.Folder);
            Assert.Equal(docString, createFunctionCommand.DocString);
            Assert.Equal(skipValidation, createFunctionCommand.SkipValidation);
        }

        [Fact]
        public void AllScalarTypes()
        {
            var name = "AllScalarTypesFct";
            var body = "42";
            //  According to list in https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/scalar-data-types/
            var scalarTypes = new[]
            {
                "bool",
                "boolean",
                "datetime",
                "date",
                "dynamic",
                "guid",
                //  Is uuid legal?
                //"uuid",
                "uniqueid",
                "int",
                "long",
                "real",
                "double",
                "string",
                "timespan",
                "time",
                "decimal"
            };
            var parameters = scalarTypes
                .Select(t => new TypedParameter($"param{t}", t));
            var parameterText =
                string.Join(", ", parameters.Select(p => $"{p.ParameterName}:{p.Type}"));
            var command = ParseOneCommand(
                ".create-or-alter function "
                + $"{name} ({parameterText}) {{ {body} }}");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal(name, createFunctionCommand.FunctionName);
            Assert.True(createFunctionCommand
                .Parameters
                .Zip(parameters, (p1, p2) => new { p1, p2, predicate = p1.Equals(p2) })
                .All(p => p.predicate));
            Assert.Equal(body, createFunctionCommand.Body);
        }

        [Fact]
        public void TableTypeParameter()
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