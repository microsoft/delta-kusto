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

        public MultiFilesActionProvider(
            IFileGateway fileGateway,
            string folderPath)
        {
            _fileGateway = fileGateway;
            _folderPath = folderPath;
        }

        async Task IActionProvider.ProcessDeltaCommandsAsync(
            bool doNotProcessIfDataLoss,
            ActionCommandCollection commands,
            CancellationToken ct)
        {
            var commandGroups = commands
                .AllCommands
                .GroupBy(c => c.ScriptPath);

            foreach (var group in commandGroups)
            {
                var fullPath = Path.Combine(_folderPath, $"{group.Key}.kql");
                var builder = new StringBuilder();

                foreach (var command in group)
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