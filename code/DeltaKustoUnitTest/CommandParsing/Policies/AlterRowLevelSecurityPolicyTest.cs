using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterRowLevelSecurityPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestRowLevelPolicy("A", "MyFunction");
        }

        [Fact]
        public void FunkyTable()
        {
            TestRowLevelPolicy("['A- 1']", "MyTable | where TenantId == 42");
        }

        //  Currently not supported by the parser
        //[Fact]
        //public void DbComposedTableName()
        //{
        //    TestRowLevelPolicy("mydb.mytable", "MyFunction");
        //}

        //  Currently not supported by the parser
        //[Fact]
        //public void ClusterComposedTableName()
        //{
        //    TestRowLevelPolicy("mycluster.['my db'].mytable", "MyFunction");
        //}

        private void TestRowLevelPolicy(string tableName, string query)
        {
            TestRowLevelPolicy(tableName, true, query);
            TestRowLevelPolicy(tableName, false, query);
        }

        private void TestRowLevelPolicy(string tableName, bool isEnabled, string query)
        {
            var realTableName = tableName.Split('.').Last();
            var enableToken = isEnabled ? "enable" : "disable";
            var commandText = $@"
.alter table {tableName} policy row_level_security {enableToken} ""{query}""
";
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterRowLevelSecurityPolicyCommand>(command);

            var realCommand = (AlterRowLevelSecurityPolicyCommand)command;

            Assert.Equal(isEnabled, realCommand.IsEnabled);
            Assert.Equal(query, realCommand.Query.Text);
        }
    }
}