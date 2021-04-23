using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta
{
    public class DeltaTableTest : ParsingTestBase
    {
        [Fact]
        public void FromEmptyToSomething()
        {
            var currentCommands = new CommandBase[0];
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(".create table t1(a: string, b: int)");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<CreateTableCommand>(delta[0]);
        }

        [Fact]
        public void FromSomethingToEmpty()
        {
            var currentCommands = Parse(".create table t1(a: string, b: int)");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = new CommandBase[0];
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<DropTableCommand>(delta[0]);
        }

        [Fact]
        public void NoChanges()
        {
            var currentCommands = Parse(".create table t1(a: string, b: int)");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands =  Parse(".create table t1(a: string, b: int)");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Empty(delta);
        }

        [Fact]
        public void AddingFolder()
        {
            var currentCommands = Parse(".create table t1(a: string, b: int) with (docstring='bla')");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(".create table t1(a: string, b: int) with(docstring='bla', folder='abc')");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<CreateTableCommand>(delta[0]);

            var createTableCommand = (CreateTableCommand)delta[0];

            Assert.Equal(new QuotedText("abc"), createTableCommand.Folder);
            //  This hasn't changed so it shouldn't be part of the command
            Assert.Null(createTableCommand.DocString);
        }
    }
}