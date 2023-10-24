using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies.IngestionTime;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta.Policies
{
    public class DeltaIngestionTimePolicyTest : ParsingTestBase
    {
        [Fact]
        public void TableFromEmptyToSomething()
        {
            TestIngestionTime(
                null,
                true,
                c =>
                {
                    Assert.True(c.IsEnabled);
                },
                null);
        }

        [Fact]
        public void TableFromSomethingToEmpty()
        {
            TestIngestionTime(
                false,
                null,
                null,
                c => { });
        }

        [Fact]
        public void TableDelta()
        {
            TestIngestionTime(
                true,
                false,
                c =>
                {
                    Assert.False(c.IsEnabled);
                },
                null);
        }

        [Fact]
        public void TableSame()
        {
            TestIngestionTime(
                true,
                true,
                null,
                null);
        }

        private void TestIngestionTime(
            bool? currentState,
            bool? targetState,
            Action<AlterIngestionTimePolicyCommand>? alterAction,
            Action<DeleteIngestionTimePolicyCommand>? deleteAction)
        {
            var createTableCommandText = ".create table A (a:int)\n\n";
            var currentText = currentState != null
                ? new AlterIngestionTimePolicyCommand(
                    new EntityName("A"),
                    currentState.Value).ToScript(null)
                : string.Empty;
            var currentCommands = Parse(createTableCommandText + currentText);
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetText = targetState != null
                ? new AlterIngestionTimePolicyCommand(
                    new EntityName("A"),
                    targetState.Value).ToScript(null)
                : string.Empty;
            var targetCommands = Parse(createTableCommandText + targetText);
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            if (alterAction == null && deleteAction == null)
            {
                Assert.Empty(delta);
            }
            else if (alterAction != null)
            {
                Assert.Single(delta);
                Assert.IsType<AlterIngestionTimePolicyCommand>(delta[0]);

                var alterCommand = (AlterIngestionTimePolicyCommand)delta[0];

                Assert.Equal("A", alterCommand.TableName.Name);
                alterAction(alterCommand);
            }
            else if (deleteAction != null)
            {
                Assert.Single(delta);
                Assert.IsType<DeleteIngestionTimePolicyCommand>(delta[0]);

                var deleteCommand = (DeleteIngestionTimePolicyCommand)delta[0];

                Assert.Equal("A", deleteCommand.TableName.Name);
                deleteAction(deleteCommand);
            }
        }
    }
}