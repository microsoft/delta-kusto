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

        public MultiFilesActionProvider(IFileGateway fileGateway, string folderPath)
        {
            _fileGateway = fileGateway;
            _folderPath = folderPath;
        }

        async Task IActionProvider.ProcessDeltaCommandsAsync(
            bool doNotProcessIfDrops,
            ActionCommandCollection commands,
            CancellationToken ct)
        {
            await ProcessDeltaCommandsAsync(
                commands.DropFunctionCommands,
                "functions/drop",
                ct);
            await ProcessDeltaCommandsAsync(
                commands.CreateFunctionCommands,
                "functions/create",
                ct);
        }

        private async Task ProcessDeltaCommandsAsync<CT>(
            IEnumerable<CT> commands,
            string folder,
            CancellationToken ct)
            where CT : CommandBase
        {
            foreach (var command in commands)
            {
                var fileName = command.ObjectName + ".kql";
                var script = command.ToScript();
                var fullPath = Path.Combine(_folderPath, folder, fileName);

                await _fileGateway.SetFileContentAsync(fullPath, script, ct);
            }
        }
    }
}