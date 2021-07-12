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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-ingestion-mapping-command"/>
    /// </summary>
    [CommandTypeOrder(400, "Drop Table Columns")]
    public class DropMappingCommand : CommandBase
    {
        public EntityName TableName { get; }

        public string MappingKind { get; }

        public QuotedText MappingName { get; }

        public override string CommandFriendlyName => ".drop ingestion mapping";

        public override string SortIndex => $"{TableName.Name}_{MappingName.Text}_{MappingKind}";

        public DropMappingCommand(
            EntityName tableName,
            string mappingKind,
            QuotedText mappingName)
        {
            TableName = tableName;
            MappingKind = mappingKind.ToLower();
            MappingName = mappingName;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableNameReference = rootElement.GetUniqueDescendant<NameReference>(
                "Table Name",
                n => n.NameInParent == "TableName");
            var mappingNameExpression = rootElement.GetUniqueDescendant<LiteralExpression>(
                "Mapping Name",
                n => n.NameInParent == "MappingName");
            var mappingKindToken = rootElement.GetUniqueDescendant<SyntaxToken>(
                "Mapping Kind",
                n => n.NameInParent == "MappingKind");
            var command = new DropMappingCommand(
                new EntityName(tableNameReference.SimpleName),
                mappingKindToken.Text,
                QuotedText.FromLiteral(mappingNameExpression));

            return command;
        }

        public override bool Equals(CommandBase? other)
        {
            var otherCommand = other as DropMappingCommand;
            var areEqualed = otherCommand != null
                && otherCommand.TableName.Equals(TableName)
                && otherCommand.MappingName.Equals(MappingName)
                && otherCommand.MappingKind.Equals(MappingKind);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".drop table ");
            builder.Append(TableName);
            builder.Append(" ingestion ");
            builder.Append(MappingKind);
            builder.Append(" mapping ");
            builder.Append(MappingName);

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            IImmutableList<CreateFunctionCommand> currentFunctionCommands,
            IImmutableList<CreateFunctionCommand> targetFunctionCommands)
        {
            var currentFunctions =
                currentFunctionCommands.ToImmutableDictionary(c => c.FunctionName);
            var currentFunctionNames = currentFunctions.Keys.ToImmutableSortedSet();
            var targetFunctions =
                targetFunctionCommands.ToImmutableDictionary(c => c.FunctionName);
            var targetFunctionNames = targetFunctions.Keys.ToImmutableSortedSet();
            var dropFunctionNames = currentFunctionNames.Except(targetFunctionNames);
            var createFunctionNames = targetFunctionNames.Except(currentFunctionNames);
            var changedFunctionsNames = targetFunctionNames
                .Intersect(currentFunctionNames)
                .Where(name => !targetFunctions[name].Equals(currentFunctions[name]));
            var dropFunctions = dropFunctionNames
                .Select(name => new DropFunctionCommand(name));
            var createAlterFunctions = createFunctionNames
                .Concat(changedFunctionsNames)
                .Select(name => targetFunctions[name]);

            return dropFunctions
                .Cast<CommandBase>()
                .Concat(createAlterFunctions);
        }
    }
}