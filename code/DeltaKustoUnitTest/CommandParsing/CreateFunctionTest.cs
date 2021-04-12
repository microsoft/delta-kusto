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
                new TypedParameterModel("state", "string", null),
                new TypedParameterModel("source", "string", null)
            };
            var parameterText = string
                .Join(", ", parameters.Select(p => $"{p.ParameterName}:{p.PrimitiveType}"));
            var folder = "Storm\\Functions";
            var docString = "";
            var skipValidation = true;
            var command = ParseOneCommand(
                ".create-or-alter function with "
                + $"(folder = \"{folder.Replace("\\", "\\\\")}\", "
                + $"docstring = \"{docString}\", skipvalidation = \"{skipValidation}\") "
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
            Assert.Equal(docString, createFunctionCommand.DocString ?? string.Empty);
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
                .Select(t => new TypedParameterModel($"param{t}", t, null));
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
        public void DefaultValueParameter()
        {
            var name = "myfct";
            var body = "print state, qty, tolerance";
            var parameters = new[]
            {
                new TypedParameterModel("state", "string", "=\"dc\""),
                new TypedParameterModel("qty", "int", "=3"),
                new TypedParameterModel("tolerance", "double", "=8.523"),
                new TypedParameterModel("duration", "timespan", "=3d")
            };
            var parameterText = string.Join(", ", parameters.Select(p => p.ToString()));
            var command = ParseOneCommand(
                ".create-or-alter function "
                + $"{name} ({parameterText}) {{ {body} }}");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal(name, createFunctionCommand.FunctionName);
            Assert.True(createFunctionCommand
                .Parameters
                .Zip(parameters, (p1, p2) => p1.Equals(p2))
                .All(p => p));
            Assert.Equal(body, createFunctionCommand.Body);
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
        public void TableWithAnyColumns()
        {
            var name = "TableStarColumn";
            var body = "myTable | count";
            var command = ParseOneCommand(
                $".create-or-alter function {name}(myTable:(*)){{ {body} }} ");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal(name, createFunctionCommand.FunctionName);
            Assert.Single(createFunctionCommand.Parameters);
            Assert.Equal("myTable", createFunctionCommand.Parameters.First().ParameterName);
            Assert.NotNull(createFunctionCommand.Parameters.First().ComplexType);
            Assert.Empty(createFunctionCommand.Parameters.First().ComplexType!.Columns);
            Assert.Equal(body, createFunctionCommand.Body);
        }

        [Fact]
        public void TableAndPrimitiveTypeParameters()
        {
            var name = "TableAndPrimTypeFct";
            var body = "42";
            var command = ParseOneCommand(
                ".create-or-alter function "
                + $"{name} (T1:(x:long), T2:(y:double, z:dynamic), a:int) {{ {body} }}");

            Assert.IsType<CreateFunctionCommand>(command);

            var createFunctionCommand = (CreateFunctionCommand)command;

            Assert.Equal(name, createFunctionCommand.FunctionName);
            Assert.Equal(3, createFunctionCommand.Parameters.Count);

            var param1 = createFunctionCommand.Parameters[0];

            Assert.Equal("T1", param1.ParameterName);
            Assert.NotNull(param1.ComplexType);
            Assert.Single(param1.ComplexType!.Columns);
            Assert.Equal("x", param1.ComplexType!.Columns.First().ColumnName);
            Assert.Equal("long", param1.ComplexType!.Columns.First().PrimitiveType);

            var param2 = createFunctionCommand.Parameters[1];

            Assert.Equal("T2", param2.ParameterName);
            Assert.NotNull(param2.ComplexType);
            Assert.Equal(2, param2.ComplexType!.Columns.Count);
            Assert.Equal("y", param2.ComplexType!.Columns.First().ColumnName);
            Assert.Equal("double", param2.ComplexType!.Columns.First().PrimitiveType);
            Assert.Equal("z", param2.ComplexType!.Columns.Last().ColumnName);
            Assert.Equal("dynamic", param2.ComplexType!.Columns.Last().PrimitiveType);

            Assert.Equal(body, createFunctionCommand.Body);

            var param3 = createFunctionCommand.Parameters[2];

            Assert.Equal("a", param3.ParameterName);
            Assert.Equal("int", param3.PrimitiveType);
        }
    }
}