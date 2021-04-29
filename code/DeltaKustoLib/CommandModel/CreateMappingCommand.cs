using DeltaKustoLib.SchemaObjects;
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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-ingestion-mapping-command"/>
    /// </summary>
    public class CreateMappingCommand : CommandBase
    {
        public EntityName TableName { get; }
        
        public string MappingKind { get; }
        
        public QuotedText MappingName { get; }

        public QuotedText MappingAsJson { get; }

        public override string CommandFriendlyName => ".create ingestion mapping";

        public CreateMappingCommand(
            EntityName tableName,
            string mappingKind,
            QuotedText mappingName,
            QuotedText mappingAsJson)
        {
            TableName = tableName;
            MappingKind = mappingKind;
            MappingName = mappingName;
            MappingAsJson = mappingAsJson;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableNameDeclaration = rootElement.GetUniqueDescendant<NameDeclaration>(
                "Table Name",
                n => n.NameInParent == "TableName");
            var mappingNameExpression = rootElement.GetUniqueDescendant<LiteralExpression>(
                "Mapping Name",
                n => n.NameInParent == "MappingName");
            var mappingKindToken = rootElement.GetUniqueDescendant<SyntaxToken>(
                "Mapping Kind",
                n => n.NameInParent == "MappingKind");
            var mappingFormatExpression = rootElement.GetUniqueDescendant<LiteralExpression>(
                "Mapping Format",
                n => n.NameInParent == "MappingFormat");
            var command = new CreateMappingCommand(
                EntityName.FromCode(tableNameDeclaration),
                mappingKindToken.Text,
                QuotedText.FromLiteral(mappingNameExpression),
                QuotedText.FromLiteral(mappingFormatExpression));

            return command;
        }

        internal static CreateFunctionCommand FromFunctionSchema(FunctionSchema schema)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(CommandBase? other)
        {
            var otherCommand = other as CreateMappingCommand;
            var areEqualed = otherCommand != null
                && otherCommand.TableName.Equals(TableName)
                && otherCommand.MappingName.Equals(MappingName)
                && otherCommand.MappingKind.Equals(MappingKind)
                && otherCommand.MappingAsJson.Equals(MappingAsJson);

            return areEqualed;
        }

        public override string ToScript()
        {
            var builder = new StringBuilder();

            builder.Append(".create table ");
            builder.Append(TableName);
            builder.Append(" ingestion ");
            builder.Append(MappingKind);
            builder.Append(" mapping ");
            builder.Append(MappingName);
            builder.Append(" ");
            builder.Append(MappingAsJson);

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