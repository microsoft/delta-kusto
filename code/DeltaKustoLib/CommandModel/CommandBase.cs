using DeltaKustoLib.CommandModel.Policies;
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
                    case "DropTableIngestionMapping":
                        return DropMappingCommand.FromCode(commandBlock);
                    case "AlterTablePolicyUpdate":
                        return AlterUpdatePolicyCommand.FromCode(commandBlock);
                    #region Policies
                    case "AlterTablePolicyAutoDelete":
                        return AlterAutoDeletePolicyCommand.FromCode(commandBlock);
                    case "DeleteTablePolicyAutoDelete":
                        return DeleteAutoDeletePolicyCommand.FromCode(commandBlock);
                    case "AlterDatabasePolicyCaching":
                    case "AlterTablePolicyCaching":
                        var partitioning = commandBlock.GetDescendants<SyntaxElement>(s => s.Kind == SyntaxKind.PartitioningKeyword).Count();

                        if (partitioning == 1)
                        {
                            if (ignoreUnknownCommands)
                            {
                                return null;
                            }
                            else
                            {
                                throw new DeltaException(
                                    $"Can't handle CommandKind 'AlterTablePolicyPartitioning'");
                            }
                        }
                        else
                        {
                            return AlterCachingPolicyCommand.FromCode(commandBlock);
                        }
                    case "DeleteDatabasePolicyCaching":
                    case "DeleteTablePolicyCaching":
                        return DeleteCachingPolicyCommand.FromCode(commandBlock);
                    case "AlterDatabasePolicyIngestionBatching":
                    case "AlterTablePolicyIngestionBatching":
                        return AlterIngestionBatchingPolicyCommand.FromCode(commandBlock);
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
                        return AlterTablesRetentionPolicyCommand.FromCode(commandBlock);
                    case "DeleteDatabasePolicyRetention":
                    case "DeleteTablePolicyRetention":
                        return DeleteRetentionPolicyCommand.FromCode(commandBlock);
                    case "AlterDatabasePolicySharding":
                    case "AlterTablePolicySharding":
                        return AlterShardingPolicyCommand.FromCode(commandBlock);
                    case "DeleteDatabasePolicySharding":
                    case "DeleteTablePolicySharding":
                        return DeleteShardingPolicyCommand.FromCode(commandBlock);
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
            //  .create-or-alter table ingestion mapping isn't a recognized command by the parser
            if (unknownCommand.Parts.Count >= 4
                && unknownCommand.Parts[0].Kind == SyntaxKind.CreateOrAlterKeyword
                && unknownCommand.Parts[1].Kind == SyntaxKind.TableKeyword
                && unknownCommand.Parts.Skip(2).Any(p => p.Kind == SyntaxKind.IngestionKeyword))
            {
                var cutPoint = unknownCommand.Parts[0].TextStart + unknownCommand.Parts[0].FullWidth;
                var newScript = ".create " + script.Substring(cutPoint);

                return ParseAndCreateCommand(newScript, ignoreUnknownCommands);
            }
            //  .create merge tables isn't a recognized command by the parser (for some reason)
            else if (unknownCommand.Parts.Count >= 2
                && unknownCommand.Parts[0].Kind == SyntaxKind.CreateMergeKeyword
                && unknownCommand.Parts[1].Kind == SyntaxKind.TablesKeyword)
            {
                var cutPoint = unknownCommand.Parts[1].TextStart + unknownCommand.Parts[1].FullWidth;
                var newScript = ".create tables " + script.Substring(cutPoint);

                return ParseAndCreateCommand(newScript, ignoreUnknownCommands);
            }
            else
            {
                throw new DeltaException("Unrecognized command");
            }
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
                .Select(l => l.Trim())
                //  Remove comment lines
                .Where(l => !l.StartsWith("//"));
            var currentCommandLines = new List<string>();

            foreach (var line in lines)
            {
                if (line == string.Empty)
                {
                    if (currentCommandLines.Any())
                    {
                        yield return string.Join('\n', currentCommandLines);
                        currentCommandLines.Clear();
                    }
                }
                else
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