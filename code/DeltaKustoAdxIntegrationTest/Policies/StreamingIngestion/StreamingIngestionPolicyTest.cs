using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest.Policies.StreamingIngestion
{
    public class StreamingIngestionPolicyTest : AdxAutoIntegrationTestBase
    {
        protected override string StatesFolderPath => "Policies/StreamingIngestion";
    }
}