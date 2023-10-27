using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta
{
    public class DeltaFunctionTest : ParsingTestBase
    {
        [Fact]
        public void FromEmptyToSomething()
        {
            var currentCommands = new CommandBase[0];
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(".create function MyFunction() { 42 }");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<CreateFunctionCommand>(delta[0]);
        }

        [Fact]
        public void FromSomethingToEmpty()
        {
            var currentCommands = Parse(".create function MyFunction() { 42 }");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = new CommandBase[0];
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<DropFunctionCommand>(delta[0]);
        }

        [Fact]
        public void AlreadyMirror()
        {
            var currentCommands = Parse(".create function MyFunction() { 42 }");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(".create function MyFunction()     { 42 }//Different syntax");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Empty(delta);
        }

        [Fact]
        public void UpdateOne()
        {
            var currentCommands = Parse(".create function MyFunction() { 42 }");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(".create function MyFunction(){ 42 }\n\n.create function MyOtherFunction(){ 42 }");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<CreateFunctionCommand>(delta[0]);
            Assert.Equal("MyOtherFunction", ((CreateFunctionCommand)delta[0]).FunctionName.Name);
        }

        [Fact]
        public void UpdateParameter()
        {
            var currentCommands = Parse(".create function MyFunction(Id:int) { 42 }");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(".create function MyFunction(){ 42 }");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<CreateFunctionCommand>(delta[0]);
            Assert.Equal("MyFunction", ((CreateFunctionCommand)delta[0]).FunctionName.Name);
        }

        [Fact]
        public void UpdateParameterWithDefaultValue()
        {
            var currentCommands = Parse(".create function MyFunction(Id:int) { 42 }");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(".create function MyFunction(Id:int=5){ 42 }");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<CreateFunctionCommand>(delta[0]);
            Assert.Equal("MyFunction", ((CreateFunctionCommand)delta[0]).FunctionName.Name);
        }

        [Fact]
        public void NoUpdateParameterWithDefaultValue()
        {
            var currentCommands = Parse(
                ".create function MyFunction(StartTime:datetime=datetime(null)) { 42 }");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(
                ".create function MyFunction(StartTime:datetime=datetime(null)){ 42 }");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Empty(delta);
        }

        [Fact]
        public void DetectDuplicates()
        {
            try
            {
                var commands = Parse(
                    ".create-or-alter function YourFunction() { 72 }\n\n"
                    + ".create-or-alter function OtherFunction() { 72 }\n\n"
                    + ".create-or-alter function with (folder='myfolder') YourFunction() { 72 }\n\n");
                var database = DatabaseModel.FromCommands(commands);

                throw new InvalidOperationException("This should have failed by now");
            }
            catch (DeltaException)
            {
            }
        }
    }
}