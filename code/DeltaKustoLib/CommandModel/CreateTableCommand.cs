using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    public class CreateTableCommand : CommandBase
    {
        public IImmutableList<ColumnModel> Columns { get; }

        public string? Folder { get; }

        public string? DocString { get; }

        public override string ObjectFriendlyTypeName => ".create table";

        public CreateTableCommand(
            string tableName,
            IEnumerable<ColumnModel> columns,
            string? folder,
            string? docString)
            : base(tableName)
        {
            Columns = columns.ToImmutableArray();
            Folder = string.IsNullOrEmpty(folder) ? null : folder;
            DocString = string.IsNullOrEmpty(docString) ? null : docString;
        }

        internal static CommandBase FromCode(CustomCommand customCommand)
        {
            var (nameDeclaration, columnsNode, withNode) = ExtractRootNodes(customCommand);
            var columns = columnsNode
                .GetDescendants<SeparatedElement<SyntaxElement>>()
                .Select(p => p.GetUniqueDescendant<CustomNode>("Table column"))
                .Select(c => ExtractColumn(c));
            var tableName = nameDeclaration.Name.SimpleName;
            var (folder, docString) = ParseWithNode(withNode);

            return new CreateTableCommand(
                tableName,
                columns,
                folder,
                docString);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherTable = other as CreateTableCommand;
            var areEqualed = otherTable != null
                && otherTable.ObjectName == ObjectName
                //  Check that all columns are equal
                && otherTable.Columns.Zip(Columns, (p1, p2) => p1.Equals(p2)).All(p => p)
                && otherTable.Folder == Folder
                && otherTable.DocString == DocString;

            return areEqualed;
        }

        public override string ToScript()
        {
            var builder = new StringBuilder();
            var properties = new[]
            {
                Folder!=null ? $"folder=\"{EscapeString(Folder)}\"" : null,
                DocString!=null ? $"docstring=\"{EscapeString(DocString)}\"" : null
            };
            var nonEmptyProperties = properties.Where(p => p != null);

            builder.Append(".create table ");
            builder.Append(ObjectName);
            builder.Append(" (");
            builder.AppendJoin(", ", Columns.Select(c => c.ToString()));
            builder.Append(")");
            if (nonEmptyProperties.Any())
            {
                builder.Append(" with (");
                builder.AppendJoin(", ", nonEmptyProperties);
                builder.Append(") ");
            }

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            IImmutableList<CreateFunctionCommand> currentFunctionCommands,
            IImmutableList<CreateFunctionCommand> targetFunctionCommands)
        {
            throw new NotImplementedException();
            //var currentFunctions =
            //    currentFunctionCommands.ToImmutableDictionary(c => c.ObjectName);
            //var currentFunctionNames = currentFunctions.Keys.ToImmutableSortedSet();
            //var targetFunctions =
            //    targetFunctionCommands.ToImmutableDictionary(c => c.ObjectName);
            //var targetFunctionNames = targetFunctions.Keys.ToImmutableSortedSet();
            //var dropFunctionNames = currentFunctionNames.Except(targetFunctionNames);
            //var createFunctionNames = targetFunctionNames.Except(currentFunctionNames);
            //var changedFunctionsNames = targetFunctionNames
            //    .Intersect(currentFunctionNames)
            //    .Where(name => !targetFunctions[name].Equals(currentFunctions[name]));
            //var dropFunctions = dropFunctionNames
            //    .Select(name => new DropFunctionCommand(name));
            //var createAlterFunctions = createFunctionNames
            //    .Concat(changedFunctionsNames)
            //    .Select(name => targetFunctions[name]);

            //return dropFunctions
            //    .Cast<CommandBase>()
            //    .Concat(createAlterFunctions);
        }

        private static (NameDeclaration, SyntaxNode, CustomNode?) ExtractRootNodes(
            CustomCommand customCommand)
        {
            var customNode = customCommand.GetUniqueImmediateDescendant<CustomNode>("Custom node");
            var rootNodes = customNode.GetImmediateDescendants<SyntaxNode>();
            //.Select(n => n.GetDescendants<SyntaxNode>())
            //.Select(l =>
            //{
            //    var (name, _, literal) = l.ExtractChildren<NameDeclaration, TokenName, LiteralExpression>("With properties");

            //    return (name: name.SimpleName, value: (string)literal.LiteralValue);
            //});

            if (rootNodes.Count < 2 || rootNodes.Count > 3)
            {
                throw new DeltaException(
                    $"2 or 3 root nodes are expected but {rootNodes.Count} were found");
            }
            else
            {
                var nameDeclaration = rootNodes[0] as NameDeclaration;
                var columnsNode = rootNodes[1];
                var withNode = rootNodes.Count == 3
                    ? rootNodes[2] as CustomNode
                    : null;

                if (nameDeclaration == null)
                {
                    throw new DeltaException("Name declaration was expected but not found");
                }
                if (withNode == null && rootNodes.Count == 3)
                {
                    throw new DeltaException($"A custom node was expected as last node");
                }

                return (nameDeclaration, columnsNode, withNode);
            }
        }

        private static ColumnModel ExtractColumn(CustomNode columnNode)
        {
            var nameDeclaration = columnNode.GetUniqueDescendant<NameDeclaration>(
                "Table column name");
            var typeDeclaration = columnNode.GetUniqueDescendant<PrimitiveTypeExpression>(
                "Table column type");

            return new ColumnModel(nameDeclaration.Name.SimpleName, typeDeclaration.Type.Text);
        }

        private static (string? folder, string? docString) ParseWithNode(
            CustomNode? withNode)
        {
            if (withNode != null)
            {
                var tokens = withNode
                    .GetDescendants<CustomNode>()
                    .Select(n =>
                    {
                        var elements = n.GetDescendants<SyntaxElement>();
                        var (token, _, litteral, _) = elements.ExtractChildren<
                            SyntaxToken,
                            SyntaxToken,
                            LiteralExpression,
                            SyntaxToken>("with-node children");

                        return (token.Kind, litteral.ConstantValue);
                    });
                string? folder = null;
                string? docString = null;

                foreach (var t in tokens)
                {
                    if (t.Kind == SyntaxKind.FolderKeyword)
                    {
                        folder = t.ConstantValue as string;
                    }
                    else if (t.Kind == SyntaxKind.DocStringKeyword)
                    {
                        docString = t.ConstantValue as string;
                    }
                    else
                    {
                        throw new DeltaException($"Unknown token:  '{t.Kind}'");
                    }
                }

                return (folder, docString);
            }
            else
            {
                return (null, null);
            }
        }

        private static (string? folder, string? docString) GetProperties(
            IEnumerable<SkippedTokens> withNodeTokens)
        {
            if (withNodeTokens.Count() > 1)
            {
                throw new DeltaException(
                    $"With-node expected to be unique but has {withNodeTokens.Count()} nodes");
            }
            else if (withNodeTokens.Count() == 0)
            {
                return (null, null);
            }
            else
            {
                var tokens = withNodeTokens.First().GetDescendants<SyntaxToken>();
                string? folder = null;
                string? docString = null;

                //  Scan the tokens and find the folder / doc string
                //  It's a flat list, not much structure to hook on
                for (int i = 0; i != tokens.Count; ++i)
                {
                    if (tokens[i].Kind == SyntaxKind.FolderKeyword)
                    {
                        folder = GetPropertyValue(tokens, i);
                    }
                    if (tokens[i].Kind == SyntaxKind.DocStringKeyword)
                    {
                        docString = GetPropertyValue(tokens, i);
                    }
                }

                return (folder, docString);
            }
        }

        private static string GetPropertyValue(IReadOnlyList<SyntaxToken> tokens, int index)
        {
            if (tokens.Count < index + 3)
            {
                throw new DeltaException("Not enough tokens in the with-node");
            }
            if (tokens[index + 1].Kind != SyntaxKind.EqualToken)
            {
                throw new DeltaException("with-property name should be followed by equal");
            }
            if (tokens[index + 2].Kind != SyntaxKind.StringLiteralToken)
            {
                throw new DeltaException("with-property value should be a string");
            }

            var value = tokens[index + 2].ValueText;

            return value;
        }
    }
}