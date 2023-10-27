using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies.IngestionTime
{
    /// <summary>
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-ingestion-time-policy-command"/>
    /// </summary>
    [Command(21100, "Alter Ingestion Time Policies")]
    public class AlterIngestionTimePolicyCommand
        : TableOnlyPolicyCommandBase, ISingularToPluralCommand
    {
        public bool IsEnabled { get; }

        public override string CommandFriendlyName => ".alter table policy ingestiontime";

        public override string ScriptPath => $"tables/policies/ingestiontime/create/{EntityName}";

        public AlterIngestionTimePolicyCommand(EntityName tableName, bool isEnabled)
            : base(tableName)
        {
            IsEnabled = isEnabled;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableName = rootElement.GetDescendants<NameReference>().Last();
            var enabledToken = rootElement.GetUniqueDescendant<SyntaxToken>(
                "Ingestion time",
                t => t.Kind == SyntaxKind.BooleanLiteralToken);
            var isEnabled = (bool)enabledToken.Value;

            return new AlterIngestionTimePolicyCommand(EntityName.FromCode(tableName.Name), isEnabled);
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".alter table ");
            builder.Append(EntityName);
            builder.Append(" policy ingestiontime ");
            builder.AppendLine(IsEnabled.ToString().ToLower());

            return builder.ToString();
        }

        public override bool Equals(CommandBase? other)
        {
            var otherPolicy = other as AlterIngestionTimePolicyCommand;
            var areEqualed = otherPolicy != null
                && base.Equals(otherPolicy)
                && otherPolicy.IsEnabled.Equals(IsEnabled);

            return areEqualed;
        }

        IEnumerable<CommandBase> ISingularToPluralCommand.ToPlural(
            IEnumerable<CommandBase> singularCommands)
        {
            var singularPolicyCommands = singularCommands
                .Cast<AlterIngestionTimePolicyCommand>();

            //  We might want to cap batches to a maximum size?
            var pluralCommands = singularPolicyCommands
                .GroupBy(c => c.IsEnabled)
                .Select(g => new AlterIngestionTimePluralPolicyCommand(
                    g.Select(c => c.EntityName),
                    g.Key));

            return pluralCommands.ToImmutableArray();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterIngestionTimePolicyCommand? currentCommand,
            AlterIngestionTimePolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy
                yield return new DeleteIngestionTimePolicyCommand(currentCommand!.EntityName);
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