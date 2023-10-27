﻿using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeltaKustoLib.CommandModel.Policies.Update
{
    /// <summary>
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/update-policy#alter-update-policy"/>
    /// </summary>
    [Command(16100, "Alter Update Policies")]
    public class AlterUpdatePolicyCommand : TableOnlyPolicyCommandBase
    {
        private static readonly UpdatePolicySerializerContext _serializerContext = new UpdatePolicySerializerContext(
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        public IImmutableList<UpdatePolicy> UpdatePolicies { get; }

        public override string CommandFriendlyName => ".alter table policy update";

        public override string ScriptPath => $"tables/policies/update/create/{EntityName}";

        public AlterUpdatePolicyCommand(
            EntityName tableName,
            IEnumerable<UpdatePolicy> updatePolicies) : base(tableName)
        {
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
            var cleanTableName = rootElement
                .GetDescendants<TokenName>(e => e.NameInParent == "Name")
                .FirstOrDefault();
            var tableName = rootElement.GetDescendants<NameReference>().Last();

            if ((cleanTableName == null || cleanTableName.Name.Text == string.Empty)
                && tableName == null)
            {
                throw new DeltaException("Can't find table name");
            }

            var policiesText = QuotedText.FromLiteral(
                rootElement.GetUniqueDescendant<LiteralExpression>(
                    "UpdatePolicy",
                    e => e.NameInParent == "UpdatePolicy"));
            var policies = Deserialize<UpdatePolicy[]>(policiesText.Text);

            if (policies == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policiesText.ToScript()}");
            }

            return new AlterUpdatePolicyCommand(
                EntityName.FromCode(tableName.Name),
                policies);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherPolicy = other as AlterUpdatePolicyCommand;
            var areEqualed = otherPolicy != null
                && base.Equals(otherPolicy)
                //  Check that all parameters are equal
                && otherPolicy.UpdatePolicies.SequenceEqual(UpdatePolicies);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".alter table ");
            builder.Append(EntityName.ToScript());
            builder.Append(" policy update");
            builder.AppendLine();
            builder.Append("```");
            builder.Append(Serialize(UpdatePolicies, _serializerContext));
            builder.AppendLine();
            builder.Append("```");

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterUpdatePolicyCommand? currentUpdatePolicyCommand,
            AlterUpdatePolicyCommand? targetUpdatePolicyCommand)
        {
            var hasCurrent = currentUpdatePolicyCommand != null
                && currentUpdatePolicyCommand.UpdatePolicies.Any();
            var hasTarget = targetUpdatePolicyCommand != null
                && targetUpdatePolicyCommand.UpdatePolicies.Any();

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy objects
                yield return new DeleteUpdatePolicyCommand(currentUpdatePolicyCommand!.EntityName);
            }
            else if (hasTarget)
            {
                if (!hasCurrent
                    || !currentUpdatePolicyCommand!
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