using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class DeleteCachingPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestDeleteCachingPolicy(EntityType.Table, "A", "3d");
        }

        [Fact]
        public void FunkyTable()
        {
            TestDeleteCachingPolicy(EntityType.Table, "A- 1", "90m");
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestDeleteCachingPolicy(EntityType.Database, "Db", "40s");
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestDeleteCachingPolicy(EntityType.Database, "db.mine", "90h");
        }

        private void TestDeleteCachingPolicy(EntityType type, string name, string duration)
        {
            var commandText = new DeleteCachingPolicyCommand(
                type,
                new EntityName(name))
                .ToScript();
            var command = ParseOneCommand(commandText);

            Assert.IsType<DeleteCachingPolicyCommand>(command);

            var cachingPolicyCommand = (DeleteCachingPolicyCommand)command;

            Assert.Equal(type, cachingPolicyCommand.EntityType);
            Assert.Equal(name, cachingPolicyCommand.EntityName.Name);
        }
    }
}