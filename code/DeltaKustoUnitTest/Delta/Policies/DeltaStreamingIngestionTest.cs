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
    public class DeltaStreamingIngestionPolicyTest : ParsingTestBase
    {
        #region Inner types
        private record StreamingIngestionPolicy
        {
            public bool IsEnabled { get; init; }
            public double? HintAllocatedRated { get; init; }
        }
        #endregion

        [Fact]
        public void FromEmptyToSomething()
        {
            TestStreamingIngestion(
                null,
                new StreamingIngestionPolicy{ IsEnabled = true, HintAllocatedRated = 2.1 },
                c =>
                {
                    var policy = c.DeserializePolicy<StreamingIngestionPolicy>();
                    Assert.True(policy.IsEnabled);
                    Assert.Equal(2.1, policy.HintAllocatedRated);
                },
                null);
        }

        [Fact]
        public void FromSomethingToEmpty()
        {
            TestStreamingIngestion(
                new StreamingIngestionPolicy{ IsEnabled = true, HintAllocatedRated = 2.1 },
                null,
                null,
                c => { });
        }

        [Fact]
        public void PolicyDelta()
        {
            TestStreamingIngestion(
                new StreamingIngestionPolicy{ IsEnabled = true, HintAllocatedRated = 2.1 },
                new StreamingIngestionPolicy{ IsEnabled = false, HintAllocatedRated = null },
                c =>
                {
                    var policy = c.DeserializePolicy<StreamingIngestionPolicy>();
                    Assert.False(policy.IsEnabled);
                    Assert.Null(policy.HintAllocatedRated);
                },
                null);
        }

        [Fact]
        public void PolicySame()
        {
            TestStreamingIngestion(
                new StreamingIngestionPolicy{ IsEnabled = true, HintAllocatedRated = 2.1 },
                new StreamingIngestionPolicy{ IsEnabled = true, HintAllocatedRated = 2.1 },
                null,
                null);
        }

        private void TestStreamingIngestion(
            StreamingIngestionPolicy? currentPolicy,
            StreamingIngestionPolicy? targetPolicy,
            Action<AlterStreamingIngestionPolicyCommand>? alterAction,
            Action<DeleteStreamingIngestionPolicyCommand>? deleteAction)
        {
            // We need to create table before we can test the delta policy as policy cannot be applied
            // wihout a table when entityType is table.
            var createTableCommandText = ".create table A (a:int)\n\n";

            foreach (var entityType in new[] { EntityType.Database, EntityType.Table })
            {
                // Setup Current
                var currentText = currentPolicy != null
                    ? new AlterStreamingIngestionPolicyCommand(
                        entityType,
                        new EntityName("A"),
                        currentPolicy.IsEnabled,
                        currentPolicy.HintAllocatedRated).ToScript(null)
                    : string.Empty;

                // Prepend create table command if entity type is Table.
                currentText = (entityType is EntityType.Table) ? createTableCommandText + currentText : currentText;
                var currentCommands = Parse(currentText);
                var currentDatabase = DatabaseModel.FromCommands(currentCommands);

                // Setup Target
                var targetText = targetPolicy != null
                    ? new AlterStreamingIngestionPolicyCommand(
                        entityType,
                        new EntityName("A"),
                        targetPolicy.IsEnabled,
                        targetPolicy.HintAllocatedRated).ToScript(null)
                    : string.Empty;

                // Prepend create table command if entity type is Table.
                targetText = (entityType is EntityType.Table) ? createTableCommandText + targetText : targetText;
                var targetCommands = Parse(targetText);
                var targetDatabase = DatabaseModel.FromCommands(targetCommands);

                // Compute Delta
                var delta = currentDatabase.ComputeDelta(targetDatabase);

                // Assert Action
                if (alterAction == null && deleteAction == null)
                {
                    Assert.Empty(delta);
                }
                else if (alterAction != null)
                {
                    Assert.Single(delta);
                    Assert.IsType<AlterStreamingIngestionPolicyCommand>(delta[0]);

                    var alterCommand = (AlterStreamingIngestionPolicyCommand)delta[0];

                    Assert.Equal(entityType, alterCommand.EntityType);
                    Assert.Equal("A", alterCommand.EntityName.Name);
                    alterAction(alterCommand);
                }
                else if (deleteAction != null)
                {
                    Assert.Single(delta);
                    Assert.IsType<DeleteStreamingIngestionPolicyCommand>(delta[0]);

                    var deleteCommand = (DeleteStreamingIngestionPolicyCommand)delta[0];

                    Assert.Equal(entityType, deleteCommand.EntityType);
                    Assert.Equal("A", deleteCommand.EntityName.Name);
                    deleteAction(deleteCommand);
                }
            }
        }
    }
}