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

            ProcessDeltaCommands(
                builder,
                commands.DropTableCommands,
                "Drop Tables");
            ProcessDeltaCommands(
                builder,
                commands.DropTablesCommands,
                "Drop Tables");
            ProcessDeltaCommands(
                builder,
                commands.DropTableColumnsCommands,
                "Drop Table Columns");
            ProcessDeltaCommands(
                builder,
                commands.DropMappingCommands,
                "Drop Table Ingestion Mappings");
            ProcessDeltaCommands(
                builder,
                commands.DeleteCachingPolicyCommands,
                "Delete Caching Policies");
            ProcessDeltaCommands(
                builder,
                commands.DeleteRetentionPolicyCommands,
                "Delete Retention Policies");
            ProcessDeltaCommands(
                builder,
                commands.DropFunctionCommands,
                "Drop functions");
            ProcessDeltaCommands(
                builder,
                commands.DropFunctionsCommands,
                "Drop functions");
            ProcessDeltaCommands(
                builder,
                commands.AlterColumnTypeCommands,
                "Alter Column Type");
            ProcessDeltaCommands(
                builder,
                commands.CreateTableCommands,
                "Create tables");
            ProcessDeltaCommands(
                builder,
                commands.CreateTablesCommands,
                "Create tables");
            ProcessDeltaCommands(
                builder,
                commands.AlterMergeTableColumnDocStringsCommands,
                "Alter merge table column doc strings");
            ProcessDeltaCommands(
                builder,
                commands.CreateMappingCommands,
                "Create table ingestion mappings");
            ProcessDeltaCommands(
                builder,
                commands.AlterUpdatePolicyCommands,
                "Alter Update Policies");
            ProcessDeltaCommands(
                builder,
                commands.AlterCachingPolicyCommands,
                "Alter Caching Policies");
            ProcessDeltaCommands(
                builder,
                commands
                .AlterTablesRetentionPolicyCommands
                .Cast<CommandBase>()
                .Concat(commands.AlterRetentionPolicyCommands),
                "Alter Retention Policies");
            ProcessDeltaCommands(
                builder,
                commands.CreateFunctionCommands,
                "Create functions");

            await _fileGateway.SetFileContentAsync(
                _filePath,
                builder.ToString(),
                ct);
        }

        private void ProcessDeltaCommands(
            StringBuilder builder,
            IEnumerable<CommandBase> commands,
            string comment)
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