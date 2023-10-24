using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest.Tables
{
    public class TableTest : AdxAutoIntegrationTestBase
    {
        protected override string StatesFolderPath => "Tables";
    }
}