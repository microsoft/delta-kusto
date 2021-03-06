﻿using DeltaKustoLib.CommandModel;
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
        private readonly bool _usePluralForms;

        public OneFileActionProvider(
            IFileGateway fileGateway,
            string filePath,
            bool usePluralForms)
        {
            _fileGateway = fileGateway;
            _filePath = filePath;
            _usePluralForms = usePluralForms;
        }

        async Task IActionProvider.ProcessDeltaCommandsAsync(
            bool doNotProcessIfDataLoss,
            ActionCommandCollection commands,
            CancellationToken ct)
        {
            var builder = new StringBuilder();

            if (_usePluralForms)
            {
                ProcessDeltaCommands(
                    builder,
                    commands.DropTableCommands.MergeToPlural(),
                    "Drop Tables");
            }
            else
            {
                ProcessDeltaCommands(
                    builder,
                    commands.DropTableCommands,
                    "Drop Tables");
            }
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
                commands.DropFunctionCommands,
                "Drop functions");
            ProcessDeltaCommands(
                builder,
                commands.AlterColumnTypeCommands,
                "Alter Column Type");
            if (_usePluralForms)
            {
                ProcessDeltaCommands(
                    builder,
                    commands.CreateTableCommands.MergeToPlural(),
                    "Create tables");
            }
            else
            {
                ProcessDeltaCommands(
                    builder,
                    commands.CreateTableCommands,
                    "Create tables");
            }
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
                commands.CreateFunctionCommands,
                "Create functions");

            await _fileGateway.SetFileContentAsync(
                _filePath,
                builder.ToString(),
                ct);
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