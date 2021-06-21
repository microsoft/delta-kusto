using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta.Policies
{
    public class AlterCachingPolicyTest : ParsingTestBase
    {
        [Fact]
        public void TableFromEmptyToSomething()
        {
            TestCaching(
                null,
                TimeSpan.FromDays(3),
                c => Assert.Equal(TimeSpan.FromDays(3), c.Duration.Duration),
                null);
        }

        [Fact]
        public void TableFromSomethingToEmpty()
        {
            TestCaching(
                TimeSpan.FromMinutes(7),
                null,
                null,
                c => { });
        }

        [Fact]
        public void TableDelta()
        {
            var targetDuration = TimeSpan.FromDays(25) + TimeSpan.FromHours(4);

            TestCaching(
                TimeSpan.FromDays(3),
                targetDuration,
                c => Assert.Equal(targetDuration, c.Duration.Duration),
                null);
        }

        [Fact]
        public void TableSame()
        {
            TestCaching(
                TimeSpan.FromMilliseconds(45),
                TimeSpan.FromMilliseconds(45),
                null,
                null);
        }

        private void TestCaching(
            TimeSpan? currentDuration,
            TimeSpan? targetDuration,
            Action<AlterCachingPolicyCommand>? alterAction,
            Action<DeleteCachingPolicyCommand>? deleteAction)
        {
            var createTableCommandText = ".create table A (a:int)\n\n";

            foreach (var entityType in new[] { EntityType.Database, EntityType.Table })
            {
                var currentCachingText = currentDuration != null
                    ? new AlterCachingPolicyCommand(
                        entityType,
                        new EntityName("A"),
                        currentDuration.Value).ToScript()
                    : string.Empty;
                var currentCommandText = createTableCommandText + currentCachingText;
                var currentCommands = Parse(currentCommandText);
                var currentDatabase = DatabaseModel.FromCommands(currentCommands);
                var targetCachingText = targetDuration != null
                    ? new AlterCachingPolicyCommand(
                        entityType,
                        new EntityName("A"),
                        targetDuration.Value).ToScript()
                    : string.Empty;
                var targetCommandText = createTableCommandText + targetCachingText;
                var targetCommands = Parse(targetCommandText);
                var targetDatabase = DatabaseModel.FromCommands(targetCommands);
                var delta = currentDatabase.ComputeDelta(targetDatabase);

                if (alterAction == null && deleteAction == null)
                {
                    Assert.Empty(delta);
                }
                else if (alterAction != null)
                {
                    Assert.Single(delta);
                    Assert.IsType<AlterCachingPolicyCommand>(delta[0]);

                    var cachingCommand = (AlterCachingPolicyCommand)delta[0];

                    Assert.Equal(entityType, cachingCommand.EntityType);
                    Assert.Equal("A", cachingCommand.EntityName.Name);
                    alterAction(cachingCommand);
                }
                else if (deleteAction != null)
                {
                    Assert.Single(delta);
                    Assert.IsType<DeleteCachingPolicyCommand>(delta[0]);

                    var cachingCommand = (DeleteCachingPolicyCommand)delta[0];

                    Assert.Equal(entityType, cachingCommand.EntityType);
                    Assert.Equal("A", cachingCommand.EntityName.Name);
                    deleteAction(cachingCommand);
                }
            }
        }
    }
}