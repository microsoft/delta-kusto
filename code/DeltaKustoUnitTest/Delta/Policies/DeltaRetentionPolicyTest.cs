using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.Retention;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta.Policies
{
    public class DeltaRetentionPolicyTest : ParsingTestBase
    {
        #region Inner types
        private record RetentionPolicy
        {
            public string SoftDeletePeriod { get; init; } = string.Empty;

            public TimeSpan GetSoftDeletePeriod() => TimeSpan.Parse(SoftDeletePeriod);
        }
        #endregion

        [Fact]
        public void TableFromEmptyToSomething()
        {
            TestRetention(
                null,
                (TimeSpan.FromDays(3), true),
                c =>
                {
                    var policy = c.DeserializePolicy<RetentionPolicy>();

                    Assert.Equal(TimeSpan.FromDays(3), policy.GetSoftDeletePeriod());
                },
                null);
        }

        [Fact]
        public void TableFromSomethingToEmpty()
        {
            TestRetention(
                (TimeSpan.FromMinutes(7), false),
                null,
                null,
                c => { });
        }

        [Fact]
        public void TableDelta()
        {
            var targetDuration = TimeSpan.FromDays(25) + TimeSpan.FromHours(4);

            TestRetention(
                (TimeSpan.FromDays(3), true),
                (targetDuration, true),
                c =>
                {
                    var policy = c.DeserializePolicy<RetentionPolicy>();
                    
                    Assert.Equal(targetDuration, policy.GetSoftDeletePeriod());
                },
                null);
        }

        [Fact]
        public void TableSame()
        {
            TestRetention(
                (TimeSpan.FromMilliseconds(45), false),
                (TimeSpan.FromMilliseconds(45), false),
                null,
                null);
        }

        private void TestRetention(
            (TimeSpan softDeletePeriod, bool recoverability)? currentPolicy,
            (TimeSpan softDeletePeriod, bool recoverability)? targetPolicy,
            Action<AlterRetentionPolicyCommand>? alterAction,
            Action<DeleteRetentionPolicyCommand>? deleteAction)
        {
            var createTableCommandText = ".create table A (a:int)\n\n";

            foreach (var entityType in new[] { EntityType.Database, EntityType.Table })
            {
                var currentText = currentPolicy != null
                    ? new AlterRetentionPolicyCommand(
                        entityType,
                        new EntityName("A"),
                        currentPolicy.Value.softDeletePeriod,
                        currentPolicy.Value.recoverability).ToScript(null)
                    : string.Empty;
                var currentCommands = Parse(createTableCommandText + currentText);
                var currentDatabase = DatabaseModel.FromCommands(currentCommands);
                var targetText = targetPolicy != null
                    ? new AlterRetentionPolicyCommand(
                        entityType,
                        new EntityName("A"),
                        targetPolicy.Value.softDeletePeriod,
                        targetPolicy.Value.recoverability).ToScript(null)
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
                    Assert.IsType<AlterRetentionPolicyCommand>(delta[0]);

                    var alterCommand = (AlterRetentionPolicyCommand)delta[0];

                    Assert.Equal(entityType, alterCommand.EntityType);
                    Assert.Equal("A", alterCommand.EntityName.Name);
                    alterAction(alterCommand);
                }
                else if (deleteAction != null)
                {
                    Assert.Single(delta);
                    Assert.IsType<DeleteRetentionPolicyCommand>(delta[0]);

                    var deleteCommand = (DeleteRetentionPolicyCommand)delta[0];

                    Assert.Equal(entityType, deleteCommand.EntityType);
                    Assert.Equal("A", deleteCommand.EntityName.Name);
                    deleteAction(deleteCommand);
                }
            }
        }
    }
}