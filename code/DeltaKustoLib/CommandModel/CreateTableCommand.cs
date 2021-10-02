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
    [Command(900, "Create tables")]
    public class CreateTableCommand : CommandBase, ISingularToPluralCommand
    {
        public EntityName TableName { get; }

        public IImmutableList<TableColumn> Columns { get; }

        public QuotedText? Folder { get; }

        public QuotedText? DocString { get; }

        public override string CommandFriendlyName => ".create table";

        public override string SortIndex => $"{Folder?.Text}_{TableName.Name}";

        public override string ScriptPath => Folder != null && Folder.Text.Any()
            ? $"tables/create/{Folder.Text}/{TableName}"
            : $"tables/create/{TableName}";

        internal CreateTableCommand(
            EntityName tableName,
            IEnumerable<TableColumn> columns,
            QuotedText? folder,
            QuotedText? docString)
        {
            TableName = tableName;
            Columns = columns.ToImmutableArray();
            Folder = folder;
            DocString = docString;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableName = rootElement.GetUniqueDescendant<NameDeclaration>(
                "TableName",
                n => n.NameInParent == "TableName");
            var folder = GetProperty(rootElement, SyntaxKind.FolderKeyword);
            var docString = GetProperty(rootElement, SyntaxKind.DocStringKeyword);
            var columns = rootElement
                .GetDescendants<NameDeclaration>(n => n.NameInParent == "ColumnName")
                .Select(n => n.Parent)
                .Select(n => new
                {
                    Name = n.GetUniqueDescendant<NameDeclaration>("Table column name"),
                    Type = n.GetUniqueDescendant<PrimitiveTypeExpression>("Table column type")
                })
                .Select(c => new TableColumn(
                    EntityName.FromCode(c.Name),
                    c.Type.Type.Text));

            return new CreateTableCommand(
                EntityName.FromCode(tableName),
                columns,
                folder,
                docString);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherTable = other as CreateTableCommand;
            var areEqualed = otherTable != null
                && otherTable.TableName.Equals(TableName)
                //  Check that all columns are equal
                && otherTable.Columns.OrderBy(c => c.ColumnName).Zip(
                    Columns.OrderBy(c => c.ColumnName),
                    (p1, p2) => p1.Equals(p2)).All(p => p)
                && object.Equals(otherTable.Folder, Folder)
                && object.Equals(otherTable.DocString, DocString);

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

            builder.Append(".create-merge table ");
            builder.Append(TableName);
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

        private static QuotedText? GetProperty(SyntaxElement rootElement, SyntaxKind kind)
        {
            var literal = rootElement
                .GetDescendants<SyntaxElement>(e => e.Kind == kind)
                .Select(e => e.Parent.GetDescendants<LiteralExpression>().FirstOrDefault())
                .FirstOrDefault();

            return literal == null
                ? null
                : QuotedText.FromLiteral(literal);
        }

        IEnumerable<CommandBase>
            ISingularToPluralCommand.ToPlural(IEnumerable<CommandBase> singularCommands)
        {
            //  We might want to cap batches to a maximum size?
            var pluralCommands = singularCommands
                .Cast<CreateTableCommand>()
                .Select(c => new { Key = (c.Folder, c.DocString), Value = c })
                .GroupBy(c => c.Key)
                .Select(g => new CreateTablesCommand(
                    g.Select(c => new CreateTablesCommand.InnerTable(
                        c.Value.TableName,
                        c.Value.Columns)),
                    g.Key.Folder,
                    g.Key.DocString));

            return pluralCommands.ToImmutableArray();
        }
    }
}