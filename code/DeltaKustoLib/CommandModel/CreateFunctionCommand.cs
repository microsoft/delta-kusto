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
        public string FunctionName { get; }

        public IImmutableList<TypedParameter> Parameters { get; }

        public string Body { get; }

        public string? Folder { get; }

        public string? DocString { get; }

        public bool? SkipValidation { get; }

        private CreateFunctionCommand(
            string databaseName,
            string functionName,
            IEnumerable<TypedParameter> parameters,
            string functionBody,
            string? folder,
            string? docString,
            bool? skipValidation)
            : base(databaseName)
        {
            FunctionName = functionName;
            Parameters = parameters.ToImmutableArray();
            Body = functionBody;
            Folder = folder;
            DocString = docString;
            SkipValidation = skipValidation;
        }

        public override bool Equals([AllowNull] CommandBase other)
        {
            var otherFunction = other as CreateFunctionCommand;
            var areEqualed = otherFunction != null
                && otherFunction.FunctionName == FunctionName
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
                Folder!=null ? $"folder=\"{Folder}\"" : null,
                DocString!=null ? $"docstring=\"{DocString}\"" : null,
                SkipValidation!=null ? $"skipvalidation=\"{SkipValidation}\"" : null
            };
            var nonEmptyProperties = properties.Where(p => p != null);

            builder.Append(".create-or-alter function ");
            if (nonEmptyProperties.Any())
            {
                builder.Append("with (");
                builder.AppendJoin(", ", nonEmptyProperties);
                builder.Append(") ");
            }
            builder.Append(FunctionName);
            builder.Append(" ");
            builder.Append("(");
            builder.AppendJoin(", ", Parameters.Select(p => $"['{p.ParameterName}']:{p.PrimitiveType}"));
            builder.Append(") ");
            builder.Append("{");
            builder.AppendLine();
            builder.Append(Body);
            builder.AppendLine();
            builder.Append("}");

            return builder.ToString();
        }

        internal static CommandBase FromCode(
            string databaseName,
            CustomCommand customCommand)
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
                databaseName,
                functionName,
                parameters,
                bodyText.Trim(),
                folder,
                docString,
                skipValidation);
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

        private static IEnumerable<TypedParameter> GetParameters(
            FunctionParameters functionParameters)
        {
            var pairDeclarations = functionParameters.GetDescendants<NameAndTypeDeclaration>();
            var names = pairDeclarations
                .Select(p => p.GetDescendants<NameDeclaration>())
                .SelectMany(n => n)
                .Select(n => n.SimpleName);
            var types = pairDeclarations
                .Select(p => p.GetDescendants<PrimitiveTypeExpression>())
                .SelectMany(n => n)
                .Select(n => n.Type.ValueText);
            var parameters = names
                .Zip(types, (n, t) => new TypedParameter(n, t));

            return parameters;
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
    }
}