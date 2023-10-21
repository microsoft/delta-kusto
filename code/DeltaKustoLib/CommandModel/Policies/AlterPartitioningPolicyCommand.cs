using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Kusto.Language.Syntax;

namespace DeltaKustoLib.CommandModel.Policies
{
    /// <summary>
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-table-partitioning-policy-command"/>
    /// </summary>
    [Command(18100, "Alter Partitioning Policy")]
    public class AlterPartitioningPolicyCommand : TableOnlyPolicyCommandBase
    {
        public override string CommandFriendlyName => ".alter <entity> policy partitioning";

        public override string ScriptPath =>
            $"tables/policies/partitioning/create/{TableName}";

        public AlterPartitioningPolicyCommand(
            EntityName tableName,
            JsonDocument policy) : base(tableName, policy)
        {
        }

        public override string ToScript(ScriptingContext? context = null)
        {
            var builder = new StringBuilder();

            builder.Append($".alter table {TableName} policy partitioning");
            builder.AppendLine();
            builder.AppendLine("```");
            builder.AppendLine(SerializePolicy());
            builder.AppendLine("```");

            return builder.ToString();
        }

        internal static CommandBase? FromCode(CommandBlock commandBlock)
        {
            var nameReferences = commandBlock.GetDescendants<NameReference>();
            var entityNameReference = nameReferences.Last();
            var policyText = QuotedText.FromLiteral(
                commandBlock.GetUniqueDescendant<LiteralExpression>(
                    "Partitioning Policy",
                    e => e.NameInParent == "Policy"));
            var policy = Deserialize<JsonDocument>(policyText.Text);

            if (policy == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policyText.ToScript()}");
            }

            return new AlterPartitioningPolicyCommand(
                EntityName.FromCode(entityNameReference.Name),
                policy);
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterPartitioningPolicyCommand? currentCommand,
            AlterPartitioningPolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {
                // No target, we remove the current policy
                yield return new DeletePartitioningPolicyCommand(
                    currentCommand!.TableName);
            }
            else if (hasTarget)
            {
                if (!hasCurrent || !currentCommand!.Equals(targetCommand!))
                {
                    // There is a target and either no current or the current is different
                    yield return targetCommand!;
                }
            }
            else
            {   // Both target and current are null: no delta
            }
        }
    }
}