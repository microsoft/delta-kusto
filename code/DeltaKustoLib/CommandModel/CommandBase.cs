using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.AutoDelete;
using DeltaKustoLib.CommandModel.Policies.Caching;
using DeltaKustoLib.CommandModel.Policies.IngestionBatching;
using DeltaKustoLib.CommandModel.Policies.IngestionTime;
using DeltaKustoLib.CommandModel.Policies.Merge;
using DeltaKustoLib.CommandModel.Policies.Partitioning;
using DeltaKustoLib.CommandModel.Policies.RestrictedView;
using DeltaKustoLib.CommandModel.Policies.Retention;
using DeltaKustoLib.CommandModel.Policies.RowLevelSecurity;
using DeltaKustoLib.CommandModel.Policies.Sharding;
using DeltaKustoLib.CommandModel.Policies.StreamingIngestion;
using DeltaKustoLib.CommandModel.Policies.Update;
using Kusto.Language;
using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    public abstract class CommandBase : IEquatable<CommandBase>
    {
        public static IImmutableList<CommandBase> FromScript(
            string script,
            bool ignoreUnknownCommands = false)
        {
            var scripts = SplitCommandScripts(script);
            var commands = scripts
                .Select(s => ParseAndCreateCommand(s, ignoreUnknownCommands))
                .Where(c => c != null)
                .Cast<CommandBase>()
                .ToImmutableArray();

            return commands;
        }

        public abstract string CommandFriendlyName { get; }

        public abstract string SortIndex { get; }

        public abstract string ScriptPath { get; }

        public abstract bool Equals([AllowNull] CommandBase other);

        public abstract string ToScript(ScriptingContext? context = default(ScriptingContext));

        #region Object methods
        public override string ToString()
        {
            return ToScript();
        }

        public override bool Equals(object? obj)
        {
            var command = obj as CommandBase;

            return command != null && this.Equals(command);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        private static CommandBase? ParseAndCreateCommand(
            string script,
            bool ignoreUnknownCommands)
        {
            try
            {
                var code = KustoCode.Parse(script);
                var command = CreateCommand(script, code, ignoreUnknownCommands);

                return command;
            }
            catch (Exception ex)
            {
                throw new DeltaException(
                    $"Issue parsing script",
                    script,
                    ex);
            }
        }

        private static CommandBase? CreateCommand(
            string script,
            KustoCode code,
            bool ignoreUnknownCommands)
        {
            var commandBlock = code.Syntax as CommandBlock;

            if (commandBlock == null)
            {
                throw new DeltaException("Script isn't a command");
            }

            var unknownCommand = commandBlock.GetDescendants<UnknownCommand>().FirstOrDefault();

            if (unknownCommand != null)
            {
                return RerouteUnknownCommand(script, unknownCommand, ignoreUnknownCommands);
            }
            else
            {
#if DEBUG
                var allElements = commandBlock.GetDescendants<SyntaxElement>();
#endif
                var customCommand = commandBlock.GetUniqueDescendant<CustomCommand>("custom command");

                switch (customCommand.CommandKind)
                {
                    case "CreateFunction":
                    case "CreateOrAlterFunction":
                        return CreateFunctionCommand.FromCode(commandBlock);
                    case "DropFunction":
                        return DropFunctionCommand.FromCode(commandBlock);
                    case "DropFunctions":
                        return DropFunctionsCommand.FromCode(commandBlock);
                    case "CreateTable":
                        return CreateTableCommand.FromCode(commandBlock);
                    case "CreateMergeTable":
                        //  We need to do this since the parsing is quite different with the with-node
                        //  between a .create and .create-merge (for unknown reasons)
                        return ParseAndCreateCommand(
                            ReplaceFirstOccurence(script, "create-merge", "create"),
                            ignoreUnknownCommands);
                    case "AlterMergeTable":
                        return ParseAndCreateCommand(
                            ReplaceFirstOccurence(script, "alter-merge", "create"),
                            ignoreUnknownCommands);
                    case "CreateTables":
                    case "CreateMergeTables":
                        return CreateTablesCommand.FromCode(commandBlock);
                    case "DropTable":
                        return DropTableCommand.FromCode(commandBlock);
                    case "DropTables":
                        return DropTablesCommand.FromCode(commandBlock);
                    case "AlterColumnType":
                        return AlterColumnTypeCommand.FromCode(commandBlock);
                    case "AlterMergeTableColumnDocStrings":
                        return AlterMergeTableColumnDocStringsCommand.FromCode(commandBlock);
                    case "DropTableColumns":
                        return DropTableColumnsCommand.FromCode(commandBlock);
                    case "CreateTableIngestionMapping":
                        return CreateMappingCommand.FromCode(commandBlock);
                    case "CreateOrAlterTableIngestionMapping":
                        //  We need to do this since the parsing is quite different with the with-node
                        //  between a .create and .create-or-alter (for unknown reasons)
                        return ParseAndCreateCommand(
                            ReplaceFirstOccurence(script, "create-or-alter", "create"),
                            ignoreUnknownCommands);
                    case "DropTableIngestionMapping":
                        return DropMappingCommand.FromCode(commandBlock);
                    #region Policies
                    case "AlterTablePolicyUpdate":
                        return AlterUpdatePolicyCommand.FromCode(commandBlock);
                    case "DeleteTablePolicyUpdate":
                        return DeleteUpdatePolicyCommand.FromCode(commandBlock);
                    case "AlterTablePolicyAutoDelete":
                        return AlterAutoDeletePolicyCommand.FromCode(commandBlock);
                    case "DeleteTablePolicyAutoDelete":
                        return DeleteAutoDeletePolicyCommand.FromCode(commandBlock);
                    case "AlterDatabasePolicyCaching":
                    case "AlterTablePolicyCaching":
                        return AlterCachingPolicyCommand.FromCode(commandBlock);
                    case "AlterTablesPolicyCaching":
                        return AlterCachingPluralPolicyCommand.FromCode(commandBlock);
                    case "DeleteDatabasePolicyCaching":
                    case "DeleteTablePolicyCaching":
                        return DeleteCachingPolicyCommand.FromCode(commandBlock);
                    case "AlterDatabasePolicyIngestionBatching":
                    case "AlterTablePolicyIngestionBatching":
                        return AlterIngestionBatchingPolicyCommand.FromCode(commandBlock);
                    case "AlterTablesPolicyIngestionBatching":
                        return AlterIngestionBatchingPluralPolicyCommand.FromCode(commandBlock);
                    case "DeleteDatabasePolicyIngestionBatching":
                    case "DeleteTablePolicyIngestionBatching":
                        return DeleteIngestionBatchingPolicyCommand.FromCode(commandBlock);
                    case "AlterDatabasePolicyMerge":
                    case "AlterTablePolicyMerge":
                        return AlterMergePolicyCommand.FromCode(commandBlock);
                    case "DeleteDatabasePolicyMerge":
                    case "DeleteTablePolicyMerge":
                        return DeleteMergePolicyCommand.FromCode(commandBlock);
                    case "AlterDatabasePolicyRetention":
                    case "AlterTablePolicyRetention":
                        return AlterRetentionPolicyCommand.FromCode(commandBlock);
                    case "AlterTablesPolicyRetention":
                        return AlterRetentionPluralTablePolicyCommand.FromCode(commandBlock);
                    case "DeleteDatabasePolicyRetention":
                    case "DeleteTablePolicyRetention":
                        return DeleteRetentionPolicyCommand.FromCode(commandBlock);
                    case "AlterDatabasePolicySharding":
                    case "AlterTablePolicySharding":
                        return AlterShardingPolicyCommand.FromCode(commandBlock);
                    case "DeleteDatabasePolicySharding":
                    case "DeleteTablePolicySharding":
                        return DeleteShardingPolicyCommand.FromCode(commandBlock);
                    case "AlterTablePolicyStreamingIngestion":
                    case "AlterDatabasePolicyStreamingIngestion":
                        return AlterStreamingIngestionPolicyCommand.FromCode(commandBlock);
                    case "DeleteTablePolicyStreamingIngestion":
                    case "DeleteDatabasePolicyStreamingIngestion":
                        return DeleteStreamingIngestionPolicyCommand.FromCode(commandBlock);
                    case "AlterTablePolicyRowLevelSecurity":
                        return AlterRowLevelSecurityPolicyCommand.FromCode(commandBlock);
                    case "DeleteTablePolicyRowLevelSecurity":
                        return DeleteRowLevelSecurityPolicyCommand.FromCode(commandBlock);
                    case "AlterTablePolicyRestrictedViewAccess":
                        return AlterRestrictedViewPolicyCommand.FromCode(commandBlock);
                    case "AlterTablesPolicyRestrictedViewAccess":
                        return AlterRestrictedViewPluralPolicyCommand.FromCode(commandBlock);
                    case "DeleteTablePolicyRestrictedViewAccess":
                        return DeleteRestrictedViewPolicyCommand.FromCode(commandBlock);
                    case "AlterTablePolicyPartitioning":
                        return AlterPartitioningPolicyCommand.FromCode(commandBlock);
                    case "DeleteTablePolicyPartitioning":
                        return DeletePartitioningPolicyCommand.FromCode(commandBlock);
                    case "AlterTablePolicyIngestionTime":
                        return AlterIngestionTimePolicyCommand.FromCode(commandBlock);
                    case "AlterTablesPolicyIngestionTime":
                        return AlterIngestionTimePluralPolicyCommand.FromCode(commandBlock);
                    case "DeleteTablePolicyIngestionTime":
                        return DeleteIngestionTimePolicyCommand.FromCode(commandBlock);
                    #endregion

                    default:
                        if (ignoreUnknownCommands)
                        {
                            return null;
                        }
                        else
                        {
                            throw new DeltaException(
                                $"Can't handle CommandKind '{customCommand.CommandKind}'");
                        }
                }
            }
        }

        private static CommandBase? RerouteUnknownCommand(
            string script,
            UnknownCommand unknownCommand,
            bool ignoreUnknownCommands)
        {
            ////  .create-or-alter table ingestion mapping isn't a recognized command by the parser
            //if (unknownCommand.Parts.Count >= 4
            //    && unknownCommand.Parts[0].Kind == SyntaxKind.CreateOrAlterKeyword
            //    && unknownCommand.Parts[1].Kind == SyntaxKind.TableKeyword
            //    && unknownCommand.Parts.Skip(2).Any(p => p.Kind == SyntaxKind.IngestionKeyword))
            //{
            //    var cutPoint = unknownCommand.Parts[0].TextStart + unknownCommand.Parts[0].FullWidth;
            //    var newScript = ".create " + script.Substring(cutPoint);

            //    return ParseAndCreateCommand(newScript, ignoreUnknownCommands);
            //}
            ////  .create merge tables isn't a recognized command by the parser (for some reason)
            //else if (unknownCommand.Parts.Count >= 2
            //    && unknownCommand.Parts[0].Kind == SyntaxKind.CreateMergeKeyword
            //    && unknownCommand.Parts[1].Kind == SyntaxKind.TablesKeyword)
            //{
            //    var cutPoint = unknownCommand.Parts[1].TextStart + unknownCommand.Parts[1].FullWidth;
            //    var newScript = ".create tables " + script.Substring(cutPoint);
            
            //    return ParseAndCreateCommand(newScript, ignoreUnknownCommands);
            //}
            //else
            //{
            //    return null;
            //}
            throw new NotImplementedException();
        }

        private static string ReplaceFirstOccurence(string script, string oldValue, string newValue)
        {
            var occurenceIndex = script.IndexOf(oldValue);

            if (occurenceIndex == -1)
            {
                throw new InvalidOperationException(
                    $"Script '{script}' should contain '{oldValue}'");
            }

            var newScript = script.Substring(0, occurenceIndex)
                + newValue
                + script.Substring(occurenceIndex + oldValue.Length);

            return newScript;
        }

        private static IEnumerable<string> SplitCommandScripts(string script)
        {
            var lines = script
                .Split('\n')
                //  Remove comment lines
                .Where(l => !l.Trim().StartsWith("//"));
            var currentCommandLines = new List<string>();

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith('.'))
                {
                    if (currentCommandLines.Any())
                    {
                        yield return string.Join('\n', currentCommandLines);
                        currentCommandLines.Clear();
                    }
                    currentCommandLines.Add(line);
                }
                else if(line.Trim() != string.Empty)
                {
                    currentCommandLines.Add(line);
                }
            }

            if (currentCommandLines.Any())
            {
                yield return string.Join('\n', currentCommandLines);
            }
        }
    }
}