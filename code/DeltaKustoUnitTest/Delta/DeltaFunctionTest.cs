using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta
{
    public class DeltaFunctionTest: ParsingTestBase
    {
        [Fact]
        public void FromEmptyToEmpty()
        {
            var currentCommands = new CommandBase[0];
            var currentDatabase = DatabaseModel.FromCommands("current", currentCommands);
            var targetCommands = new CommandBase[0];
            var targetDatabase = DatabaseModel.FromCommands("target", targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Empty(delta);
        }

        [Fact]
        public void FromEmptyToSomething()
        {
            var currentCommands = new CommandBase[0];
            var currentDatabase = DatabaseModel.FromCommands("current", currentCommands);
            var targetCommands = Parse(".create function MyFunction() { 42 }");
            var targetDatabase = DatabaseModel.FromCommands("target", targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<CreateFunctionCommand>(delta[0]);
        }

        [Fact]
        public void FromSomethingToEmpty()
        {
            var currentCommands = Parse(".create function MyFunction() { 42 }");
            var currentDatabase = DatabaseModel.FromCommands("current", currentCommands);
            var targetCommands = new CommandBase[0];
            var targetDatabase = DatabaseModel.FromCommands("target", targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<DropFunctionCommand>(delta[0]);
        }

        [Fact]
        public void AlreadyMirror()
        {
            var currentCommands = Parse(".create function MyFunction() { 42 }");
            var currentDatabase = DatabaseModel.FromCommands("current", currentCommands);
            var targetCommands = Parse(".create function MyFunction()     { 42 }//Different syntax");
            var targetDatabase = DatabaseModel.FromCommands("target", targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Empty(delta);
        }

        [Fact]
        public void UpdateOne()
        {
            var currentCommands = Parse(".create function MyFunction() { 42 }");
            var currentDatabase = DatabaseModel.FromCommands("current", currentCommands);
            var targetCommands = Parse(".create function MyFunction(){ 42 }\n\n.create function MyOtherFunction(){ 42 }");
            var targetDatabase = DatabaseModel.FromCommands("target", targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<CreateFunctionCommand>(delta[0]);
            Assert.Equal("MyOtherFunction", ((CreateFunctionCommand)delta[0]).FunctionName);
        }
    }
}