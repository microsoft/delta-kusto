using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.IngestionBatching;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta.Policies
{
    public class DeltaIngestionBatchingPolicyTest : ParsingTestBase
    {
        #region Inner types
        private record IngestionBatchingPolicy
        {
            public int MaximumNumberOfItems { get; init; }
        }
        #endregion

        [Fact]
        public void TableFromEmptyToSomething()
        {
            TestIngestionBatching(
                null,
                (5),
                c =>
                {
                    var policy = c.DeserializePolicy<IngestionBatchingPolicy>();

                    Assert.Equal(5, policy.MaximumNumberOfItems);
                },
                null);
        }

        [Fact]
        public void TableFromSomethingToEmpty()
        {
            TestIngestionBatching(
                12,
                null,
                null,
                c => { });
        }

        [Fact]
        public void TableDelta()
        {
            TestIngestionBatching(
                30,
                40,
                c =>
                {
                    var policy = c.DeserializePolicy<IngestionBatchingPolicy>();
                    
                    Assert.Equal(40, policy.MaximumNumberOfItems);
                },
                null);
        }

        [Fact]
        public void TableSame()
        {
            TestIngestionBatching(
                15,
                15,
                null,
                null);
        }

        private void TestIngestionBatching(
            int? currentMaxItems,
            int? targetMaxItems,
            Action<AlterIngestionBatchingPolicyCommand>? alterAction,
            Action<DeleteIngestionBatchingPolicyCommand>? deleteAction)
        {
            var createTableCommandText = ".create table A (a:int)\n\n";

            foreach (var entityType in new[] { EntityType.Database, EntityType.Table })
            {
                var currentText = currentMaxItems != null
                    ? new AlterIngestionBatchingPolicyCommand(
                        entityType,
                        new EntityName("A"),
                        TimeSpan.FromDays(1),
                        currentMaxItems.Value,
                        512).ToScript(null)
                    : string.Empty;
                var currentCommands = Parse(createTableCommandText + currentText);
                var currentDatabase = DatabaseModel.FromCommands(currentCommands);
                var targetText = targetMaxItems != null
                    ? new AlterIngestionBatchingPolicyCommand(
                        entityType,
                        new EntityName("A"),
                        TimeSpan.FromDays(1),
                        targetMaxItems.Value,
                        512).ToScript(null)
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
                    Assert.IsType<AlterIngestionBatchingPolicyCommand>(delta[0]);

                    var alterCommand = (AlterIngestionBatchingPolicyCommand)delta[0];

                    Assert.Equal(entityType, alterCommand.EntityType);
                    Assert.Equal("A", alterCommand.EntityName.Name);
                    alterAction(alterCommand);
                }
                else if (deleteAction != null)
                {
                    Assert.Single(delta);
                    Assert.IsType<DeleteIngestionBatchingPolicyCommand>(delta[0]);

                    var deleteCommand = (DeleteIngestionBatchingPolicyCommand)delta[0];

                    Assert.Equal(entityType, deleteCommand.EntityType);
                    Assert.Equal("A", deleteCommand.EntityName.Name);
                    deleteAction(deleteCommand);
                }
            }
        }
    }
}