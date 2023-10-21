using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterRestrictedViewPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestRestrictedViewPolicy("MyTable");
        }

        [Fact]
        public void FunkyTable()
        {
            TestRestrictedViewPolicy("['A- 1']");
        }

        [Fact]
        public void DbComposedTableName()
        {
            TestRestrictedViewPolicy("mydb.mytable");
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            TestRestrictedViewPolicy("mycluster.['my db'].mytable");
        }

        private void TestRestrictedViewPolicy(string tableName)
        {
            TestRestrictedViewPolicy(tableName, true);
            TestRestrictedViewPolicy(tableName, false);
        }

        private void TestRestrictedViewPolicy(string tableName, bool isEnabled)
        {
            var commandText = @$"
.alter table {tableName} policy restricted_view_access {isEnabled.ToString().ToLower()}";
            var command = ParseOneCommand(commandText);
            var actualTableName = GetActualTableName(tableName);

            Assert.IsType<AlterRestrictedViewPolicyCommand>(command);
            Assert.Equal(isEnabled, ((AlterRestrictedViewPolicyCommand)command).IsEnabled);
            Assert.Equal(
                actualTableName,
                ((AlterRestrictedViewPolicyCommand)command).TableName.Name);
        }
    }
}