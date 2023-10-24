using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.Merge;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.Merge
{
    public class DeleteMergePolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestMergePolicy(EntityType.Table, "A");
        }

        [Fact]
        public void FunkyTable()
        {
            TestMergePolicy(EntityType.Table, "A- 1");
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestMergePolicy(EntityType.Database, "Db");
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestMergePolicy(EntityType.Database, "db.mine");
        }

        private void TestMergePolicy(EntityType type, string name)
        {
            var commandText = new DeleteMergePolicyCommand(type, new EntityName(name))
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<DeleteMergePolicyCommand>(command);
        }
    }
}