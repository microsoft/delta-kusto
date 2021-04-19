using Kusto.Language.Parsing;
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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-merge-table-column"/>
    /// </summary>
    public class AlterMergeTableColumnDocStringsCommand : CommandBase
    {
        #region Inner Types
        public class ColumnDocString : IEquatable<ColumnDocString>
        {
            public EntityName ColumnName { get; }

            public QuotedText DocString { get; }

            public ColumnDocString(EntityName columnName, QuotedText docString)
            {
                ColumnName = columnName;
                DocString = docString;
            }

            public bool Equals([AllowNull] ColumnDocString other)
            {
                return other != null
                    && ColumnName.Equals(other.ColumnName)
                    && DocString.Equals(other.DocString);
            }

            public override string ToString()
            {
                return $"{ColumnName}:{DocString}";
            }
        }
        #endregion

        public EntityName TableName { get; }

        public IImmutableList<ColumnDocString> Columns { get; }

        public override string CommandFriendlyName => ".alter-merge table column-docstring";

        private AlterMergeTableColumnDocStringsCommand(
            EntityName tableName,
            IEnumerable<ColumnDocString> columns)
        {
            TableName = tableName;
            Columns = columns.ToImmutableArray();
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var identifiers = rootElement
                .GetDescendants<SyntaxNode>(e => e.NameInParent == "Name"
                && e.Kind != SyntaxKind.BracketedName)
                .Select(t => EntityName.FromCode(t));
            var literals = rootElement
                .GetDescendants<LiteralExpression>(e => e.NameInParent == "DocString")
                .Select(l => new QuotedText(l.LiteralValue.ToString()!));

            if (identifiers.Count() < 1)
            {
                throw new DeltaException("There should be at least one identifier in the command");
            }
            if (identifiers.Count() != literals.Count() + 1)
            {
                throw new DeltaException("Mismatch number of identifiers vs literals");
            }

            var tableName = identifiers.First();
            var columns = identifiers
                .Skip(1)
                .Zip(literals, (id, lit) => new ColumnDocString(id, lit));

            return new AlterMergeTableColumnDocStringsCommand(tableName, columns);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherTable = other as AlterMergeTableColumnDocStringsCommand;
            var areEqualed = otherTable != null
                && otherTable.TableName.Equals(TableName)
                //  Check that all columns are equal
                && otherTable.Columns.Zip(Columns, (p1, p2) => p1.Equals(p2)).All(p => p);

            return areEqualed;
        }

        public override string ToScript()
        {
            var builder = new StringBuilder();

            builder.Append(".alter-merge table ");
            builder.Append(TableName);
            builder.Append(" column-docstrings (");
            builder.AppendJoin(
                ", ",
                Columns.Select(c => $"{c.ColumnName}:{c.DocString}"));
            builder.Append(")");

            return builder.ToString();
        }
    }
}