using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
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
                    .ToScript();
                var command = ParseOneCommand(commandText);

                Assert.IsType<AlterCachingPolicyCommand>(command);

                var alterCachingPolicyCommand = (AlterCachingPolicyCommand)command;

                Assert.Equal(type, alterCachingPolicyCommand.EntityType);
                Assert.Equal(name, alterCachingPolicyCommand.EntityName.Name);
                Assert.Equal(hotData, alterCachingPolicyCommand.HotData.Duration);
                Assert.Equal(hotIndex, alterCachingPolicyCommand.HotIndex.Duration);
            }
        }
    }
}