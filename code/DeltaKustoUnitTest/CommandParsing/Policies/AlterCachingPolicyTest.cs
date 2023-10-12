using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterCachingPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestCachingPolicy(EntityType.Table, "A", TimeSpan.FromDays(3));
        }

        [Fact]
        public void FunkyTable()
        {
            TestCachingPolicy(EntityType.Table, "A- 1", TimeSpan.FromMinutes(90));
        }

        [Fact]
        public void DbComposedTableName()
        {
            var command = ParseOneCommand(".alter table mydb.mytable policy caching hot=3d");

            Assert.IsType<AlterCachingPolicyCommand>(command);

            var realCommand = (AlterCachingPolicyCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            var command = ParseOneCommand(
                ".alter table ['my cluster'].mydb.mytable policy caching"
                + " hot=3d, hotindex=time(10.00:00:00)");

            Assert.IsType<AlterCachingPolicyCommand>(command);

            var realCommand = (AlterCachingPolicyCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void OneHotWindow()
        {
            foreach (var entityType in new[] { EntityType.Table, EntityType.Database })
            {
                var entityTypeName = entityType == EntityType.Table
                    ? "table"
                    : "database";
                var command = ParseOneCommand(
                    $".alter {entityTypeName} myEntity policy caching"
                    + " hot=3d, hot_window = datetime(2021-01-01) .. datetime(2021-02-01)");

                Assert.IsType<AlterCachingPolicyCommand>(command);

                var realCommand = (AlterCachingPolicyCommand)command;

                Assert.Equal(entityType, realCommand.EntityType);
                Assert.Equal("myEntity", realCommand.EntityName.Name);
                Assert.Single(realCommand.HotWindows);
                Assert.Equal(new DateTime(2021, 01, 01), realCommand.HotWindows.First().From);
                Assert.Equal(new DateTime(2021, 02, 01), realCommand.HotWindows.First().To);
            }
        }

        [Fact]
        public void TwoHotWindows()
        {
            foreach (var entityType in new[] { EntityType.Table, EntityType.Database })
            {
                var entityTypeName = entityType == EntityType.Table
                    ? "table"
                    : "database";
                var command = ParseOneCommand(
                    $".alter {entityTypeName} myEntity policy caching"
                    + " hot=3d, "
                    + "hot_window = datetime(2021-01-01) .. datetime(2021-02-01)"
                    + "hot_window = datetime(2021-03-01) .. datetime(2021-04-01)");

                Assert.IsType<AlterCachingPolicyCommand>(command);

                var realCommand = (AlterCachingPolicyCommand)command;

                Assert.Equal(entityType, realCommand.EntityType);
                Assert.Equal("myEntity", realCommand.EntityName.Name);
                Assert.Equal(2, realCommand.HotWindows.Count);
                Assert.Equal(new DateTime(2021, 01, 01), realCommand.HotWindows.First().From);
                Assert.Equal(new DateTime(2021, 02, 01), realCommand.HotWindows.First().To);
                Assert.Equal(new DateTime(2021, 03, 01), realCommand.HotWindows.Last().From);
                Assert.Equal(new DateTime(2021, 04, 01), realCommand.HotWindows.Last().To);
            }
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestCachingPolicy(EntityType.Database, "Db", TimeSpan.FromSeconds(40));
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestCachingPolicy(EntityType.Database, "db.mine", TimeSpan.FromHours(90));
        }

        private void TestCachingPolicy(
            EntityType type,
            string name,
            TimeSpan hotData)
        {
            for (var i = 0; i != 2; ++i)
            {
                var hotIndex = hotData + TimeSpan.FromMinutes(i);
                var commandText = new AlterCachingPolicyCommand(
                    type,
                    new EntityName(name),
                    hotData,
                    hotIndex,
                    new HotWindow[0])
                    .ToScript(null);
                var command = ParseOneCommand(commandText);

                Assert.IsType<AlterCachingPolicyCommand>(command);
            }
        }
    }
}