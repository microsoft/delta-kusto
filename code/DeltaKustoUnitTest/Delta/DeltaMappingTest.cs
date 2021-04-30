using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta
{
    public class DeltaMappingTest : ParsingTestBase
    {
        [Fact]
        public void FromEmptyToSomething()
        {
            var currentCommands = new CommandBase[0];
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(
                ".create table MyTable (rownumber:int) \n\n"
                + ".create table MyTable ingestion csv mapping 'my-mapping' "
                + "'[{\"column\" : \"rownumber\"}]'");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Equal(2, delta.Count);
            Assert.IsType<CreateTableCommand>(delta[0]);
            Assert.IsType<CreateMappingCommand>(delta[1]);
        }
    }
}