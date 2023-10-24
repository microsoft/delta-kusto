using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies.IngestionBatching;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.IngestionBatching
{
    public class AlterIngestionBatchingPluralPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTables()
        {
            TestIngestionBatchingPolicy(TimeSpan.FromDays(3), 2000, 200, "A", "B", "C");
        }

        [Fact]
        public void FunkyTables()
        {
            TestIngestionBatchingPolicy(
                TimeSpan.FromDays(30),
                1500,
                150,
                "A- 1",
                "B- 1",
                "CA-  45");
        }

        private void TestIngestionBatchingPolicy(
            TimeSpan maximumBatchingTimeSpan,
            int maximumNumberOfItems,
            int maximumRawDataSizeMb,
            params string[] tableNames)
        {
            var commandText = new AlterIngestionBatchingPluralPolicyCommand(
                tableNames.Select(t=> new EntityName(t)),
                maximumBatchingTimeSpan,
                maximumNumberOfItems,
                maximumRawDataSizeMb)
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterIngestionBatchingPluralPolicyCommand>(command);

            var realCommand = (AlterIngestionBatchingPluralPolicyCommand)command;
            var commandTableSet = ImmutableHashSet.CreateRange(
                realCommand.TableNames.Select(t => t.Name));
            var tableSet = ImmutableHashSet.CreateRange(tableNames);

            Assert.Equal(commandTableSet.Count, tableSet.Count);
            Assert.Empty(commandTableSet.Except(tableSet));
        }
    }
}