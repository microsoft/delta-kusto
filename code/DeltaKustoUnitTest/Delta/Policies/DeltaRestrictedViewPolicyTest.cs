using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies.RestrictedView;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta.Policies
{
    public class DeltaRestrictedViewPolicyTest : ParsingTestBase
    {
        [Fact]
        public void TableFromEmptyToSomething()
        {
            TestRestrictedView(
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
            TestRestrictedView(
                false,
                null,
                null,
                c => { });
        }

        [Fact]
        public void TableDelta()
        {
            TestRestrictedView(
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
            TestRestrictedView(
                true,
                true,
                null,
                null);
        }

        private void TestRestrictedView(
            bool? currentState,
            bool? targetState,
            Action<AlterRestrictedViewPolicyCommand>? alterAction,
            Action<DeleteRestrictedViewPolicyCommand>? deleteAction)
        {
            var createTableCommandText = ".create table A (a:int)\n\n";
            var currentText = currentState != null
                ? new AlterRestrictedViewPolicyCommand(
                    new EntityName("A"),
                    currentState.Value).ToScript(null)
                : string.Empty;
            var currentCommands = Parse(createTableCommandText + currentText);
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetText = targetState != null
                ? new AlterRestrictedViewPolicyCommand(
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
                Assert.IsType<AlterRestrictedViewPolicyCommand>(delta[0]);

                var alterCommand = (AlterRestrictedViewPolicyCommand)delta[0];

                Assert.Equal("A", alterCommand.EntityName.Name);
                alterAction(alterCommand);
            }
            else if (deleteAction != null)
            {
                Assert.Single(delta);
                Assert.IsType<DeleteRestrictedViewPolicyCommand>(delta[0]);

                var deleteCommand = (DeleteRestrictedViewPolicyCommand)delta[0];

                Assert.Equal("A", deleteCommand.EntityName.Name);
                deleteAction(deleteCommand);
            }
        }
    }
}