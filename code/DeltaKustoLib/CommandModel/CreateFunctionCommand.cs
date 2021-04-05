using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    public class CreateFunctionCommand : CommandBase
    {
        public IImmutableList<TypedParameterModel> Parameters { get; }

        public string Body { get; }

        public string? Folder { get; }

        public string? DocString { get; }

        public bool SkipValidation { get; }

        public override string ObjectFriendlyTypeName => ".create function";

        public CreateFunctionCommand(
            string functionName,
            IEnumerable<TypedParameterModel> parameters,
            string functionBody,
            string? folder,
            string? docString,
            bool? skipValidation)
            : base(functionName)
        {
            if (functionBody.Trim().StartsWith('{'))
            {
                throw new ArgumentException(
                    $"Body should start with curly braces:  '{functionBody}'",
                    nameof(functionBody));
            }
            Parameters = parameters.ToImmutableArray();
            ValidateNoTableParameterAfterScalar(functionName, Parameters);
            Body = functionBody.Trim().Replace("\r", string.Empty);
            Folder = string.IsNullOrEmpty(folder) ? null : folder;
            DocString = string.IsNullOrEmpty(docString) ? null : docString;
            SkipValidation = skipValidation ?? true;
        }

        internal static CommandBase FromCode(CustomCommand customCommand)
        {
            var (withNode, nameDeclaration, functionDeclaration) = ExtractRootNodes(customCommand);
            var (functionParameters, functionBody) = functionDeclaration
                .GetImmediateDescendants<SyntaxNode>()
                .ExtractChildren<FunctionParameters, FunctionBody>("Function declaration");
            var functionName = nameDeclaration.Name.SimpleName;
            var parameters = GetParameters(functionParameters);
            var bodyText = functionBody.Root.ToString(IncludeTrivia.All).Substring(
                functionBody.OpenBrace.TextStart + 1,
                functionBody.CloseBrace.TextStart - functionBody.OpenBrace.TextStart - 1);
            var (folder, docString, skipValidation) = GetProperties(withNode);

            return new CreateFunctionCommand(
                functionName,
                parameters,
                bodyText.Trim(),
                folder,
                docString,
                skipValidation);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as CreateFunctionCommand;
            var areEqualed = otherFunction != null
                && otherFunction.ObjectName == ObjectName
                //  Check that all parameters are equal
                && otherFunction.Parameters.Zip(Parameters, (p1, p2) => p1.Equals(p2)).All(p => p)
                && otherFunction.Body == Body
                && otherFunction.Folder == Folder
                && otherFunction.DocString == DocString
                && otherFunction.SkipValidation == SkipValidation;

            return areEqualed;
        }

        public override string ToScript()
        {
            var builder = new StringBuilder();
            var properties = new[]
            {
                Folder!=null ? $"folder=\"{EscapeString(Folder)}\"" : null,
                DocString!=null ? $"docstring=\"{EscapeString(DocString)}\"" : null,
                $"skipvalidation=\"{SkipValidation}\""
            };
            var nonEmptyProperties = properties.Where(p => p != null);

            builder.Append(".create-or-alter function ");
            if (nonEmptyProperties.Any())
            {
                builder.Append("with (");
                builder.AppendJoin(", ", nonEmptyProperties);
                builder.Append(") ");
            }
            builder.Append(ObjectName);
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

        internal CreateFunctionCommand ForceSkipValidation()
        {
            return SkipValidation == true
                ? this
                : new CreateFunctionCommand(
                    ObjectName,
                    Parameters,
                    Body,
                    Folder,
                    DocString,
                    true);
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            IImmutableList<CreateFunctionCommand> currentFunctionCommands,
            IImmutableList<CreateFunctionCommand> targetFunctionCommands)
        {
            var currentFunctions =
                currentFunctionCommands.ToImmutableDictionary(c => c.ObjectName);
            var currentFunctionNames = currentFunctions.Keys.ToImmutableSortedSet();
            var targetFunctions =
                targetFunctionCommands.ToImmutableDictionary(c => c.ObjectName);
            var targetFunctionNames = targetFunctions.Keys.ToImmutableSortedSet();
            var dropFunctionNames = currentFunctionNames.Except(targetFunctionNames);
            var createFunctionNames = targetFunctionNames.Except(currentFunctionNames);
            var changedFunctionsNames = targetFunctionNames
                .Intersect(currentFunctionNames)
                .Where(name => !targetFunctions[name].Equals(currentFunctions[name]));
            var dropFunctions = dropFunctionNames
                .Select(name => new DropFunctionCommand(name));
            var createAlterFunctions = createFunctionNames
                .Concat(changedFunctionsNames)
                .Select(name => targetFunctions[name]);

            return dropFunctions
                .Cast<CommandBase>()
                .Concat(createAlterFunctions);
        }

        private static (CustomNode?, NameDeclaration, FunctionDeclaration) ExtractRootNodes(
            CustomCommand customCommand)
        {
            var customNode = customCommand.GetUniqueImmediateDescendant<CustomNode>("Custom node");
            var rootNodes = customNode.GetImmediateDescendants<SyntaxNode>();

            if (rootNodes.Count < 2 || rootNodes.Count > 3)
            {
                throw new DeltaException(
                    $"2 or 3 root nodes are expected but {rootNodes.Count} were found");
            }
            else
            {
                CustomNode? withNode = null;

                if (rootNodes.Count == 3)
                {
                    withNode = rootNodes.First() as CustomNode;

                    if (withNode == null)
                    {
                        throw new DeltaException(
                            $"A custom node was expected as first node but '{rootNodes.First().GetType().Name}' was found instead");
                    }
                }

                var nameDeclaration = rootNodes.SkipLast(1).Last() as NameDeclaration;
                var functionDeclaration = rootNodes.Last() as FunctionDeclaration;

                if (nameDeclaration == null)
                {
                    throw new DeltaException("Name declaration was expected but not found");
                }
                if (functionDeclaration == null)
                {
                    throw new DeltaException("Function declaration was expected but not found");
                }

                return (withNode, nameDeclaration, functionDeclaration);
            }
        }

        private static IEnumerable<TypedParameterModel> GetParameters(
            FunctionParameters functionParameters)
        {
            var typeParameters = functionParameters
                .GetDescendants<FunctionParameter>()
                .Select(fp => GetParameter(fp));

            return typeParameters;
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
                    name.SimpleName,
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
                        name.SimpleName,
                        new TableParameterModel(new ColumnModel[0]));
                }
                else
                {
                    var columns = typeExpression!
                        .Columns
                        .Select(c => c.GetUniqueImmediateDescendant<NameAndTypeDeclaration>("Function parameter table column"))
                        .Select(n => GetColumnSchema(n));
                    var table = new TableParameterModel(columns);

                    return new TypedParameterModel(name.SimpleName, table);
                }
            }
        }

        private static ColumnModel GetColumnSchema(NameAndTypeDeclaration declaration)
        {
            var (name, type) = declaration
                .GetImmediateDescendants<SyntaxNode>()
                .ExtractChildren<NameDeclaration, PrimitiveTypeExpression>("Column pair");

            return new ColumnModel(name.SimpleName, type.Type.ValueText);
        }

        private static bool? GetSkipValidation(string text)
        {
            if (text.ToLower() != "true" && text.ToLower() != "false")
            {
                throw new DeltaException(
                    $"skipvalidation must be 'true' or 'false', it can't be '{text}'");
            }

            var skipValidation = bool.Parse(text);

            return skipValidation;
        }

        private static (string? folder, string? docString, bool? skipValidation) GetProperties(
            CustomNode? withNode)
        {
            if (withNode == null)
            {
                return (null, null, null);
            }
            else
            {
                var properties = withNode!
                    .GetDescendants<CustomNode>()
                    .Select(n => n.GetDescendants<SyntaxNode>())
                    .Select(l =>
                    {
                        var (name, _, literal) = l.ExtractChildren<NameDeclaration, TokenName, LiteralExpression>("With properties");

                        return (name: name.SimpleName, value: (string)literal.LiteralValue);
                    });
                string? folder = null;
                string? docString = null;
                bool? skipValidation = null;

                foreach (var property in properties)
                {
                    switch (property.name)
                    {
                        case "folder":
                            folder = property.value;
                            break;
                        case "docstring":
                            docString = property.value;
                            break;
                        case "skipvalidation":
                            skipValidation = GetSkipValidation(property.value);
                            break;
                        default:
                            throw new DeltaException(
                                $"Unsupported function property name:  '{property.name}'");
                    }
                }

                return (folder, docString, skipValidation);
            }
        }

        private void ValidateNoTableParameterAfterScalar(
            string functionName,
            IImmutableList<TypedParameterModel> parameters)
        {
            //  This implements the rule cited in a note in https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/functions/user-defined-functions#input-arguments
            //  "When using both tabular input arguments and scalar input arguments, put all tabular input arguments before the scalar input arguments."
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
    }
}