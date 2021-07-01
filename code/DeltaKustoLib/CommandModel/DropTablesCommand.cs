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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-table-command"/>
    /// </summary>
    public class DropTablesCommand : CommandBase
    {
        public IImmutableList<EntityName> TableNames { get; }

        public override string CommandFriendlyName => ".drop tables";

        internal DropTablesCommand(IImmutableList<EntityName> tableNames)
        {
            if(!tableNames.Any())
            {
                throw new ArgumentNullException(nameof(tableNames), "At least one table name is needed");
            }
            TableNames = tableNames.OrderBy(n => n.Name).ToImmutableArray();
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var nameReferences = rootElement.GetDescendants<NameReference>();
            var names = nameReferences
                .Select(n => new EntityName(n.Name.SimpleName))
                .ToImmutableArray();

            return new DropTablesCommand(names);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherTables = other as DropTablesCommand;
            var areEqualed = otherTables != null
                && otherTables.TableNames.ToHashSet().SetEquals(TableNames);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            return $".drop tables ({string.Join(", ", TableNames.Select(t => t.ToScript()))})";
        }
    }
}