using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Action
{
    public class OneFileActionProvider : IActionProvider
    {
        private readonly IFileGateway _fileGateway;
        private readonly string _filePath;

        public OneFileActionProvider(IFileGateway fileGateway, string filePath)
        {
            _fileGateway = fileGateway;
            _filePath = filePath;
        }

        async Task IActionProvider.ProcessDeltaCommandsAsync(
            bool doNotProcessIfDataLoss,
            ActionCommandCollection commands,
            CancellationToken ct)
        {
            var builder = new StringBuilder();

            foreach (var group in commands.CommandGroups)
            {
                builder.Append("//  ");
                builder.Append(group.HeaderComment);
                builder.AppendLine();
                builder.AppendLine();

                foreach (var command in group.Commands)
                {
                    var script = command.ToScript();

                    builder.Append(script);
                    builder.AppendLine();
                    builder.AppendLine();
                }
            }
            await _fileGateway.SetFileContentAsync(
                _filePath,
                builder.ToString(),
                ct);
        }
    }
}