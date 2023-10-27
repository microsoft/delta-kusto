using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.AutoDelete;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta.Policies
{
    public class DeltaAutoDeletePolicyTest : ParsingTestBase
    {
        #region Inner types
        private record AutoDeletePolicy
        {
            public string ExpiryDate { get; init; } = string.Empty;

            public DateTime GetExpiryDate() => DateTime.Parse(ExpiryDate);
        }
        #endregion

        [Fact]
        public void TableFromEmptyToSomething()
        {
            var targetDate = new DateTime(2030, 1, 1);

            TestAutoDelete(
                null,
                (targetDate, true),
                c =>
                {
                    var policy = c.DeserializePolicy<AutoDeletePolicy>();

                    Assert.Equal(targetDate, policy.GetExpiryDate());
                },
                null);
        }

        [Fact]
        public void TableFromSomethingToEmpty()
        {
            var targetDate = new DateTime(2030, 6, 21);

            TestAutoDelete(
                (targetDate, false),
                null,
                null,
                c => { });
        }

        [Fact]
        public void TableDelta()
        {
            var currentDate = new DateTime(2031, 11, 21);
            var targetDate = new DateTime(2030, 11, 21);

            TestAutoDelete(
                (currentDate, true),
                (targetDate, true),
                c =>
                {
                    var policy = c.DeserializePolicy<AutoDeletePolicy>();

                    Assert.Equal(targetDate, policy.GetExpiryDate());
                },
                null);
        }

        [Fact]
        public void TableSame()
        {
            var date = new DateTime(2031, 11, 30);

            TestAutoDelete(
                (date, false),
                (date, false),
                null,
                null);
        }

        private void TestAutoDelete(
            (DateTime expiryDate, bool deleteIfNotEmpty)? currentPolicy,
            (DateTime expiryDate, bool deleteIfNotEmpty)? targetPolicy,
            Action<AlterAutoDeletePolicyCommand>? alterAction,
            Action<DeleteAutoDeletePolicyCommand>? deleteAction)
        {
            var createTableCommandText = ".create table A (a:int)\n\n";
            var currentText = currentPolicy != null
                ? new AlterAutoDeletePolicyCommand(
                    new EntityName("A"),
                    currentPolicy.Value.expiryDate,
                    currentPolicy.Value.deleteIfNotEmpty).ToScript(null)
                : string.Empty;
            var currentCommands = Parse(createTableCommandText + currentText);
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetText = targetPolicy != null
                ? new AlterAutoDeletePolicyCommand(
                    new EntityName("A"),
                    targetPolicy.Value.expiryDate,
                    targetPolicy.Value.deleteIfNotEmpty).ToScript(null)
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
                Assert.IsType<AlterAutoDeletePolicyCommand>(delta[0]);

                var alterCommand = (AlterAutoDeletePolicyCommand)delta[0];

                Assert.Equal("A", alterCommand.EntityName.Name);
                alterAction(alterCommand);
            }
            else if (deleteAction != null)
            {
                Assert.Single(delta);
                Assert.IsType<DeleteAutoDeletePolicyCommand>(delta[0]);

                var deleteCommand = (DeleteAutoDeletePolicyCommand)delta[0];

                Assert.Equal("A", deleteCommand.EntityName.Name);
                deleteAction(deleteCommand);
            }
        }
    }
}