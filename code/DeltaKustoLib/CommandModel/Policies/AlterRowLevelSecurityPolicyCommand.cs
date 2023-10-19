using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies
{
    /// <summary>
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-table-row-level-security-policy-command"/>
    /// </summary>
    [Command(20100, "Alter Row Level Policies")]
    public class AlterRowLevelSecurityPolicyCommand : TableOnlyPolicyCommandBase
    {
        public bool IsEnabled { get; }

        public string Query { get; }

        public override string CommandFriendlyName => ".alter <entity> policy row_level_security";

        public override string ScriptPath => $"tables/policies/row_level_security/create/{TableName}";

        public AlterRowLevelSecurityPolicyCommand(
            EntityName tableName,
            bool isEnabled,
            string query)
            : base(tableName)
        {
            IsEnabled = isEnabled;
            Query = query;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableName = rootElement.GetDescendants<NameReference>().Last();
            var policyText = QuotedText.FromLiteral(
                rootElement.GetUniqueDescendant<LiteralExpression>(
                    "AutoDeletePolicy",
                    e => e.NameInParent == "AutoDeletePolicy"));
            var policy = Deserialize<JsonDocument>(policyText.Text);

            if (policy == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policyText.ToScript()}");
            }

            return new AlterAutoDeletePolicyCommand(EntityName.FromCode(tableName.Name), policy);
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".alter table ");
            builder.Append(TableName);
            builder.Append(" policy auto_delete");
            builder.AppendLine();
            builder.Append("```");
            builder.Append(SerializePolicy());
            builder.AppendLine();
            builder.Append("```");

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterAutoDeletePolicyCommand? currentCommand,
            AlterAutoDeletePolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy
                yield return new DeleteAutoDeletePolicyCommand(currentCommand!.TableName);
            }
            else if (hasTarget)
            {
                if (!hasCurrent || !currentCommand!.Equals(targetCommand!))
                {   //  There is a target and either no current or the current is different
                    yield return targetCommand!;
                }
            }
            else
            {   //  Both target and current are null:  no delta
            }
        }
    }
}