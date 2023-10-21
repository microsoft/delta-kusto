using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies.RowLevelSecurity;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta.Policies
{
    public class DeltaRowLevelSecurityPolicyTest : ParsingTestBase
    {
        [Fact]
        public void TableFromEmptyToSomething()
        {
            TestRowLevelSecurity(
                null,
                (true, new QuotedText("MyQuery")),
                c =>
                {
                    Assert.True(c.IsEnabled);
                    Assert.Equal("MyQuery", c.Query.Text);
                },
                null);
        }

        [Fact]
        public void TableFromSomethingToEmpty()
        {
            TestRowLevelSecurity(
                (false, new QuotedText("MyQuery")),
                null,
                null,
                c => { });
        }

        [Fact]
        public void TableDeltaEnabled()
        {
            TestRowLevelSecurity(
                (false, new QuotedText("MyQuery")),
                (true, new QuotedText("MyQuery")),
                c =>
                {
                    Assert.True(c.IsEnabled);
                    Assert.Equal("MyQuery", c.Query.Text);
                },
                null);
        }

        [Fact]
        public void TableDeltaQuery()
        {
            TestRowLevelSecurity(
                (true, new QuotedText("MyQuery")),
                (false, new QuotedText("MyQuery2")),
                c =>
                {
                    Assert.False(c.IsEnabled);
                    Assert.Equal("MyQuery2", c.Query.Text);
                },
                null);
        }

        [Fact]
        public void TableSame()
        {
            TestRowLevelSecurity(
                (true, new QuotedText("MyQuery")),
                (true, new QuotedText("MyQuery")),
                null,
                null);
        }

        private void TestRowLevelSecurity(
            (bool isEnabled, QuotedText query)? currentState,
            (bool isEnabled, QuotedText query)? targetState,
            Action<AlterRowLevelSecurityPolicyCommand>? alterAction,
            Action<DeleteRowLevelSecurityPolicyCommand>? deleteAction)
        {
            var createTableCommandText = ".create table A (a:int)\n\n";
            var currentText = currentState != null
                ? new AlterRowLevelSecurityPolicyCommand(
                    new EntityName("A"),
                    currentState.Value.isEnabled,
                    currentState.Value.query).ToScript(null)
                : string.Empty;
            var currentCommands = Parse(createTableCommandText + currentText);
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetText = targetState != null
                ? new AlterRowLevelSecurityPolicyCommand(
                    new EntityName("A"),
                    targetState.Value.isEnabled,
                    targetState.Value.query).ToScript(null)
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
                Assert.IsType<AlterRowLevelSecurityPolicyCommand>(delta[0]);

                var alterCommand = (AlterRowLevelSecurityPolicyCommand)delta[0];

                Assert.Equal("A", alterCommand.TableName.Name);
                alterAction(alterCommand);
            }
            else if (deleteAction != null)
            {
                Assert.Single(delta);
                Assert.IsType<DeleteRowLevelSecurityPolicyCommand>(delta[0]);

                var deleteCommand = (DeleteRowLevelSecurityPolicyCommand)delta[0];

                Assert.Equal("A", deleteCommand.TableName.Name);
                deleteAction(deleteCommand);
            }
        }
    }
}