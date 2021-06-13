using DeltaKustoLib.SchemaObjects;
using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace DeltaKustoLib.CommandModel
{
    /// <summary>
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/update-policy#alter-update-policy"/>
    /// </summary>
    public class AlterUpdatePolicyCommand : CommandBase
    {
        public EntityName TableName { get; }

        public IImmutableList<UpdatePolicy> UpdatePolicies { get; }

        public override string CommandFriendlyName => ".alter table policy update";

        public AlterUpdatePolicyCommand(
            EntityName tableName,
            IEnumerable<UpdatePolicy> updatePolicies)
        {
            TableName = tableName;
            UpdatePolicies = updatePolicies
                .OrderBy(p => p.Source)
                .ThenBy(p => p.IsEnabled)
                .ThenBy(p => p.Query)
                .ThenBy(p => p.PropagateIngestionProperties)
                .ThenBy(p => p.IsTransactional)
                .ToImmutableArray();
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var q = rootElement.GetDescendants<SyntaxElement>();
            var cleanTableName = rootElement
                .GetDescendants<TokenName>(e => e.NameInParent == "Name")
                .FirstOrDefault();
            var escapedTableName = rootElement
                .GetDescendants<LiteralExpression>(e => e.NameInParent == "Name")
                .FirstOrDefault();

            if ((cleanTableName == null || cleanTableName.Name.Text == string.Empty)
                && escapedTableName == null)
            {
                throw new DeltaException("Can't find table name");
            }

            var tableName = escapedTableName != null
                ? EntityName.FromCode(escapedTableName)
                : EntityName.FromCode(cleanTableName!);
            var policiesText = QuotedText.FromLiteral(
                rootElement.GetUniqueDescendant<LiteralExpression>(
                    "UpdatePolicy",
                    e => e.NameInParent == "UpdatePolicy"));
            var policies = JsonSerializer.Deserialize<UpdatePolicy[]>(policiesText.Text);

            if (policies == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policiesText.ToScript()}");
            }

            return new AlterUpdatePolicyCommand(tableName, policies);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as AlterUpdatePolicyCommand;
            var areEqualed = otherFunction != null
                && otherFunction.TableName.Equals(TableName)
                //  Check that all parameters are equal
                && otherFunction.UpdatePolicies.SequenceEqual(UpdatePolicies);

            return areEqualed;
        }

        public override string ToScript()
        {
            var builder = new StringBuilder();

            builder.Append(".alter table ");
            builder.Append(TableName);
            builder.Append(" policy update @'");
            builder.Append(JsonSerializer.Serialize(UpdatePolicies));
            builder.Append("'");

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterUpdatePolicyCommand? currentUpdatePolicyCommand,
            AlterUpdatePolicyCommand? targetUpdatePolicyCommand)
        {
            var hasCurrent = currentUpdatePolicyCommand != null
                && !currentUpdatePolicyCommand.UpdatePolicies.Any();
            var hasTarget = targetUpdatePolicyCommand != null
                && !targetUpdatePolicyCommand.UpdatePolicies.Any();

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy objects
                yield return new AlterUpdatePolicyCommand(
                    currentUpdatePolicyCommand!.TableName,
                    new UpdatePolicy[0]);
            }
            else if (hasTarget)
            {
                if (!hasCurrent
                    || currentUpdatePolicyCommand!
                    .UpdatePolicies
                    .SequenceEqual(targetUpdatePolicyCommand!.UpdatePolicies))
                {   //  There is a target and either no current or the current is different
                    yield return targetUpdatePolicyCommand!;
                }
            }
            else
            {   //  Both target and current are null:  no delta
            }
        }
    }
}