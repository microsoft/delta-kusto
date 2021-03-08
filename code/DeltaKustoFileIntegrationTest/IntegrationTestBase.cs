using delta_kusto;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoFileIntegrationTest
{
    public abstract class IntegrationTestBase
    {
        protected async Task<int> RunMainAsync(params string[] args)
        {
            var returnedValue = await Program.Main(args);

            return returnedValue;
        }
    }
}