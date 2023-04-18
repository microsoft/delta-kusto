using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    /// <summary>
    /// Related to issue https://github.com/microsoft/delta-kusto/issues/91
    /// </summary>
    public class CreateAlterMaterializedViewTest : ParsingTestBase
    {
        [Fact]
        public void Something()
        {
            Assert.False(true);
        }
    }
}