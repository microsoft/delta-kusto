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
    [Command(10100, "Alter Auto Delete Policies")]
    public class AlterAutoDeletePolicyCommand : TableOnlyPolicyCommandBase
    {
        public override string CommandFriendlyName => ".alter <entity> policy auto_delete";

        public override string ScriptPath => $"tables/policies/auto_delete/{TableName}";

        public AlterAutoDeletePolicyCommand(
            EntityName tableName,
            JsonDocument policy) : base(tableName, policy)
        {
        }

        public AlterAutoDeletePolicyCommand(
            EntityName tableName,
            DateTime expiryDate,
            bool deleteIfNotEmpty)
            : this(
                  tableName,
                  ToJsonDocument(new
                  {
                      ExpiryDate = expiryDate.ToString(),
                      DeleteIfNotEmpty = deleteIfNotEmpty
                  }))
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableName = rootElement.GetDescendants<NameReference>().Last();
            var policyText = QuotedText.FromLiteral(
                rootElement.GetUniqueDescendant<LiteralExpression>(
                    "AutoDeletePolicy",
                    e => e.NameInParent == "AutoDeletePolicy"));
            var policy = JsonSerializer.Deserialize<JsonDocument>(policyText.Text);

            if (policy == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policyText.ToScript()}");
            }

            return new AlterAutoDeletePolicyCommand(EntityName.FromCode(tableName.Name), policy);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as AlterAutoDeletePolicyCommand;
            var areEqualed = otherFunction != null
                && otherFunction.TableName.Equals(TableName)
                && PolicyEquals(otherFunction);

            return areEqualed;
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