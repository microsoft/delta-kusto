using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies.RowLevelSecurity
{
    /// <summary>
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-table-row-level-security-policy-command"/>
    /// </summary>
    [Command(20100, "Alter Row Level Policies")]
    public class AlterRowLevelSecurityPolicyCommand : TableOnlyPolicyCommandBase
    {
        public bool IsEnabled { get; }

        public QuotedText Query { get; }

        public override string CommandFriendlyName => ".alter <entity> policy row_level_security";

        public override string ScriptPath => $"tables/policies/row_level_security/create/{TableName}";

        public AlterRowLevelSecurityPolicyCommand(
            EntityName tableName,
            bool isEnabled,
            QuotedText query)
            : base(tableName)
        {
            IsEnabled = isEnabled;
            Query = query;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableName = rootElement.GetDescendants<NameReference>().Last();
            var isEnabled = rootElement
                .GetDescendants<SyntaxElement>(e => e.Kind == SyntaxKind.IdentifierToken)
                .Select(e => e.ToString().Trim())
                .Where(t => t == "enable" || t == "disable")
                .Select(t => new bool?(t == "enable"))
                .LastOrDefault();

            if (isEnabled == null)
            {
                throw new DeltaException(
                    "No 'enable' or 'disable' token found in row level security command");
            }

            var query = QuotedText.FromLiteral(
                rootElement.GetUniqueDescendant<LiteralExpression>(
                    "Row Level Security",
                    e => e.NameInParent == "Query"));

            return new AlterRowLevelSecurityPolicyCommand(
                EntityName.FromCode(tableName.Name),
                isEnabled.Value,
                query);
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".alter table ");
            builder.Append(TableName.ToScript());
            builder.Append(" policy row_level_security ");
            builder.Append(IsEnabled ? "enable" : "disable");
            builder.Append(" ");
            builder.AppendLine(Query.ToScript());

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterRowLevelSecurityPolicyCommand? currentCommand,
            AlterRowLevelSecurityPolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy
                yield return new DeleteRowLevelSecurityPolicyCommand(currentCommand!.TableName);
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