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
    public class DeltaMergePolicyTest : ParsingTestBase
    {
        #region Inner types
        private record MergePolicy
        {
            public int MaxExtentsToMerge { get; init; }
        }
        #endregion

        [Fact]
        public void TableFromEmptyToSomething()
        {
            TestMerge(
                null,
                42,
                c =>
                {
                    var policy = c.DeserializePolicy<MergePolicy>();

                    Assert.Equal(42, policy.MaxExtentsToMerge);
                },
                null);
        }

        [Fact]
        public void TableFromSomethingToEmpty()
        {
            TestMerge(
                54,
                null,
                null,
                c => { });
        }

        [Fact]
        public void TableDelta()
        {
            var targetDuration = TimeSpan.FromDays(25) + TimeSpan.FromHours(4);

            TestMerge(
                56,
                72,
                c =>
                {
                    var policy = c.DeserializePolicy<MergePolicy>();
                    
                    Assert.Equal(72, policy.MaxExtentsToMerge);
                },
                null);
        }

        [Fact]
        public void TableSame()
        {
            TestMerge(
                84,
                84,
                null,
                null);
        }

        private void TestMerge(
            int? currentMaxExtentsToMerge,
            int? targetMaxExtentsToMerge,
            Action<AlterMergePolicyCommand>? alterAction,
            Action<DeleteMergePolicyCommand>? deleteAction)
        {
            var createTableCommandText = ".create table A (a:int)\n\n";

            foreach (var entityType in new[] { EntityType.Database, EntityType.Table })
            {
                var currentText = currentMaxExtentsToMerge != null
                    ? new AlterMergePolicyCommand(
                        entityType,
                        new EntityName("A"),
                        12,
                        currentMaxExtentsToMerge.Value,
                        TimeSpan.FromHours(1)).ToScript(null)
                    : string.Empty;
                var currentCommands = Parse(createTableCommandText + currentText);
                var currentDatabase = DatabaseModel.FromCommands(currentCommands);
                var targetText = targetMaxExtentsToMerge != null
                    ? new AlterMergePolicyCommand(
                        entityType,
                        new EntityName("A"),
                        12,
                        targetMaxExtentsToMerge.Value,
                        TimeSpan.FromHours(1)).ToScript(null)
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
                    Assert.IsType<AlterMergePolicyCommand>(delta[0]);

                    var alterCommand = (AlterMergePolicyCommand)delta[0];

                    Assert.Equal(entityType, alterCommand.EntityType);
                    Assert.Equal("A", alterCommand.EntityName.Name);
                    alterAction(alterCommand);
                }
                else if (deleteAction != null)
                {
                    Assert.Single(delta);
                    Assert.IsType<DeleteMergePolicyCommand>(delta[0]);

                    var deleteCommand = (DeleteMergePolicyCommand)delta[0];

                    Assert.Equal(entityType, deleteCommand.EntityType);
                    Assert.Equal("A", deleteCommand.EntityName.Name);
                    deleteAction(deleteCommand);
                }
            }
        }
    }
}