using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    public static class CommandCollectionHelper
    {
        public static IImmutableList<DropTablesCommand> MergeToPlural(
            this IEnumerable<DropTableCommand> singularCommands)
        {
            //  We might want to cap batches to a maximum size?
            var pluralCommand = new DropTablesCommand(
                singularCommands
                .Select(c => c.TableName)
                .ToImmutableArray());

            return ImmutableArray.Create(pluralCommand);
        }

        public static IImmutableList<CreateTablesCommand> MergeToPlural(
            this IEnumerable<CreateTableCommand> singularCommands)
        {
            //  We might want to cap batches to a maximum size?
            var pluralCommands = singularCommands
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