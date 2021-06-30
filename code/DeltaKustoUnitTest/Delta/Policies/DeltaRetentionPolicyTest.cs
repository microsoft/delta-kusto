using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
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
        [Fact]
        public void TableFromEmptyToSomething()
        {
            TestRetention(
                null,
                new RetentionPolicy { SoftDeletePeriod = TimeSpan.FromDays(3).ToString() },
                c => Assert.Equal(TimeSpan.FromDays(3), c.SoftDeletePeriod),
                null);
        }

        [Fact]
        public void TableFromSomethingToEmpty()
        {
            TestRetention(
                new RetentionPolicy
                {
                    SoftDeletePeriod = TimeSpan.FromMinutes(7).ToString(),
                    Recoverability = EnableBoolean.Disabled.ToString()
                },
                null,
                null,
                c => { });
        }

        [Fact]
        public void TableDelta()
        {
            var targetDuration = TimeSpan.FromDays(25) + TimeSpan.FromHours(4);

            TestRetention(
                new RetentionPolicy { SoftDeletePeriod = TimeSpan.FromDays(3).ToString() },
                new RetentionPolicy { SoftDeletePeriod = targetDuration.ToString() },
                c => Assert.Equal(targetDuration, c.SoftDeletePeriod),
                null);
        }

        [Fact]
        public void TableSame()
        {
            TestRetention(
                new RetentionPolicy { SoftDeletePeriod = TimeSpan.FromMilliseconds(45).ToString() },
                new RetentionPolicy { SoftDeletePeriod = TimeSpan.FromMilliseconds(45).ToString() },
                null,
                null);
        }

        private void TestRetention(
            RetentionPolicy? currentPolicy,
            RetentionPolicy? targetPolicy,
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
                        currentPolicy.GetSoftDeletePeriod(),
                        currentPolicy.GetRecoverability()).ToScript(null)
                    : string.Empty;
                var currentCommands = Parse(createTableCommandText + currentText);
                var currentDatabase = DatabaseModel.FromCommands(currentCommands);
                var targetText = targetPolicy != null
                    ? new AlterRetentionPolicyCommand(
                        entityType,
                        new EntityName("A"),
                        targetPolicy.GetSoftDeletePeriod(),
                        targetPolicy.GetRecoverability()).ToScript(null)
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