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
                .Join(", ", parameters.Select(p => $"{p.ParameterName}:{p.PrimitiveType}"));
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
                string.Join(", ", parameters.Select(p => $"{p.ParameterName}:{p.PrimitiveType}"));
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
        public void UUidScalarTypes()
        {
            var name = "UUidScalarTypeFct";
            var body = "42";

            try
            {
                //  According to list in https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/scalar-data-types/
                //  it should be legal but it doesn't work in Web UI
                ParseOneCommand(
                    ".create-or-alter function "
                    + $"{name} (a:uuid) {{ {body} }}");

                throw new Exception("This should have failed with ArgumentNullException by now");
            }
            catch (ArgumentNullException)
            {
            }
        }

        [Fact]
        public void SimpleTableTypeParameter()
        {
            var name = "TableTypeFct";
            var body = "42";
            var command = ParseOneCommand(
                ".create-or-alter function "
                + $"{name} (T:(x:long)) {{ {body} }}");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal(name, createFunctionCommand.FunctionName);
            Assert.Single(createFunctionCommand.Parameters);
            Assert.Equal("T", createFunctionCommand.Parameters.First().ParameterName);
            Assert.NotNull(createFunctionCommand.Parameters.First().ComplexType);
            Assert.Single(createFunctionCommand.Parameters.First().ComplexType!.Columns);
            Assert.Equal("x", createFunctionCommand.Parameters.First().ComplexType!.Columns.First().ColumnName);
            Assert.Equal("long", createFunctionCommand.Parameters.First().ComplexType!.Columns.First().PrimitiveType);
            Assert.Equal(body, createFunctionCommand.Body);
        }

        [Fact]
        public void TableAndPrimitiveTypeParameters()
        {
            var name = "TableAndPrimTypeFct";
            var body = "42";
            var command = ParseOneCommand(
                ".create-or-alter function "
                + $"{name} (a:int, T1:(x:long), T2:(y:double, z:dynamic)) {{ {body} }}");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal(name, createFunctionCommand.FunctionName);
            Assert.Equal(3, createFunctionCommand.Parameters.Count);

            var param1 = createFunctionCommand.Parameters[0];

            Assert.Equal("a", param1.ParameterName);
            Assert.Equal("int", param1.PrimitiveType);

            var param2 = createFunctionCommand.Parameters[1];

            Assert.Equal("T1", param2.ParameterName);
            Assert.NotNull(param2.ComplexType);
            Assert.Single(param2.ComplexType!.Columns);
            Assert.Equal("x", param2.ComplexType!.Columns.First().ColumnName);
            Assert.Equal("long", param2.ComplexType!.Columns.First().PrimitiveType);

            var param3 = createFunctionCommand.Parameters[2];

            Assert.Equal("T2", param3.ParameterName);
            Assert.NotNull(param3.ComplexType);
            Assert.Equal(2, param3.ComplexType!.Columns.Count);
            Assert.Equal("y", param3.ComplexType!.Columns.First().ColumnName);
            Assert.Equal("double", param3.ComplexType!.Columns.First().PrimitiveType);
            Assert.Equal("z", param3.ComplexType!.Columns.Last().ColumnName);
            Assert.Equal("dynamic", param3.ComplexType!.Columns.Last().PrimitiveType);

            Assert.Equal(body, createFunctionCommand.Body);
        }

        [Fact]
        public void IfNotExists()
        {
            throw new NotImplementedException();
        }
    }
}