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
    [CommandTypeOrder(100, "Drop Tables")]
    public class DropTableCommand : CommandBase, ISingularToPluralCommand
    {
        public EntityName TableName { get; }

        public override string CommandFriendlyName => ".drop table";

        public override string SortIndex => TableName.Name;

        public override string ScriptPath => "tables/drop";

        internal DropTableCommand(EntityName tableName)
        {
            TableName = tableName;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var nameReference = rootElement.GetUniqueDescendant<NameReference>(
                "TableName",
                n => n.NameInParent == "TableName");

            return new DropTableCommand(new EntityName(nameReference.Name.SimpleName));
        }

        public override bool Equals(CommandBase? other)
        {
            var otherTable = other as DropTableCommand;
            var areEqualed = otherTable != null
                && otherTable.TableName.Equals(TableName);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            return $".drop table {TableName}";
        }

        IEnumerable<CommandBase>
            ISingularToPluralCommand.MergeToPlural(IEnumerable<CommandBase> singularCommands)
        {
            if (singularCommands.Any())
            {
                //  We might want to cap batches to a maximum size?
                var pluralCommand = new DropTablesCommand(
                    singularCommands
                    .Cast<DropTableCommand>()
                    .Select(c => c.TableName)
                    .ToImmutableArray());

                return ImmutableArray.Create(pluralCommand);
            }
            else
            {
                return ImmutableArray<DropTablesCommand>.Empty;
            }
        }
    }
}