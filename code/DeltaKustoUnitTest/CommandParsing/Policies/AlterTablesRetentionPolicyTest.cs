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

        [Fact]
        public void FunkyTables()
        {
            TestRetentionPolicy(TimeSpan.FromMinutes(90), false, "A- 1", "Beta été");
        }

        private void TestRetentionPolicy(
            TimeSpan softDelete,
            bool recoverability,
            params string[] names)
        {
            var commandText = new AlterTablesRetentionPolicyCommand(
                names.Select(n => new EntityName(n)),
                softDelete,
                recoverability)
                .ToScript();
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterTablesRetentionPolicyCommand>(command);

            var realCommand = (AlterTablesRetentionPolicyCommand)command;

            Assert.Equal(names, realCommand.TableNames.Select(t => t.Name));
            Assert.Equal(softDelete, realCommand.SoftDelete);
            Assert.Equal(recoverability, realCommand.Recoverability);
        }
    }
}