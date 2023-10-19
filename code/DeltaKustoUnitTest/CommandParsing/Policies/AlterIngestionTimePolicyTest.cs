using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterIngestionTimePolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestIngestionTimePolicy("A");
        }

        [Fact]
        public void FunkyTable()
        {
            TestIngestionTimePolicy("['A- 1']");
        }

        //  Currently unsupported in parser
        //[Fact]
        //public void DbComposedTableName()
        //{
        //    TestIngestionTimePolicy("mydb.mytable");
        //}

        //[Fact]
        //public void ClusterComposedTableName()
        //{
        //    TestIngestionTimePolicy("mycluster.['my db'].mytable");
        //}

        private void TestIngestionTimePolicy(string tableName)
        {
            TestIngestionTimePolicy(tableName, true);
            TestIngestionTimePolicy(tableName, false);
        }

        private void TestIngestionTimePolicy(string tableName, bool isEnabled)
        {
            var actualTableName = GetActualTableName(tableName);
            var commandText = $@"
.alter table {tableName} policy ingestiontime {isEnabled.ToString().ToLower()}";
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterIngestionTimePolicyCommand>(command);

            var realCommand = (AlterIngestionTimePolicyCommand)command;

            Assert.Equal(actualTableName, realCommand.TableName.Name);
            Assert.Equal(isEnabled, realCommand.IsEnabled);
        }
    }
}