using DeltaKustoIntegration.Database;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest.Functions
{
    public class FunctionsAdxToFileTest : AdxIntegrationTestBase
    {
        private const string STATES_FOLDER_PATH = "Functions/States";

        [Fact]
        public async Task AdxToFile()
        {
            await TestAdxToFile(STATES_FOLDER_PATH, "outputs/functions/adx-to-file/");
        }

        [Fact]
        public async Task FileToAdx()
        {
            await TestFileToAdx(STATES_FOLDER_PATH, "outputs/functions/file-to-adx/");
        }

        [Fact]
        public async Task AdxToAdx()
        {
            await TestAdxToAdx(STATES_FOLDER_PATH, "outputs/functions/adx-to-adx/");
        }
    }
}