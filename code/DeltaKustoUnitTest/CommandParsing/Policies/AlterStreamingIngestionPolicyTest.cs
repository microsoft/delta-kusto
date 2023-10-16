using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterStreamingIngestionPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestStreamingIngestionPolicy(EntityType.Table, "A", true, 2.1);
        }

        [Fact]
        public void FunkyTable()
        {
            TestStreamingIngestionPolicy(EntityType.Table, "A -1", true, 2.1);
        }

        [Fact]
        public void DbComposedTableName()
        {
            var command = ParseOneCommand(@$"
.alter table mydb.mytable policy streamingingestion
```
{{
    ""IsEnabled"":true
}}
```");

            Assert.IsType<AlterStreamingIngestionPolicyCommand>(command);

            var realCommand = (AlterStreamingIngestionPolicyCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            var command = ParseOneCommand(@$"
.alter table mycluster.['my db'].mytable policy streamingingestion
```
{{
    ""IsEnabled"":true,
    ""HintAllocatedRate"":4.5
}}
```");

            Assert.IsType<AlterStreamingIngestionPolicyCommand>(command);

            var realCommand = (AlterStreamingIngestionPolicyCommand)command;

            Assert.Equal(EntityType.Table, realCommand.EntityType);
            Assert.Equal("mytable", realCommand.EntityName.Name);
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestStreamingIngestionPolicy(EntityType.Database, "Db", true, 2.1);
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestStreamingIngestionPolicy(EntityType.Database, "db.mine", true, null);
        }

        private void TestStreamingIngestionPolicy(
            EntityType type,
            string name,
            bool isEnabled,
            double? hintAllocatedRate)
        {
            var commandText = new AlterStreamingIngestionPolicyCommand(
                type,
                new EntityName(name),
                isEnabled,
                hintAllocatedRate)
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterStreamingIngestionPolicyCommand>(command);
        }
    }
}