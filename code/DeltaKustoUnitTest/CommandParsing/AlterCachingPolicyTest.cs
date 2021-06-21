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

        private void TestCachingPolicy(EntityType type, string name, TimeSpan duration)
        {
            var commandText = new AlterCachingPolicyCommand(
                type,
                new EntityName(name),
                duration)
                .ToScript();
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterCachingPolicyCommand>(command);

            var alterCachingPolicyCommand = (AlterCachingPolicyCommand)command;

            Assert.Equal(type, alterCachingPolicyCommand.EntityType);
            Assert.Equal(name, alterCachingPolicyCommand.EntityName.Name);
            Assert.Equal(duration, alterCachingPolicyCommand.Duration.Duration);
        }
    }
}