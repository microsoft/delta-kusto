using DeltaKustoLib.SchemaObjects;
using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    /// <summary>
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-alter-function"/>
    /// </summary>
    public class CreateFunctionCommand : CommandBase
    {
        public EntityName FunctionName { get; }

        public IImmutableList<TypedParameterModel> Parameters { get; }

        public string Body { get; }

        public QuotedText? Folder { get; }

        public QuotedText? DocString { get; }

        public bool SkipValidation { get; }

        public override string CommandFriendlyName => ".create function";

        public CreateFunctionCommand(
            EntityName functionName,
            IEnumerable<TypedParameterModel> parameters,
            string functionBody,
            QuotedText? folder,
            QuotedText? docString,
            bool? skipValidation)
        {
            FunctionName = functionName;
            Parameters = parameters.ToImmutableArray();

            if (functionBody.Trim().StartsWith('{'))
            {
                throw new ArgumentException(
                    $"Body should start with curly braces:  '{functionBody}'",
                    nameof(functionBody));
            }
            ValidateNoTableParameterAfterScalar(functionName, Parameters);
            Body = functionBody.Trim().Replace("\r", string.Empty);
            Folder = folder;
            DocString = docString;
            SkipValidation = skipValidation ?? true;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var functionName = EntityName.FromCode(
                rootElement.GetUniqueDescendant<SyntaxElement>(
                    "Function Name",
                    e => e.NameInParent == "FunctionName"));
            var functionDeclaration = rootElement.GetUniqueDescendant<FunctionDeclaration>(
                "Function declaration");
            var body = TrimFunctionSchemaBody(functionDeclaration.Body.ToString());
            var parameters = functionDeclaration
                .Parameters
                .Parameters
                .Select(p => p.Element)
                .Select(fp => GetParameter(fp));
            var (folder, docString, skipValidation) = GetProperties(rootElement);

            return new CreateFunctionCommand(
                functionName,
                parameters,
                body,
                folder,
                docString,
                skipValidation);
        }

        internal static CreateFunctionCommand FromFunctionSchema(FunctionSchema schema)
        {
            var parameters = schema
                .InputParameters
                .Select(i => FromParameterSchema(i));
            var body = TrimFunctionSchemaBody(schema.Body);

            return new CreateFunctionCommand(
                new EntityName(schema.Name),
                parameters,
                body,
                QuotedText.FromText(schema.Folder),
                QuotedText.FromText(schema.DocString),
                true);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as CreateFunctionCommand;
            var areEqualed = otherFunction != null
                && otherFunction.FunctionName.Equals(FunctionName)
                //  Check that all parameters are equal
                && otherFunction.Parameters.Zip(Parameters, (p1, p2) => p1.Equals(p2)).All(p => p)
                && otherFunction.Body.Equals(Body)
                && object.Equals(otherFunction.Folder, Folder)
                && object.Equals(otherFunction.DocString, DocString)
                && object.Equals(otherFunction.SkipValidation, SkipValidation);

            return areEqualed;
        }

        public override string ToScript()
        {
            var builder = new StringBuilder();
            var properties = new[]
            {
                $"folder={Folder ?? QuotedText.Empty}",
                $"docstring={DocString ?? QuotedText.Empty}",
                $"skipvalidation={new QuotedText(SkipValidation.ToString())}"
            };

            builder.Append(".create-or-alter function ");
            builder.Append("with (");
            builder.AppendJoin(", ", properties);
            builder.Append(") ");
            builder.Append(FunctionName);
            builder.Append(" ");
            builder.Append("(");
            builder.AppendJoin(", ", Parameters.Select(p => p.ToString()));
            builder.Append(") ");
            builder.Append("{");
            builder.AppendLine();
            builder.Append(Body);
            builder.AppendLine();
            builder.Append("}");

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            IImmutableList<CreateFunctionCommand> currentFunctionCommands,
            IImmutableList<CreateFunctionCommand> targetFunctionCommands)
        {
            var currentFunctions =
                currentFunctionCommands.ToImmutableDictionary(c => c.FunctionName.Name);
            var currentFunctionNames = currentFunctions.Keys.ToImmutableSortedSet();
            var targetFunctions =
                targetFunctionCommands.ToImmutableDictionary(c => c.FunctionName.Name);
            var targetFunctionNames = targetFunctions.Keys.ToImmutableSortedSet();
            var dropFunctionNames = currentFunctionNames.Except(targetFunctionNames);
            var createFunctionNames = targetFunctionNames.Except(currentFunctionNames);
            var changedFunctionsNames = targetFunctionNames
                .Intersect(currentFunctionNames)
                .Where(name => !targetFunctions[name].Equals(currentFunctions[name]));
            var dropFunctions = dropFunctionNames
                .Select(name => new DropFunctionCommand(new EntityName(name)));
            var createAlterFunctions = createFunctionNames
                .Concat(changedFunctionsNames)
                .Select(name => targetFunctions[name]);

            return dropFunctions
                .Cast<CommandBase>()
                .Concat(createAlterFunctions);
        }

        private static TypedParameterModel GetParameter(FunctionParameter functionParameter)
        {
            var declaration = functionParameter.NameAndType;
            var defaultValue = functionParameter.DefaultValue;
            var (name, type) = declaration
                .GetImmediateDescendants<SyntaxNode>()
                .ExtractChildren<NameDeclaration, TypeExpression>("Parameter pair");

            if (type is PrimitiveTypeExpression)
            {
                var typeExpression = type as PrimitiveTypeExpression;

                return new TypedParameterModel(
                    EntityName.FromCode(name),
                    typeExpression!.Type.ValueText,
                    defaultValue != null ? defaultValue.ToString() : null);
            }
            else
            {
                var typeExpression = type as SchemaTypeExpression;
                var cols = typeExpression!.Columns;

                //  Consider the case T(*)
                if (cols.Count == 1
                    && cols.First().GetImmediateDescendants<NameAndTypeDeclaration>().Count == 0)
                {
                    return new TypedParameterModel(
                        EntityName.FromCode(name),
                        new TableParameterModel(new TableColumn[0]));
                }
                else
                {
                    var columns = typeExpression!
                        .Columns
                        .Select(c => c.GetUniqueImmediateDescendant<NameAndTypeDeclaration>("Function parameter table column"))
                        .Select(n => GetColumnSchema(n));
                    var table = new TableParameterModel(columns);

                    return new TypedParameterModel(
                        EntityName.FromCode(name),
                        table);
                }
            }
        }

        private static TableColumn GetColumnSchema(NameAndTypeDeclaration declaration)
        {
            var (name, type) = declaration
                .GetImmediateDescendants<SyntaxNode>()
                .ExtractChildren<NameDeclaration, PrimitiveTypeExpression>("Column pair");

            return new TableColumn(new EntityName(name.SimpleName), type.Type.ValueText);
        }

        private static (QuotedText? folder, QuotedText? docString, bool? skipValidation)
            GetProperties(SyntaxElement rootElement)
        {
            var propertyMap = rootElement
                .GetDescendants<NameDeclaration>(e => e.NameInParent == "PropertyName")
                .Select(n => new
                {
                    Name = n.Name.SimpleName.ToUpperInvariant(),
                    Value = n.Parent.GetUniqueDescendant<LiteralExpression>(
                        "Property Value").LiteralValue.ToString()
                })
                .ToImmutableDictionary(i => i.Name, i => i.Value);
            Func<string, string?> findValue = (name) =>
            {
                var key = name.ToUpperInvariant();

                if (propertyMap.ContainsKey(key))
                {
                    return propertyMap[key];
                }
                else
                {
                    return null;
                }
            };
            var folderText = findValue("folder");
            var folder = string.IsNullOrWhiteSpace(folderText)
                ? null
                : new QuotedText(folderText);
            var docStringText = findValue("docstring");
            var docString = string.IsNullOrWhiteSpace(docStringText)
                ? null
                : new QuotedText(docStringText);
            var skipValidationText = findValue("skipValidation");
            var skipValidation = skipValidationText == null
                ? null
                : (skipValidationText.ToUpperInvariant() == "FALSE" ? (bool?)false : true);

            return (folder, docString, skipValidation);
        }

        private void ValidateNoTableParameterAfterScalar(
            EntityName functionName,
            IImmutableList<TypedParameterModel> parameters)
        {
            //  This implements the rule cited in a note in
            //  https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/functions/user-defined-functions#input-arguments
            //  "When using both tabular input arguments and scalar input arguments,
            //  put all tabular input arguments before the scalar input arguments."
            if (parameters.Count() > 2)
            {
                var skipEnd = parameters.Take(parameters.Count - 1);
                var skipBeginning = parameters.Skip(1);
                var zipped = skipEnd.Zip(skipBeginning, (p1, p2) => (current: p1, next: p2));
                var violation = zipped
                    .Where(p => p.current.ComplexType == null && p.next.ComplexType != null);
                var firstViolation = violation.FirstOrDefault();

                if (violation.Any())
                {
                    throw new DeltaException(
                        $"In function parameters, table types should preceed scalar parameters.  "
                        + $"This rule isn't respected in function '{functionName}':  "
                        + $"parameter '{firstViolation.next.ParameterName}' is a table parameter "
                        + $"and follows '{firstViolation.current.ParameterName}' which is a scalar");
                }
            }
        }

        private static string TrimFunctionSchemaBody(string body)
        {
            var trimmedBody = body.Trim();

            if (trimmedBody.Length < 2)
            {
                throw new InvalidOperationException(
                    $"Function body should at least be 2 characters but isn't:  {body}");
            }
            if (trimmedBody.First() != '{' || trimmedBody.Last() != '}')
            {
                throw new InvalidOperationException(
                    $"Function body was expected to be surrounded by curly brace but isn't:"
                    + $"  {body}");
            }

            var actualBody = trimmedBody
                .Substring(1, trimmedBody.Length - 2)
                //  This trim removes the carriage return so they don't accumulate in translations
                .Trim();

            return actualBody;
        }

        private static TypedParameterModel FromParameterSchema(InputParameterSchema input)
        {
            return input.CslType == null
                ? new TypedParameterModel(
                    new EntityName(input.Name),
                    new TableParameterModel(
                        input.Columns.Select(
                            c => new TableColumn(new EntityName(c.Name), c.CslType))))
                : new TypedParameterModel(
                    new EntityName(input.Name),
                    input.CslType,
                    input.CslDefaultValue != null ? "=" + input.CslDefaultValue : null);
        }
    }
}