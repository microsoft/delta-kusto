using DeltaKustoIntegration.Database;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest.Policies.Merge
{
    public class MergePolicyTest : AdxAutoIntegrationTestBase
    {
        protected override string StatesFolderPath => "Policies/Merge";
    }
}