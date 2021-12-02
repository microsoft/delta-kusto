using CsvHelper;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Functions.ScriptCsv
{
    public class FunctionScriptCsvTest : IntegrationTestBase
    {
        #region Inner Types
        private class CommandRow
        {
            public string Command { get; set; } = string.Empty;

            public string ScriptPath { get; set; } = string.Empty;

            public string Script { get; set; } = string.Empty;
        }
        #endregion

        [Fact]
        public async Task ToScriptFolder()
        {
            var paramsPath = "Functions/ScriptCsv/ToCsv/folder-params.yaml";
            var parameters = await RunParametersAsync(paramsPath);
            var inputPath = parameters.Jobs!.First().Value.Target!.Scripts!.First().FilePath!;
            var inputCommands = await LoadScriptAsync(paramsPath, inputPath);

            Assert.Single(inputCommands);
            Assert.IsType<CreateFunctionCommand>(inputCommands.First());

            var inputFunction = (CreateFunctionCommand)inputCommands.First();

            var outputPath = parameters.Jobs.First().Value.Action!.CsvPath!;
            var rootFolder = Path.GetDirectoryName(paramsPath) ?? "";
            var filePath = Path.Combine(rootFolder!, outputPath);

            using (var reader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = (await csvReader.GetRecordsAsync<CommandRow>().ToEnumerableAsync())
                    .ToArray();

                Assert.Single(records);
                Assert.Equal(inputFunction.CommandFriendlyName, records.First().Command);
                Assert.Equal(inputFunction.ToScript(null), records.First().Script);
            }
        }
    }
}