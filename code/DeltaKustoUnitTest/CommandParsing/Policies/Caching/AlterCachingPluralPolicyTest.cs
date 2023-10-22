using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies.Caching;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.Caching
{
    public class AlterCachingPluralPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTables()
        {
            TestCachingPolicy(TimeSpan.FromDays(3), "A", "B", "C", "D");
        }

        [Fact]
        public void FunkyTables()
        {
            TestCachingPolicy(TimeSpan.FromMinutes(90), "A- 1", "B- 24");
        }

        [Fact]
        public void OneHotWindow()
        {
            var command = ParseOneCommand($@"
.alter tables (table1, table2) policy caching
hot=3d, hot_window = datetime(2021-01-01) .. datetime(2021-02-01)");

            Assert.IsType<AlterCachingPluralPolicyCommand>(command);

            var realCommand = (AlterCachingPluralPolicyCommand)command;

            Assert.Equal(2, realCommand.TableNames.Count);
            Assert.Contains("table1", realCommand.TableNames.Select(e => e.Name));
            Assert.Contains("table2", realCommand.TableNames.Select(e => e.Name));
            Assert.Single(realCommand.HotWindows);
            Assert.Equal(new DateTime(2021, 01, 01), realCommand.HotWindows.First().From);
            Assert.Equal(new DateTime(2021, 02, 01), realCommand.HotWindows.First().To);
        }

        [Fact]
        public void TwoHotWindows()
        {
            var command = ParseOneCommand(
                $".alter tables (['A- 1'], B) policy caching"
                + " hot=3d, "
                + "hot_window = datetime(2021-01-01) .. datetime(2021-02-01)"
                + "hot_window = datetime(2021-03-01) .. datetime(2021-04-01)");

            Assert.IsType<AlterCachingPluralPolicyCommand>(command);

            var realCommand = (AlterCachingPluralPolicyCommand)command;

            Assert.Equal(2, realCommand.TableNames.Count);
            Assert.Contains("A- 1", realCommand.TableNames.Select(e => e.Name));
            Assert.Contains("B", realCommand.TableNames.Select(e => e.Name));
            Assert.Equal(2, realCommand.HotWindows.Count);
            Assert.Equal(new DateTime(2021, 01, 01), realCommand.HotWindows.First().From);
            Assert.Equal(new DateTime(2021, 02, 01), realCommand.HotWindows.First().To);
            Assert.Equal(new DateTime(2021, 03, 01), realCommand.HotWindows.Last().From);
            Assert.Equal(new DateTime(2021, 04, 01), realCommand.HotWindows.Last().To);
        }

        private void TestCachingPolicy(TimeSpan hotData, params string[] tableNames)
        {
            for (var i = 0; i != 2; ++i)
            {
                var hotIndex = hotData + TimeSpan.FromMinutes(i);
                var commandText = new AlterCachingPluralPolicyCommand(
                    tableNames.Select(t => new EntityName(t)),
                    hotData,
                    hotIndex,
                    new HotWindow[0])
                    .ToScript(null);
                var command = ParseOneCommand(commandText);

                Assert.IsType<AlterCachingPluralPolicyCommand>(command);

                var realCommand = (AlterCachingPluralPolicyCommand)command;
                var commandTableSet = ImmutableHashSet.CreateRange(
                    realCommand.TableNames.Select(t => t.Name));
                var tableSet = ImmutableHashSet.CreateRange(tableNames);

                Assert.Equal(commandTableSet.Count, tableSet.Count);
                Assert.Empty(commandTableSet.Except(tableSet));
                Assert.Equal(hotData, realCommand.HotData.Duration);
                Assert.Equal(hotIndex, realCommand.HotIndex.Duration);
                Assert.Empty(realCommand.HotWindows);
            }
        }
    }
}