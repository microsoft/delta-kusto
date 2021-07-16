using DeltaKustoFileIntegrationTest;
using DeltaKustoIntegration.Database;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest
{
    public abstract class AdxAutoIntegrationTestBase : AdxIntegrationTestBase
    {
        protected abstract string StatesFolderPath { get; }

        public AdxAutoIntegrationTestBase(AdxDbFixture adxDbFixture)
            : base(adxDbFixture)
        {
        }

        [Fact]
        public async Task AdxToFile()
        {
            await TestAdxToFile(StatesFolderPath);
        }

        [Fact]
        public async Task FileToAdx()
        {
            await TestFileToAdx(StatesFolderPath);
        }

        [Fact]
        public async Task AdxToAdx()
        {
            await TestAdxToAdx(StatesFolderPath);
        }
    }
}