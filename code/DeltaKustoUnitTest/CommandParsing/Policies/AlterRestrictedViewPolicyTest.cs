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
            TestAutoDeletePolicy("MyTable");
        }

        [Fact]
        public void FunkyTable()
        {
            TestAutoDeletePolicy("['A- 1']");
        }

        [Fact]
        public void DbComposedTableName()
        {
            TestAutoDeletePolicy("mydb.mytable");
        }

        [Fact]
        public void ClusterComposedTableName()
        {
            TestAutoDeletePolicy("mycluster.['my db'].mytable");
        }

        private void TestAutoDeletePolicy(string tableName)
        {
            TestAutoDeletePolicy(tableName, true);
            TestAutoDeletePolicy(tableName, false);
        }

        private void TestAutoDeletePolicy(string tableName, bool isEnabled)
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

        private static string GetActualTableName(string tableName)
        {
            var actualTableName = tableName.Split('.').Last();

            if (actualTableName.StartsWith('['))
            {
                return actualTableName.Substring(2, actualTableName.Length - 4);
            }
            else
            {
                return actualTableName;
            }
        }
    }
}