using DeltaKustoIntegration.Database;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest.Policies.Update
{
    public class UpdatePolicyTest : AdxIntegrationTestBase
    {
        private const string STATES_FOLDER_PATH = "Policies/Update";

        [Fact]
        public async Task AdxToFile()
        {
            await TestAdxToFile(STATES_FOLDER_PATH);
        }

        [Fact]
        public async Task FileToAdx()
        {
            await TestFileToAdx(STATES_FOLDER_PATH);
        }

        [Fact]
        public async Task AdxToAdx()
        {
            await TestAdxToAdx(STATES_FOLDER_PATH);
        }
    }
}