using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta.Policies
{
    public class AlterCachingPolicyTest : ParsingTestBase
    {
        [Fact]
        public void TableFromEmptyToSomething()
        {
            var currentCommands = Parse(".create table A (a:int)");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(
                ".create table A (a:int)"
                + "\n\n"
                + ".alter table A policy caching hot=3d");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<AlterCachingPolicyCommand>(delta[0]);

            var cachingCommand = (AlterCachingPolicyCommand)delta[0];

            Assert.Equal("A", cachingCommand.EntityName.Name);
            Assert.Equal("3d", cachingCommand.DurationText);
        }
    }
}