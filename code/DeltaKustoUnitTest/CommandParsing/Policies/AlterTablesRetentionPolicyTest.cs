using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterTablesRetentionPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestRetentionPolicy(TimeSpan.FromDays(3), true, "A");
        }

        [Fact]
        public void SimpleTables()
        {
            TestRetentionPolicy(TimeSpan.FromDays(3), true, "A", "Bottle", "Cigar");
        }

        [Fact]
        public void FunkyTable()
        {
            TestRetentionPolicy(TimeSpan.FromMinutes(90), false, "A- 1");
        }

        //[Fact]
        //public void FunkyTables()
        //{
        //    TestRetentionPolicy(TimeSpan.FromMinutes(90), false, "A- 1", "Beta \u00E9t\u00E9");
        //}

        private void TestRetentionPolicy(
            TimeSpan softDeletePeriod,
            bool recoverability,
            params string[] names)
        {
            var commandText = new AlterTablesRetentionPolicyCommand(
                names.Select(n => new EntityName(n)),
                softDeletePeriod,
                recoverability)
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterTablesRetentionPolicyCommand>(command);
        }
    }
}