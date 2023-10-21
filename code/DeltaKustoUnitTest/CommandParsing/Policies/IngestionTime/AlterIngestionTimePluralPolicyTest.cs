using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.IngestionTime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.IngestionTime
{
    public class AlterIngestionTimePluralPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTables()
        {
            TestIngestionTimePolicy("MyTable", "YourTable");
        }

        [Fact]
        public void FunkyTables()
        {
            TestIngestionTimePolicy("MyTable", "['A- 1']", "['Beta-- 1']");
        }

        private void TestIngestionTimePolicy(params string[] tableNames)
        {
            TestIngestionTimePolicy(tableNames, true);
            TestIngestionTimePolicy(tableNames, false);
        }

        private void TestIngestionTimePolicy(IEnumerable<string> tableNames, bool areEnabled)
        {
            var tableListText = string.Join(", ", tableNames);
            var commandText = @$"
.alter tables ({tableListText}) policy ingestiontime {areEnabled.ToString().ToLower()}";
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterIngestionTimePluralPolicyCommand>(command);

            var realCommand = (AlterIngestionTimePluralPolicyCommand)command;

            Assert.Equal(areEnabled, realCommand.AreEnabled);
            Assert.Equal(tableNames.Count(), realCommand.TableNames.Count);

            var expectedNames = ImmutableHashSet.Create(tableNames
                .Select(t => GetActualTableName(t))
                .ToArray());
            var observedNames = ImmutableHashSet.Create(realCommand.TableNames
                .Select(t => t.Name)
                .ToArray());

            foreach (var name in expectedNames)
            {
                Assert.Contains(name, observedNames);
            }
        }
    }
}