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
                    hotIndex)
                    .ToScript(null);
                var command = ParseOneCommand(commandText);

                Assert.IsType<AlterCachingPolicyCommand>(command);
            }
        }
    }
}