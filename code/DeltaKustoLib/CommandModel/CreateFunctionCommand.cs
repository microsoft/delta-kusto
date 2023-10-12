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
    [Command(1200, "Create functions")]
    public class CreateFunctionCommand : CommandBase
    {
        public EntityName FunctionName { get; }

        public IImmutableList<TypedParameterModel> Parameters { get; }

        public string Body { get; }

        public QuotedText Folder { get; }

        public QuotedText DocString { get; }

        public bool IsView { get; }

        public override string CommandFriendlyName => ".create function";

        public override string SortIndex => $"{Folder?.Text}_{FunctionName.Name}";

        public override string ScriptPath => Folder.Text.Any()
            ? $"functions/create/{FolderHelper.Escape(Folder).Text}/{FunctionName}"
            : $"functions/create/{FunctionName}";

        public CreateFunctionCommand(
            EntityName functionName,
            IEnumerable<TypedParameterModel> parameters,
            string functionBody,
            QuotedText folder,
            QuotedText docString,
            bool isView)
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
            IsView = isView;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var functionNameDeclaration = rootElement.GetFirstDescendant<NameDeclaration>(
                n => n.NameInParent == "FunctionName" || n.NameInParent == string.Empty);
            var functionName = EntityName.FromCode(functionNameDeclaration.Name);
            var functionDeclaration = rootElement
                .GetAtLeastOneDescendant<FunctionDeclaration>("Function declaration")
                .First();
            var body = TrimFunctionSchemaBody(functionDeclaration.Body.ToString());
            var parameters = functionDeclaration
                .Parameters
                .Parameters
                .Select(p => p.Element)
                .Select(fp => GetParameter(fp));
            QuotedText folder;
            QuotedText docString;
            bool isView;

            GetProperties(functionDeclaration, out folder, out docString, out isView);

            return new CreateFunctionCommand(
                functionName,
                parameters,
                body,
                folder,
                docString,
                isView);
        }

        private static void GetProperties(
            FunctionDeclaration functionDeclaration,
            out QuotedText folder,
            out QuotedText docString,
            out bool isView)
        {
            var propertyList = functionDeclaration
                .Parent
                .GetFirstDescendant<SyntaxElement>(
                e => e.Kind == SyntaxKind.List && e.NameInParent == string.Empty);

            //  Default values:
            folder = docString = QuotedText.Empty;
            isView = false;
            if (propertyList != null)
            {
                var properties = propertyList
                    .GetDescendants<SeparatedElement>()
                    .Select(p => new
                    {
                        Name = p.GetUniqueDescendant<NameDeclaration>(
                            "Property name").Name.SimpleName,
                        Value = p.GetAtMostOneDescendant<LiteralExpression>(
                            "Value",
                            e => e.Kind == SyntaxKind.StringLiteralExpression
                            || e.Kind == SyntaxKind.BooleanLiteralExpression)
                    });

                foreach (var p in properties)
                {
                    if (p.Name.Equals("docstring", StringComparison.InvariantCultureIgnoreCase))
                    {
                        docString = QuotedText.FromLiteral(p.Value!);
                    }
                    if (p.Name.Equals("folder", StringComparison.InvariantCultureIgnoreCase))
                    {
                        folder = QuotedText.FromLiteral(p.Value!);
                    }
                    if (p.Name.Equals("view", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var value = p.Value?.LiteralValue;

                        if (!(value is bool))
                        {
                            throw new DeltaException(
                                $"Expected function property 'view' to be boolean but value is {value}");
                        }
                        isView = (bool)value!;
                    }
                }
            }
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as CreateFunctionCommand;
            var areEqualed = otherFunction != null
                && otherFunction.FunctionName.Equals(FunctionName)
                //  Check that all parameters are equal
                && otherFunction.Parameters.Count() == Parameters.Count()
                && otherFunction.Parameters.Zip(Parameters, (p1, p2) => p1.Equals(p2)).All(p => p)
                && otherFunction.Body.Equals(Body)
                && object.Equals(otherFunction.Folder, Folder)
                && object.Equals(otherFunction.DocString, DocString);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();
            var properties = new[]
            {
                $"folder={Folder ?? QuotedText.Empty}",
                $"docstring={DocString ?? QuotedText.Empty}",
                $"skipvalidation=true"
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
            var createdFunctions = DeltaHelper.GetCreated(
                currentFunctionCommands,
                targetFunctionCommands,
                c => c.FunctionName);
            var updatedFunctions = DeltaHelper.GetUpdated(
                currentFunctionCommands,
                targetFunctionCommands,
                c => c.FunctionName);
            var droppedFunctions = DeltaHelper.GetDropped(
                currentFunctionCommands,
                targetFunctionCommands,
                c => c.FunctionName);
            var createCommands = createdFunctions
                .Concat(updatedFunctions.Select(p => p.after));
            var dropCommands = droppedFunctions
                .Select(c => new DropFunctionCommand(c.FunctionName));

            return createCommands
                .Cast<CommandBase>()
                .Concat(dropCommands);
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
    }
}