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
            TestCachingPolicy(EntityType.Table, "A", "3d");
        }

        [Fact]
        public void FunkyTable()
        {
            TestCachingPolicy(EntityType.Table, "A- 1", "90m");
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestCachingPolicy(EntityType.Database, "Db", "40s");
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestCachingPolicy(EntityType.Database, "db.mine", "90h");
        }

        private void TestCachingPolicy(EntityType type, string name, string duration)
        {
            var commandText = new AlterCachingPolicyCommand(
                type,
                new EntityName(name),
                TimeSpan.FromSeconds(3),
                duration)
                .ToScript();
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterCachingPolicyCommand>(command);

            var alterCachingPolicyCommand = (AlterCachingPolicyCommand)command;

            Assert.Equal(type, alterCachingPolicyCommand.EntityType);
            Assert.Equal(name, alterCachingPolicyCommand.EntityName.Name);
            Assert.Equal(duration, alterCachingPolicyCommand.DurationText);
        }
    }
}