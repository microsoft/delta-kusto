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
            if (_usePluralForms)
            {
                await ProcessDeltaCommandsAsync(
                    commands.DropTableCommands.MergeToPlural(),
                    c => "drop",
                    "tables",
                    ct);
            }
            else
            {
                await ProcessDeltaCommandsAsync(
                    commands.DropTableCommands,
                    c => c.TableName.Name,
                    "tables/drop",
                    ct);
            }
            await ProcessDeltaCommandsAsync(
                commands.DropTableColumnsCommands,
                c => c.TableName.Name,
                "columns/drop",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.AlterColumnTypeCommands,
                c => c.TableName.Name,
                "columns/alter-type",
                ct);
            if (_usePluralForms)
            {
                await ProcessDeltaCommandsAsync(
                    commands.CreateTableCommands.MergeToPlural(),
                    c => "create",
                    "tables",
                    ct);
            }
            else
            {
                await ProcessDeltaCommandsAsync(
                    commands.CreateTableCommands,
                    c => c.TableName.Name,
                    "tables/create",
                    ct);
            }
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
                commands.DropFunctionCommands,
                c => c.FunctionName.Name,
                "functions/drop",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.CreateFunctionCommands,
                c => c.FunctionName.Name,
                "functions/create",
                ct);
        }

        private async Task ProcessDeltaCommandsAsync<CT>(
            IEnumerable<CT> commands,
            Func<CT, string> fileNameExtractor,
            string folder,
            CancellationToken ct)
            where CT : CommandBase
        {
            foreach (var command in commands)
            {
                var fileName = $"{fileNameExtractor(command)}.kql";
                var script = command.ToScript();
                var fullPath = Path.Combine(_folderPath, folder, fileName);

                await _fileGateway.SetFileContentAsync(fullPath, script, ct);
            }
        }
    }
}