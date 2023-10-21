using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.KustoModel;
using Kusto.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.Delta.Policies
{
    public class DeltaPartitioningPolicyTest : ParsingTestBase
    {
        #region Policy objects
        private static readonly object _policy1 = new
        {
            PartitionKeys = new[]
            {
                new
                {
                    ColumnName="MyColumn",
                    Kind="Hash",
                    Properties = new
                    {
                        Function="XxHash64",
                        MaxPartitionCount=128,
                        PartitionAssignmentMode="Uniform"
                    }
                }
            }
        };
        private static readonly object _policy2 = new
        {
            PartitionKeys = new[]
            {
                new
                {
                    ColumnName="MyOtherColumn",
                    Kind="Hash",
                    Properties = new
                    {
                        Function="XxHash64",
                        MaxPartitionCount=64,
                        PartitionAssignmentMode="Uniform"
                    }
                }
            }
        };
        #endregion

        [Fact]
        public void TableFromEmptyToSomething()
        {
            TestPartitioning(
                null,
                _policy1,
                true,
                false);
        }

        [Fact]
        public void TableFromSomethingToEmpty()
        {
            TestPartitioning(
                _policy1,
                null,
                false,
                true);
        }

        [Fact]
        public void TableDelta()
        {
            var targetDuration = TimeSpan.FromDays(25) + TimeSpan.FromHours(4);

            TestPartitioning(
                _policy1,
                _policy2,
                true,
                false);
        }

        [Fact]
        public void TableSame()
        {
            TestPartitioning(
                _policy2,
                _policy2,
                false,
                false);
        }

        private void TestPartitioning(
            object? currentPolicy,
            object? targetPolicy,
            bool hasAlter,
            bool hasDelete)
        {
            var createTableCommandText = ".create table A (a:int)\n\n";
            var currentText = currentPolicy != null
                ? new AlterPartitioningPolicyCommand(
                    new EntityName("A"),
                    JsonSerializer.Deserialize<JsonDocument>(
                        JsonSerializer.Serialize(currentPolicy))!).ToScript(null)
                : string.Empty;
            var currentCommands = Parse(createTableCommandText + currentText);
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetText = targetPolicy != null
                ? new AlterPartitioningPolicyCommand(
                    new EntityName("A"),
                    JsonSerializer.Deserialize<JsonDocument>(
                        JsonSerializer.Serialize(targetPolicy))!).ToScript(null)
                : string.Empty;
            var targetCommands = Parse(createTableCommandText + targetText);
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            if (!hasAlter && !hasDelete)
            {
                Assert.Empty(delta);
            }
            else if (hasAlter)
            {
                Assert.Single(delta);
                Assert.IsType<AlterPartitioningPolicyCommand>(delta[0]);

                var alterCommand = (AlterPartitioningPolicyCommand)delta[0];

                Assert.Equal("A", alterCommand.TableName.Name);
            }
            else if (hasDelete)
            {
                Assert.Single(delta);
                Assert.IsType<DeletePartitioningPolicyCommand>(delta[0]);

                var deleteCommand = (DeletePartitioningPolicyCommand)delta[0];

                Assert.Equal("A", deleteCommand.TableName.Name);
            }
        }
    }
}