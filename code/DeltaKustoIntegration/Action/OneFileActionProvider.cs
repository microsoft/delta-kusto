using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;
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

        async Task IActionProvider.ProcessDeltaCommandsAsync(
            bool doNotProcessIfDrops,
            ActionCommandCollection commands)
        {
            var builder = new StringBuilder();

            ProcessDeltaCommands(
                builder,
                commands.DropFunctionCommands,
                "Drop functions");
            ProcessDeltaCommands(
                builder,
                commands.CreateFunctionCommands,
                "Create functions");

            await _fileGateway.SetFileContentAsync(_filePath, builder.ToString());
        }

        private void ProcessDeltaCommands<CT>(
            StringBuilder builder,
            IEnumerable<CT> commands,
            string comment)
            where CT : CommandBase
        {
            if (commands.Any())
            {
                builder.Append("//  ");
                builder.Append(comment);
                builder.AppendLine();
                builder.AppendLine();
            }

            foreach (var command in commands)
            {
                var script = command.ToScript();

                builder.Append(script);
                builder.AppendLine();
                builder.AppendLine();
            }
        }
    }
}