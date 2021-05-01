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

        [Fact]
        public void FromSomethingToEmpty()
        {
            var currentCommands = Parse(
                ".create table MyTable (rownumber:int) \n\n"
                + ".create table MyTable ingestion csv mapping 'my-mapping' "
                + "'[{\"column\" : \"rownumber\"}]'");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(
                ".create table MyTable (rownumber:int)");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<DropMappingCommand>(delta[0]);
        }

        [Fact]
        public void AlreadyMirror()
        {
            var currentCommands = Parse(
                ".create table MyTable (rownumber:int) \n\n"
                + ".create table MyTable ingestion csv mapping 'my-mapping' "
                + "'[{\"column\" : \"rownumber\"}]'");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(
                ".create table MyTable (rownumber:int) \n\n"
                + ".create table MyTable ingestion csv mapping 'my-mapping' "
                + "'[{\"column\" : \"rownumber\"}]'");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Empty(delta);
        }

        [Fact]
        public void AddOne()
        {
            var currentCommands = Parse(
                ".create table MyTable (rownumber:int) \n\n"
                + ".create table MyTable ingestion csv mapping 'my-mapping' "
                + "'[{\"column\" : \"rownumber\"}]'");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(
                ".create table MyTable (rownumber:int) \n\n"
                + ".create table MyTable ingestion csv mapping 'my-mapping' "
                + "'[{\"column\" : \"rownumber\"}]'\n\n"
                + ".create table MyTable ingestion csv mapping 'my-other-mapping' "
                + "'[{\"column\" : \"rownumber\"}]'\n\n");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<CreateMappingCommand>(delta[0]);
            Assert.Equal("my-other-mapping", ((CreateMappingCommand)delta[0]).MappingName.Text);
        }

        [Fact]
        public void AddOneSameNameDifferentKind()
        {
            var currentCommands = Parse(
                ".create table MyTable (rownumber:int) \n\n"
                + ".create table MyTable ingestion csv mapping 'my-mapping' "
                + "'[{\"column\" : \"rownumber\"}]'");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(
                ".create table MyTable (rownumber:int) \n\n"
                + ".create table MyTable ingestion csv mapping 'my-mapping' "
                + "'[{\"column\" : \"rownumber\"}]'\n\n"
                + ".create table MyTable ingestion json mapping 'my-mapping' "
                + "'[{\"column\" : \"rownumber\"}]'\n\n");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<CreateMappingCommand>(delta[0]);
            Assert.Equal("my-mapping", ((CreateMappingCommand)delta[0]).MappingName.Text);
            Assert.Equal("json", ((CreateMappingCommand)delta[0]).MappingKind);
        }

        [Fact]
        public void DetectDuplicates()
        {
            try
            {
                var commands = Parse(
                    ".create table MyTable (rownumber:int) \n\n"
                    + ".create table MyTable ingestion csv mapping 'my-mapping' "
                    + "'[{\"column\" : \"rownumber\"}]'\n\n"
                    + ".create table MyTable ingestion csv mapping 'my-mapping' "
                    + "'[{\"column\" : \"rownumber\"}]'\n\n");
                var database = DatabaseModel.FromCommands(commands);

                throw new InvalidOperationException("This should have failed by now");
            }
            catch (DeltaException)
            {
            }
        }
    }
}