using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies
{
    public class AlterRestrictedViewPluralPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTables()
        {
            TestRestrictedViewPolicy("MyTable", "YourTable");
        }

        [Fact]
        public void FunkyTables()
        {
            TestRestrictedViewPolicy("MyTable", "['A- 1']", "['Beta-- 1']");
        }

        private void TestRestrictedViewPolicy(params string[] tableNames)
        {
            TestRestrictedViewPolicy(tableNames, true);
            TestRestrictedViewPolicy(tableNames, false);
        }

        private void TestRestrictedViewPolicy(IEnumerable<string> tableNames, bool areEnabled)
        {
            var tableListText = string.Join(", ", tableNames);
            var commandText = @$"
.alter tables ({tableListText}) policy restricted_view_access {areEnabled.ToString().ToLower()}";
            var command = ParseOneCommand(commandText);

            Assert.IsType<AlterRestrictedViewPluralPolicyCommand>(command);

            var realCommand = (AlterRestrictedViewPluralPolicyCommand)command;

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