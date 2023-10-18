using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Collections;
using System.Collections.Generic;
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

            var zipped = tableNames
                .Select(t => GetActualTableName(t))
                .Zip(realCommand.TableNames.Select(n => n.Name));

            foreach (var pair in zipped)
            {
                Assert.Equal(pair.First, pair.Second);
            }
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