using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest
{
    [CollectionDefinition("ADX collection")]
    public class AdxIntegrationTestCollection : ICollectionFixture<AdxDbFixture>
    {
    }
}