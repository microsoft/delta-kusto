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

        async Task IActionProvider.ProcessDeltaCommandsAsync(IEnumerable<CommandBase> commands)
        {
            var builder = new StringBuilder();

            ProcessDeltaCommands<DropFunctionCommand>(
                builder,
                commands,
                "Drop functions");
            ProcessDeltaCommands<CreateFunctionCommand>(
                builder,
                commands,
                "Create functions");

            await _fileGateway.SetFileContentAsync(_filePath, builder.ToString());
        }

        private void ProcessDeltaCommands<CT>(
            StringBuilder builder,
            IEnumerable<CommandBase> commands,
            string comment)
            where CT : CommandBase
        {
            var typedCommands = commands
                .OfType<CT>()
                .OrderBy(c => c.ObjectName);

            if (typedCommands.Any())
            {
                builder.Append("//  ");
                builder.Append(comment);
                builder.AppendLine();
                builder.AppendLine();
            }

            foreach (var command in typedCommands)
            {
                var script = command.ToScript();

                builder.Append(script);
                builder.AppendLine();
                builder.AppendLine();
            }
        }
    }
}