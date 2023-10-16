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
    public class DeltaPartitioningPolicyTest : ParsingTestBase
    {
        [Fact]
        public void TableFromEmptyToSomething()
        {
            TestPartitioning(
                null,
                42,
                c =>
                {
                    Assert.Equal(42, c.MaxRowCount);
                },
                null);
        }

        [Fact]
        public void TableFromSomethingToEmpty()
        {
            TestPartitioning(
                72,
                null,
                null,
                c => { });
        }

        [Fact]
        public void TableDelta()
        {
            var targetDuration = TimeSpan.FromDays(25) + TimeSpan.FromHours(4);

            TestPartitioning(
                42,
                54,
                c =>
                {
                    Assert.Equal(54, c.MaxRowCount);
                },
                null);
        }

        [Fact]
        public void TableSame()
        {
            TestPartitioning(
                193,
                193,
                null,
                null);
        }

        private void TestPartitioning(
            int? currentMaxRowCount,
            int? targetMaxRowCount,
            Action<AlterPar>? alterAction,
            Action<DeleteShardingPolicyCommand>? deleteAction)
        {
            var createTableCommandText = ".create table A (a:int)\n\n";

            foreach (var entityType in new[] { EntityType.Database, EntityType.Table })
            {
                var currentText = currentMaxRowCount != null
                    ? new AlterShardingPolicyCommand(
                        entityType,
                        new EntityName("A"),
                        currentMaxRowCount.Value,
                        200,
                        300).ToScript(null)
                    : string.Empty;
                var currentCommands = Parse(createTableCommandText + currentText);
                var currentDatabase = DatabaseModel.FromCommands(currentCommands);
                var targetText = targetMaxRowCount != null
                    ? new AlterShardingPolicyCommand(
                        entityType,
                        new EntityName("A"),
                        targetMaxRowCount.Value,
                        200,
                        300).ToScript(null)
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
                    Assert.IsType<AlterShardingPolicyCommand>(delta[0]);

                    var alterCommand = (AlterShardingPolicyCommand)delta[0];

                    Assert.Equal(entityType, alterCommand.EntityType);
                    Assert.Equal("A", alterCommand.EntityName.Name);
                    alterAction(alterCommand);
                }
                else if (deleteAction != null)
                {
                    Assert.Single(delta);
                    Assert.IsType<DeleteShardingPolicyCommand>(delta[0]);

                    var deleteCommand = (DeleteShardingPolicyCommand)delta[0];

                    Assert.Equal(entityType, deleteCommand.EntityType);
                    Assert.Equal("A", deleteCommand.EntityName.Name);
                    deleteAction(deleteCommand);
                }
            }
        }
    }
}