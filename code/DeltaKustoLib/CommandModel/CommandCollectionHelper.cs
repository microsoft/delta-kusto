using DeltaKustoLib.CommandModel.Policies;
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
            if (singularCommands.Any())
            {
                //  We might want to cap batches to a maximum size?
                var pluralCommand = new DropTablesCommand(
                    singularCommands
                    .Select(c => c.TableName)
                    .ToImmutableArray());

                return ImmutableArray.Create(pluralCommand);
            }
            else
            {
                return ImmutableArray<DropTablesCommand>.Empty;
            }
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

        public static IImmutableList<DropFunctionsCommand> MergeToPlural(
            this IEnumerable<DropFunctionCommand> singularCommands)
        {
            if (singularCommands.Any())
            {
                //  We might want to cap batches to a maximum size?
                var functionNames = singularCommands
                    .Select(c => c.FunctionName)
                    .ToImmutableArray();
                var pluralCommand = new DropFunctionsCommand(functionNames);

                return ImmutableArray<DropFunctionsCommand>
                    .Empty
                    .Add(pluralCommand);
            }
            else
            {
                return ImmutableArray<DropFunctionsCommand>.Empty;
            }
        }

        public static IImmutableList<AlterTablesRetentionPolicyCommand> MergeToPlural(
            this IEnumerable<AlterRetentionPolicyCommand> singularCommands)
        {
            if (singularCommands.Any(c => c.EntityType != EntityType.Table))
            {
                throw new ArgumentException(
                    "Expect only table policies",
                    nameof(singularCommands));
            }

            //  We might want to cap batches to a maximum size?
            var pluralCommands = singularCommands
                .Select(c => new { Key = (c.SoftDeletePeriod, c.Recoverability), Value = c })
                .GroupBy(c => c.Key)
                .Select(g => new AlterTablesRetentionPolicyCommand(
                    g.Select(a => a.Value.EntityName),
                    g.Key.SoftDeletePeriod,
                    g.Key.Recoverability));

            return pluralCommands.ToImmutableArray();
        }
    }
}