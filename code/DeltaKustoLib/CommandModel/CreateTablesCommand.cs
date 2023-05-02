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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-merge-table-command"/>
    /// </summary>
    [Command(800, "Create tables")]
    public class CreateTablesCommand : CommandBase
    {
        #region Inner Types
        public class InnerTable
        {
            public InnerTable(
                EntityName tableName,
                IEnumerable<TableColumn> columns)
            {
                TableName = tableName;
                Columns = columns.ToImmutableArray();
            }

            public EntityName TableName { get; }

            public IImmutableList<TableColumn> Columns { get; }

            public override string ToString()
            {
                return TableName
                    + " ("
                    + string.Join(", ", Columns)
                    + ")";
            }

            public override bool Equals(object? obj)
            {
                var otherTable = obj as InnerTable;

                return otherTable != null
                    && otherTable.TableName.Equals(TableName)
                    && otherTable.Columns.OrderBy(c => c.ColumnName.Name)
                    .SequenceEqual(Columns.OrderBy(c => c.ColumnName.Name));
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        public IImmutableList<InnerTable> Tables { get; }

        public QuotedText? Folder { get; }

        public QuotedText? DocString { get; }

        public override string CommandFriendlyName => ".create tables";

        public override string SortIndex => $"{Folder?.Text}_{Tables.First().TableName.Name}";

        public override string ScriptPath => $"tables/create-many";

        public CreateTablesCommand(
            IEnumerable<InnerTable> tables,
            QuotedText? folder,
            QuotedText? docString)
        {
            Tables = tables.ToImmutableArray();
            Folder = folder;
            DocString = docString;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var (folder, docString) = ExtractProperties(rootElement);
            Func<NameDeclaration, InnerTable> tableExtraction = (table) =>
            {
                var columns = table
                    .Parent
                    .GetDescendants<SeparatedElement>()
                    .Select(s => new TableColumn(
                        EntityName.FromCode(s.GetUniqueDescendant<NameDeclaration>("Column Name")),
                        s.GetUniqueDescendant<PrimitiveTypeExpression>("Column Type").Type.Text));

                return new InnerTable(EntityName.FromCode(table), columns);
            };
            var tables = rootElement
                .GetDescendants<NameDeclaration>(n => n.NameInParent == "TableName")
                .Select(t => tableExtraction(t));

            return new CreateTablesCommand(tables, folder, docString);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherTable = other as CreateTablesCommand;
            var areEqualed = otherTable != null
                && otherTable.Tables.OrderBy(t => t.TableName.Name)
                .SequenceEqual(Tables.OrderBy(t => t.TableName.Name))
                && object.Equals(otherTable.Folder, Folder);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();
            var properties = new[]
            {
                Folder != null ? $"folder={Folder}" : null,
                DocString != null ? $"docstring={DocString}" : null
            };
            var nonEmptyProperties = properties.Where(p => p != null);

            builder.Append(".create-merge tables ");
            builder.AppendJoin(", ", Tables);
            if (nonEmptyProperties.Any())
            {
                builder.Append(" with (");
                builder.AppendJoin(", ", nonEmptyProperties);
                builder.Append(") ");
            }

            return builder.ToString();
        }

        private static (QuotedText? folder, QuotedText? docString) ExtractProperties(
            SyntaxElement rootElement)
        {
            var withKeyword = rootElement
                .GetDescendants<SyntaxElement>(n => n.Kind == SyntaxKind.WithKeyword)
                .FirstOrDefault();

            if (withKeyword != null)
            {
                var elements = withKeyword.Parent
                    .GetDescendants<SeparatedElement>();
                QuotedText? folder = null;
                QuotedText? docString = null;

                foreach (var element in elements)
                {
                    var nameDeclaration =
                        element.GetUniqueDescendant<NameDeclaration>("Property name");
                    var valueExpression =
                        element.GetUniqueDescendant<LiteralExpression>("Property value");

                    switch (nameDeclaration.Name.SimpleName.ToLower())
                    {
                        case "folder":
                            folder = QuotedText.FromLiteral(valueExpression);
                            break;
                        case "docstring":
                            docString = QuotedText.FromLiteral(valueExpression);
                            break;
                    }
                }

                return (folder, docString);
            }
            else
            {
                return (null, null);
            }
        }
    }
}