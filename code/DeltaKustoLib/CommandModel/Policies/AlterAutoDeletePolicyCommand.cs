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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/auto-delete-policy-command#alter-policy"/>
    /// </summary>
    public class AlterAutoDeletePolicyCommand : CommandBase
    {
        private static readonly JsonSerializerOptions _policiesSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public EntityName TableName { get; }

        public DateTime ExpiryDate { get; }

        public bool DeleteIfNotEmpty { get; }

        public override string CommandFriendlyName => ".alter <entity> policy auto_delete";

        public AlterAutoDeletePolicyCommand(
            EntityName tableName,
            DateTime expiryDate,
            bool deleteIfNotEmpty)
        {
            TableName = tableName;
            ExpiryDate = expiryDate;
            DeleteIfNotEmpty = deleteIfNotEmpty;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableName = rootElement.GetDescendants<NameReference>().Last();
            var policyText = QuotedText.FromLiteral(
                rootElement.GetUniqueDescendant<LiteralExpression>(
                    "AutoDeletePolicy",
                    e => e.NameInParent == "AutoDeletePolicy"));
            var policy = JsonSerializer.Deserialize<AutoDeletePolicy>(policyText.Text);

            if (policy == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policyText.ToScript()}");
            }

            return new AlterAutoDeletePolicyCommand(
                EntityName.FromCode(tableName.Name),
                policy.GetExpiryDate(),
                policy.DeleteIfNotEmpty);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as AlterAutoDeletePolicyCommand;
            var areEqualed = otherFunction != null
                && otherFunction.TableName.Equals(TableName)
                && otherFunction.ExpiryDate.Equals(ExpiryDate)
                && otherFunction.DeleteIfNotEmpty.Equals(DeleteIfNotEmpty);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();
            var policy = AutoDeletePolicy.Create(ExpiryDate, DeleteIfNotEmpty);

            builder.Append(".alter table ");
            builder.Append(TableName);
            builder.Append(" policy auto_delete");
            builder.AppendLine();
            builder.Append("```");
            builder.Append(JsonSerializer.Serialize(policy, _policiesSerializerOptions));
            builder.AppendLine();
            builder.Append("```");

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterRetentionPolicyCommand? currentCommand,
            AlterRetentionPolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy
                throw new NotImplementedException();
                //yield return new DeleteAutoDeletePolicyCommand(currentCommand!.EntityName);
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