using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Text;
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

        async Task IActionProvider.ProcessDeltaCommandsAsync(IEnumerable<CommandBase> commands)
        {
            var builder = new StringBuilder();

            foreach (var command in commands)
            {
                var script = command.ToScript();

                builder.Append(script);
                builder.AppendLine();
                builder.AppendLine();
            }

            await _fileGateway.SetFileContentAsync(_filePath, builder.ToString());
        }
    }
}