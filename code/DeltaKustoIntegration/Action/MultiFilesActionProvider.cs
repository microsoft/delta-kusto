using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Action
{
    public class MultiFilesActionProvider : IActionProvider
    {
        private readonly IFileGateway _fileGateway;
        private readonly string _folderPath;
        private readonly bool _usePluralForms;

        public MultiFilesActionProvider(
            IFileGateway fileGateway,
            string folderPath,
            bool usePluralForms)
        {
            _fileGateway = fileGateway;
            _folderPath = folderPath;
            _usePluralForms = usePluralForms;
        }

        async Task IActionProvider.ProcessDeltaCommandsAsync(
            bool doNotProcessIfDataLoss,
            ActionCommandCollection commands,
            CancellationToken ct)
        {
            await ProcessDeltaCommandsAsync(
                _usePluralForms
                ? commands.DropTableCommands.MergeToPlural().Cast<CommandBase>()
                : commands.DropTableCommands,
                c => "drop",
                "tables",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.DropTableColumnsCommands,
                c => c.TableName.Name,
                "columns/drop",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.DropMappingCommands,
                c => $"{c.TableName.Name}-{c.MappingName}-{c.MappingKind}",
                "tables/ingestion-mappings/drop",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.DeleteCachingPolicyCommands,
                c => $"{c.EntityName.Name}",
                "tables/policies/caching/delete",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.DeleteRetentionPolicyCommands,
                c => $"{c.EntityName.Name}",
                "tables/policies/retention/delete",
                ct);
            await ProcessDeltaCommandsAsync(
                _usePluralForms
                ? commands.DropFunctionCommands.MergeToPlural().Cast<CommandBase>()
                : commands.DropFunctionCommands,
                c => "drop",
                "functions",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.AlterColumnTypeCommands,
                c => c.TableName.Name,
                "columns/alter-type",
                ct);
            await ProcessDeltaCommandsAsync(
                _usePluralForms
                ? commands.CreateTableCommands.MergeToPlural().Cast<CommandBase>()
                : commands.CreateTableCommands,
                c => "create",
                "tables",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.AlterMergeTableColumnDocStringsCommands,
                c => c.TableName.Name,
                "columns/alter-doc-strings",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.CreateMappingCommands,
                c => $"{c.TableName.Name}-{c.MappingName}-{c.MappingKind}",
                "tables/ingestion-mappings",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.AlterUpdatePolicyCommands,
                c => $"{c.TableName.Name}",
                "tables/policies/update",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.AlterCachingPolicyCommands,
                c => $"{c.EntityName.Name}",
                "tables/policies/caching",
                ct);
            if(_usePluralForms)
            {
                await ProcessDeltaCommandsAsync(
                    commands
                    .AlterRetentionPolicyCommands
                    .Where(c => c.EntityType == EntityType.Table)
                    .MergeToPlural(),
                    c => "retention",
                    "tables/policies",
                    ct);
            }
            else
            {
                await ProcessDeltaCommandsAsync(
                    commands.AlterRetentionPolicyCommands.Where(c => c.EntityType == EntityType.Table),
                    c => c.EntityName.Name,
                    "tables/policies/retention",
                    ct);
            }
            await ProcessDeltaCommandsAsync(
                commands.AlterRetentionPolicyCommands.Where(c => c.EntityType == EntityType.Database),
                c => "retention",
                "db/policies",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.CreateFunctionCommands,
                c => c.FunctionName.Name,
                "functions",
                ct);
        }

        private async Task ProcessDeltaCommandsAsync<CT>(
            IEnumerable<CT> commands,
            Func<CT, string> fileNameExtractor,
            string folder,
            CancellationToken ct)
            where CT : CommandBase
        {
            var commandGroups = commands
                .GroupBy(c => fileNameExtractor(c));

            foreach (var group in commandGroups)
            {
                var fileName = $"{group.Key}.kql";
                var fullPath = Path.Combine(_folderPath, folder, fileName);
                var builder = new StringBuilder();

                foreach (var command in commands)
                {
                    var script = command.ToScript();

                    builder.Append(script);
                    builder.AppendLine();
                    builder.AppendLine();
                }
                await _fileGateway.SetFileContentAsync(fullPath, builder.ToString(), ct);
            }
        }
    }
}