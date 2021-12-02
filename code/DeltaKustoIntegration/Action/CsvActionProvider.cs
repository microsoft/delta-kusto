using CsvHelper;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Action
{
    public class CsvActionProvider : IActionProvider
    {
        #region Inner Types
        private class CommandRow
        {
            public string Command { get; set; } = string.Empty;

            public string ScriptPath { get; set; } = string.Empty;

            public string Script { get; set; } = string.Empty;
        }
        #endregion

        private readonly IFileGateway _fileGateway;
        private readonly string _filePath;

        public CsvActionProvider(IFileGateway fileGateway, string filePath)
        {
            _fileGateway = fileGateway;
            _filePath = filePath;
        }

        async Task IActionProvider.ProcessDeltaCommandsAsync(
            bool doNotProcessIfDataLoss,
            CommandCollection commands,
            CancellationToken ct)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csvWriter.WriteHeader<CommandRow>();
                csvWriter.NextRecord();
                
                foreach (var group in commands.CommandGroups)
                {
                    foreach (var command in group.Commands)
                    {
                        var row = new CommandRow
                        {
                            Command = command.CommandFriendlyName,
                            ScriptPath = command.ScriptPath,
                            Script = command.ToScript()
                        };

                        csvWriter.WriteRecord(row);
                    }
                }
                csvWriter.Flush();
                writer.Flush();
                stream.Flush();

                var csvContent = ASCIIEncoding.UTF8.GetString(stream.ToArray());

                await _fileGateway.SetFileContentAsync(
                    _filePath,
                    csvContent,
                    ct);
            }
        }
    }
}