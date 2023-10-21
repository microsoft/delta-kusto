using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Kusto.Language.Syntax;

namespace DeltaKustoLib.CommandModel.Policies.RestrictedView
{
    /// <summary>
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-table-restricted-view-access-policy-command"/>
    /// </summary>
    [Command(19100, "Alter restricted view Policy")]
    public class AlterRestrictedViewPolicyCommand : TableOnlyPolicyCommandBase
    {
        public bool IsEnabled { get; }

        public override string CommandFriendlyName => ".alter table policy restricted_view_access";

        public override string ScriptPath =>
            $"tables/policies/restricted_view_access/create/{TableName}";

        public AlterRestrictedViewPolicyCommand(
            EntityName tableName,
            bool isEnabled)
            : base(tableName)
        {
            IsEnabled = isEnabled;
        }

        public override string ToScript(ScriptingContext? context = null)
        {
            return $".alter table {TableName} policy restricted_view_access {IsEnabled}";
        }

        public override bool Equals(CommandBase? other)
        {
            var otherPolicy = other as AlterRestrictedViewPolicyCommand;
            var areEqualed = otherPolicy != null
                && base.Equals(otherPolicy)
                && otherPolicy.IsEnabled.Equals(IsEnabled);

            return areEqualed;
        }

        internal static CommandBase? FromCode(CommandBlock commandBlock)
        {
            var nameReferences = commandBlock.GetDescendants<NameReference>();
            var tableNameReference = nameReferences.Last();
            var booleanToken = commandBlock.GetUniqueDescendant<SyntaxToken>(
                "boolean",
                t => t.Kind == SyntaxKind.BooleanLiteralToken);

            return new AlterRestrictedViewPolicyCommand(
                EntityName.FromCode(tableNameReference.Name),
                (bool)booleanToken.Value);
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterRestrictedViewPolicyCommand? currentCommand,
            AlterRestrictedViewPolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {
                // No target, we remove the current policy
                yield return new DeleteRestrictedViewPolicyCommand(currentCommand!.TableName);
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