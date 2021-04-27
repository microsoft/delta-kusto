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
            var folder = rootElement
                .GetDescendants<SyntaxElement>(n => n.Kind == SyntaxKind.FolderKeyword)
                .Select(n => n.Parent.GetUniqueDescendant<SyntaxToken>(
                    "Folder Name",
                    e => e.Kind == SyntaxKind.StringLiteralToken))
                .Select(n => QuotedText.FromToken(n))
                .FirstOrDefault();
            var docString = rootElement
                .GetDescendants<SyntaxElement>(n => n.Kind == SyntaxKind.DocStringKeyword)
                .Select(n => n.Parent.GetUniqueDescendant<SyntaxToken>(
                    "Doc string",
                    e => e.Kind == SyntaxKind.StringLiteralToken))
                .Select(n => QuotedText.FromToken(n))
                .FirstOrDefault();
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

        public override string ToScript()
        {
            var builder = new StringBuilder();

            builder.Append(".create-merge tables ");
            builder.AppendJoin(", ", Tables);
            if (Folder != null)
            {
                builder.Append("with (folder = ");
                builder.Append(Folder);
                builder.Append(")");
            }

            return builder.ToString();
        }
    }
}