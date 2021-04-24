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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-column"/>
    /// </summary>
    public class DropTableColumnsCommand : CommandBase
    {
        public EntityName TableName { get; }

        public IImmutableList<EntityName> ColumnNames { get; }

        public override string CommandFriendlyName => ".drop table columns";

        internal DropTableColumnsCommand(
            EntityName tableName,
            IImmutableList<EntityName> columnNames)
        {
            TableName = tableName;
            ColumnNames = columnNames;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableNameReference = rootElement.GetUniqueDescendant<NameReference>(
                "Table name",
                n => n.NameInParent == "TableName");
            var columnNameReferences = rootElement.GetDescendants<NameReference>(
                n => n.NameInParent != "TableName");
            var tableName = new EntityName(tableNameReference.Name.SimpleName);
            var columnNames = columnNameReferences
                .Select(n => new EntityName(n.Name.SimpleName))
                .ToImmutableArray();

            return new DropTableColumnsCommand(tableName, columnNames);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherCommand = other as DropTableColumnsCommand;
            var areEqualed = otherCommand != null
                && otherCommand.TableName.Equals(TableName)
                && otherCommand.ColumnNames.ToHashSet().SetEquals(ColumnNames);

            return areEqualed;
        }

        public override string ToScript()
        {
            return $".drop table {TableName} columns ({string.Join(", ", ColumnNames)})";
        }
    }
}